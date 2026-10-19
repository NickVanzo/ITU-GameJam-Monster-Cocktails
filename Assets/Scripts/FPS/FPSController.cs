using System;
using System.Collections.Generic;
using System.Text;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(CharacterController))]
public class FPSController : IDamageable, IWielder
{
    public event Action OnHurt;
    public event Action OnDied;
    public event Action<bool> OnHitEnemy;

    public Transform CameraRoot;
    public Transform RightHandRoot;
    public Transform LeftHandRoot;
    public MuzzleFlash MuzzleFlash;
    public MuzzleFlash OffhandMuzzleFlash;
    public CinemachineImpulseSource ImpulseSource;
    public CinemachineCamera FPSCamera;

    [Header("Movement")]
    [SerializeField] float fWalkSpeed = 7.0f;
    [SerializeField] float fSprintSpeed = 10.0f;
    [SerializeField] float fGroundAcceleration = 60.0f;
    [SerializeField] float fAirAcceleration = 15.0f;
    [SerializeField] float fGravity = -25.0f;

    [Header("Look")]
    [SerializeField] float fMouseSensitivity = 0.12f;
    [SerializeField] float fStickSensitivity = 180.0f;
    [SerializeField] float fMaxPitch = 85.0f;

    [Header("Shooting")]
    [SerializeField] LayerMask HitMask = ~0;

    [Header("Zoom")]
    [SerializeField] CameraZoom Zoom = new();
    [Tooltip("Gun position while fully zoomed, with its sights on the centre of the screen.")]
    [SerializeField] Vector3 vGunAimPosition = new(0.0f, -0.077f, 0.3f);
    [Tooltip("Recoil is multiplied by this while fully zoomed, so the gun doesn't flip across the view.")]
    [SerializeField] float fGunZoomedRecoil = 0.25f;

    [Header("Gun Bob")]
    [SerializeField] float fGunIdleBob = 0.008f;
    [SerializeField] float fGunIdleBobFrequency = 1.2f;
    [SerializeField] float fGunWalkBob = 0.025f;
    [Tooltip("Bob is multiplied by this while fully zoomed.")]
    [SerializeField] float fGunZoomedBob = 0.3f;

    [Header("Interaction")]
    [SerializeField] float fInteractRange = 2.5f;

    [Header("Audio")]
    [SerializeField] float fStepLength = 2.4f;
    [SerializeField] float fLandSoundSpeed = 4.0f;

    
    // States
    CharacterController Controller;
    bool bCanMove = false;
    bool bWasGrounded = true;
    Vector3 m_vVelocity;
    
    // Sound
    float m_fStepDistance;
    float m_fPitch;
    
    // Todo : Move animation stuff to a different gun handle maybe 
    float m_fGunBobPhase;
    float m_fGunWalkBlend;
    float m_fBaseFieldOfView;

    // Keybinds
    InputAction MoveAction;
    InputAction LookAction;
    InputAction AttackAction;
    InputAction ReloadAction;
    InputAction SprintAction;
    InputAction InteractAction;
    InputAction ZoomAction;
    InputAction SwapAction;
    InputAction LastLoadoutAction;
    readonly InputAction[] m_aSlotActions = new InputAction[4];

    public bool CanMove => bCanMove;
    public ElevatorRope LookedAtRope { get; private set; }
    public ElevatorRope HeldRope { get; private set; }

    [Tooltip("Used when no gun was picked in the bar's armory, e.g. when playing this level on its own.")]
    [SerializeField] GunObject GunDefinition;
    [SerializeField] List<EquippableObject> OffhandItems = new();
    Hand m_LeftHand;
    Hand m_RightHand;
    readonly List<HeldItem> m_aCarried = new();
    (HeldItem Right, HeldItem Left) m_LastLoadout;
    string m_sLastHit;
    float m_fLastHitTime;

    public HeldItem RightHandItem => m_RightHand?.Current;
    public HeldItem LeftHandItem => m_LeftHand?.Current;

    class Hand
    {
        public readonly Transform Root;
        public readonly MuzzleFlash MuzzleFlash;
        public readonly Vector3 vRestPosition;
        public readonly Vector3 vMuzzleOffset;
        public readonly float fSide;
        public float fKick;
        public float fKickTarget;
        public HeldItem Current;
        public HeldItem Target;
        public float fRaise;

        public bool IsReady => Current != null && Current == Target && fRaise >= 1.0f;

        // TODO : proper vfx implmentation, muzzleflash doesnt have to be link at all
        public Hand(Transform root, MuzzleFlash muzzleFlash)
        {
            Root = root;
            MuzzleFlash = muzzleFlash;

            if(root == null)
            {
                return;
            }

            vRestPosition = root.localPosition;
            fSide = vRestPosition.x < 0.0f ? -1.0f : 1.0f;

            if(muzzleFlash != null)
            {
                vMuzzleOffset = muzzleFlash.transform.localPosition - vRestPosition;
            }
        }
    }

    Hand GetPrimaryHand()
    {
        if(m_RightHand.Target is { TakesMainButton: true })
        {
            return m_RightHand;
        }

        return m_LeftHand.Target is { TakesMainButton: true } ? m_LeftHand : m_RightHand;
    }

    Hand GetSecondaryHand()
    {
        return GetPrimaryHand() == m_RightHand ? m_LeftHand : m_RightHand;
    }

    Hand HandHolding(HeldItem held)
    {
        return m_RightHand.Current == held ? m_RightHand : m_LeftHand.Current == held ? m_LeftHand : null;
    }

    protected override void Awake()
    {
        base.Awake();

        Controller = GetComponent<CharacterController>();

        m_RightHand = new Hand(RightHandRoot, MuzzleFlash);
        m_LeftHand  = new Hand(LeftHandRoot, OffhandMuzzleFlash);
        if(LeftHandRoot != null && LeftHandRoot == RightHandRoot)
        {
            Debug.LogWarning("FPSController: Offhand and Gun are the same transform, give the off hand its own root", this);
        }

        InputActionAsset actions = InputSystem.actions;
        MoveAction        = actions.FindAction("Player/Move", true);
        LookAction        = actions.FindAction("Player/Look", true);
        AttackAction      = actions.FindAction("Player/Attack", true);
        ReloadAction      = actions.FindAction("Player/Reload", true);
        SprintAction      = actions.FindAction("Player/Sprint", true);
        InteractAction    = actions.FindAction("Player/Interact", true);
        ZoomAction        = actions.FindAction("Player/Zoom", true);
        SwapAction        = actions.FindAction("Player/SwapHands", true);
        LastLoadoutAction = actions.FindAction("Player/LastLoadout", true);

        for(int i = 0; i < m_aSlotActions.Length; ++i)
        {
            m_aSlotActions[i] = actions.FindAction($"Player/QuickSlot{i + 1}", true);
        }

        GunObject frame = Loadout.Frame != null ? Loadout.Frame : GunDefinition;
        if(frame == null)
        {
            Debug.LogError("FPSController: no gun picked in the armory and no GunDefinition assigned, the player can't shoot.", this);
            return;
        }

        m_aCarried.Add(frame.Materialize(m_RightHand.Root, this));
        if(LeftHandRoot != null)
        {
            foreach(EquippableObject item in Loadout.Offhand.Count > 0 ? Loadout.Offhand : OffhandItems)
            {
                if(item != null)
                {
                    m_aCarried.Add(item.Materialize(m_RightHand.Root, this));
                }
            }
        }

        ResetHands();
    }

    static InputAction EquipActionOf(HeldItem held)
    {
        return held.Item.EquipAction != null ? held.Item.EquipAction.action : null;
    }

    void OnEnable()
    {
        Controller.enabled = true;
        MoveAction.Enable();
        LookAction.Enable();
        AttackAction.Enable();
        ReloadAction.Enable();
        SprintAction.Enable();
        InteractAction.Enable();
        ZoomAction.Enable();
        SwapAction.Enable();
        LastLoadoutAction.Enable();

        foreach(InputAction slot in m_aSlotActions)
        {
            slot.Enable();
        }

        foreach(HeldItem held in m_aCarried)
        {
            EquipActionOf(held)?.Enable();
        }
    }

    void OnDisable()
    {
        bCanMove = false;
        LookedAtRope = null;
        HeldRope = null;
        Controller.enabled = false;
    }

    public void Spawn(Vector3 position, Quaternion rotation)
    {
        transform.SetPositionAndRotation(position, Quaternion.Euler(0.0f, rotation.eulerAngles.y, 0.0f));
        CameraRoot.localRotation = Quaternion.identity;
        m_vVelocity = Vector3.zero;
        m_fPitch = 0.0f;
        bWasGrounded = true;
        m_fStepDistance = 0.0f;
        ResetHands();
        ResetZoom();
        ResetHealth();
        Physics.SyncTransforms();
    }

    public void Teleport(Vector3 position)
    {
        transform.position = position;
        Physics.SyncTransforms();
    }

    public void SetCanMove(bool bValue)
    {
        bCanMove = bValue;
        m_vVelocity = Vector3.zero;
    }

    public void Update()
    {
        UpdateZoom(IsAlive() && bCanMove && ZoomAction.IsPressed() && CanAim());
        UpdateHands();

        if(!IsAlive() || !bCanMove)
        {
            LookedAtRope = null;
            HeldRope = null;
            HandleShooting(false);
            return;
        }

        HandleLook();
        HandleMovement();
        HandleInteraction();
        HandleEquipment();
        HandleShooting(true);
    }

    static bool ClaimsSecondaryButton(HeldItem held)
    {
        return held != null && held.Item.eSecondaryButton == SecondaryButton.Use;
    }

    bool CanAim()
    {
        Hand primary = GetPrimaryHand();
        return primary.IsReady && primary.Current.CanAim && !ClaimsSecondaryButton(GetSecondaryHand().Target);
    }

    void HandleEquipment()
    {
        (HeldItem, HeldItem) before = (m_RightHand.Target, m_LeftHand.Target);

        if(LastLoadoutAction.WasPressedThisFrame())
        {
            SetTarget(m_RightHand, m_LastLoadout.Right);
            SetTarget(m_LeftHand, m_LastLoadout.Left);
            m_LastLoadout = before;
            return;
        }

        if(SwapAction.WasPressedThisFrame() && !IsTwoHanded(m_RightHand.Target))
        {
            HeldItem right = m_RightHand.Target;
            SetTarget(m_RightHand, m_LeftHand.Target);
            SetTarget(m_LeftHand, right);
        }

        for(int i = 0; i < m_aSlotActions.Length && i < m_aCarried.Count; ++i)
        {
            if(m_aSlotActions[i].WasPressedThisFrame())
            {
                Equip(m_aCarried[i]);
            }
        }

        foreach(HeldItem held in m_aCarried)
        {
            InputAction action = EquipActionOf(held);
            if(action == null)
            {
                continue;
            }

            Hand hand = held == m_RightHand.Target ? m_RightHand : held == m_LeftHand.Target ? m_LeftHand : null;

            if(action.WasPressedThisFrame())
            {
                held.bTapPending = hand != null && held.Item.eRepress == EquipRepress.Use;
                held.fPressTime = Time.time;

                if(held.bTapPending)
                {
                    continue;
                }

                if(hand != null)
                {
                    SetTarget(hand, null);
                }
                else
                {
                    Equip(held);
                }
            }
            else if(held.bTapPending && action.IsPressed() && held.Item.fHoldToHolster > 0.0f && Time.time - held.fPressTime >= held.Item.fHoldToHolster)
            {
                held.bTapPending = false;
                if(hand != null)
                {
                    SetTarget(hand, null);
                }
            }
            else if(held.bTapPending && action.WasReleasedThisFrame())
            {
                held.bTapPending = false;
                held.Use();
            }
        }

        if((m_RightHand.Target, m_LeftHand.Target) != before)
        {
            m_LastLoadout = before;
        }
    }

    void Equip(HeldItem held)
    {
        if(held == m_RightHand.Target || held == m_LeftHand.Target)
        {
            return;
        }

        if(IsTwoHanded(held) || IsTwoHanded(m_RightHand.Target))
        {
            SetTarget(m_RightHand, held);
            SetTarget(m_LeftHand, null);
            return;
        }

        SetTarget(m_RightHand.Target == null ? m_RightHand : m_LeftHand, held);
    }

    static bool IsTwoHanded(HeldItem held)
    {
        return held != null && held.Item.bTwoHanded;
    }

    static void SetTarget(Hand hand, HeldItem held)
    {
        if(hand.Current != null && hand.Target == hand.Current && held != hand.Current)
        {
            Sfx.Play(hand.Current.Item.HolsterSound);
        }

        hand.Target = held;
    }

    void ResetHands()
    {
        if(m_RightHand == null)
        {
            return;
        }

        foreach(Hand hand in new[] { m_RightHand, m_LeftHand })
        {
            hand.Current = null;
            hand.Target = null;
            hand.fRaise = 0.0f;
            hand.fKick = 0.0f;
            hand.fKickTarget = 0.0f;
        }

        foreach(HeldItem held in m_aCarried)
        {
            held.Detach();
        }

        if(m_aCarried.Count > 0)
        {
            m_RightHand.Current = m_RightHand.Target = m_aCarried[0];
            m_RightHand.fRaise = 1.0f;
            AttachToHand(m_aCarried[0], m_RightHand);
        }

        m_LastLoadout = (m_RightHand.Target, m_LeftHand.Target);
    }

    void StepHand(Hand hand, Hand other)
    {
        HeldItem moving = hand.Current ?? hand.Target;
        float fDrawTime = moving != null ? Mathf.Max(moving.Item.fDrawTime, 0.01f) : 0.15f;
        bool bRaise = hand.Current != null && hand.Current == hand.Target;
        hand.fRaise = Mathf.MoveTowards(hand.fRaise, bRaise ? 1.0f : 0.0f, Time.deltaTime / fDrawTime);

        if(hand.fRaise > 0.0f || hand.Current == hand.Target)
        {
            return;
        }

        if(hand.Current != null)
        {
            hand.Current.Detach();
            hand.Current = null;
        }

        if(hand.Target != null && hand.Target != other.Current)
        {
            hand.Current = hand.Target;
            AttachToHand(hand.Current, hand);
            Sfx.Play(hand.Current.Item.DrawSound);
        }
    }

    static void AttachToHand(HeldItem held, Hand hand)
    {
        if(hand.Root != null)
        {
            held.Attach(hand.Root);
            PlaceView(hand, held, Quaternion.identity, Vector3.zero);
        }
    }

    void HandleLook()
    {
        Vector2 vLook = LookAction.ReadValue<Vector2>();
        bool bPointer = LookAction.activeControl != null && LookAction.activeControl.device is Pointer;
        vLook *= (bPointer ? fMouseSensitivity : fStickSensitivity * Time.deltaTime) * Zoom.Scale;

        transform.Rotate(0.0f, vLook.x, 0.0f);
        m_fPitch = Mathf.Clamp(m_fPitch - vLook.y, -fMaxPitch, fMaxPitch);
        CameraRoot.localRotation = Quaternion.Euler(m_fPitch, 0.0f, 0.0f);
    }

    public void HandleMovement()
    {
        Vector2 vInput = Vector2.ClampMagnitude(MoveAction.ReadValue<Vector2>(), 1.0f);
        float fSpeed = SprintAction.IsPressed() ? fSprintSpeed : fWalkSpeed;
        Vector3 vTarget = (transform.right * vInput.x + transform.forward * vInput.y) * fSpeed;

        bool bGrounded = Controller.isGrounded;
        float fAcceleration = bGrounded ? fGroundAcceleration : fAirAcceleration;
        Vector3 vHorizontal = Vector3.MoveTowards(new Vector3(m_vVelocity.x, 0.0f, m_vVelocity.z), vTarget, fAcceleration * Time.deltaTime);
        m_vVelocity.x = vHorizontal.x;
        m_vVelocity.z = vHorizontal.z;

        if(bGrounded && m_vVelocity.y < 0.0f)
        {
            m_vVelocity.y = -2.0f;
        }

        m_vVelocity.y += fGravity * Time.deltaTime;
        float fFallSpeed = -m_vVelocity.y;
        Controller.Move(m_vVelocity * Time.deltaTime);

        UpdateMovementSounds(fFallSpeed);
    }

    void UpdateMovementSounds(float fFallSpeed)
    {
        bool bGrounded = Controller.isGrounded;
        if(bGrounded && !bWasGrounded && fFallSpeed > fLandSoundSpeed)
        {
            Sfx.Play(Sfx.Sounds.PlayerLand);
        }
        bWasGrounded = bGrounded;

        Vector3 vHorizontal = Controller.velocity;
        vHorizontal.y = 0.0f;
        float fSpeed = vHorizontal.magnitude;

        if(!bGrounded || fSpeed < 0.5f)
        {
            // Standing still: the first step comes half a stride after starting to move.
            m_fStepDistance = fStepLength * 0.5f;
            return;
        }

        m_fStepDistance += fSpeed * Time.deltaTime;
        if(m_fStepDistance >= fStepLength)
        {
            m_fStepDistance -= fStepLength;
            Sfx.Play(Sfx.Sounds.PlayerFootstep);
        }
    }

    void HandleInteraction()
    {
        LookedAtRope = null;

        if(Physics.Raycast(CameraRoot.position, CameraRoot.forward, out RaycastHit hit, fInteractRange, HitMask, QueryTriggerInteraction.Ignore))
        {
            LookedAtRope = hit.collider.GetComponentInParent<ElevatorRope>();
        }

        // Grab the rope by looking at it while [E] is down, then keep pulling until [E] is released, wherever you look.
        if(!InteractAction.IsPressed())
        {
            HeldRope = null;
        }
        else if(HeldRope == null)
        {
            HeldRope = LookedAtRope;
        }

        if(HeldRope != null)
        {
            HeldRope.Hold();
        }
    }


    public void Fired(HeldItem item, float fRecoil)
    {
        Hand hand = HandHolding(item) ?? m_RightHand;
        m_fPitch -= fRecoil;
        hand.fKick = 1.0f;
        hand.fKickTarget = -65.0f * ( item.Item is GunObject ? ((GunObject) item.Item).fBaseRecoil : 1.0f );

        if(ImpulseSource != null)
        {
            float fCameraKick =  -0.06f;
            ImpulseSource.GenerateImpulseWithVelocity(new Vector3(0.0f, 0.0f, fCameraKick));
        }

        if(hand.MuzzleFlash != null)
        {
            hand.MuzzleFlash.Play();
        }
    }

    public void Hit(RayHit hit)
    {
        m_fLastHitTime = Time.time;

        if(hit.Target == null)
        {
            m_sLastHit = "NO TARGET";
            Sfx.Play(Sfx.Sounds.BulletImpact, hit.vPoint);
            return;
        }

        // Runs AFTER the damage is dealt, so this is the health left
        string sName = hit.Target is EnemyBehavior enemy && enemy.Monster != null ? enemy.Monster.sName : hit.Target.name;
        m_sLastHit = $"{sName.ToUpper()}{(hit.bHeadshot ? " HEADSHOT" : "")}  HP {hit.Target.Current}/{hit.Target.Max}";

        Sfx.Play(hit.bHeadshot ? Sfx.Sounds.HeadshotMarker : Sfx.Sounds.HitMarker);
        OnHitEnemy?.Invoke(hit.bHeadshot);
    }

    public string DescribeQuickBar()
    {
        if(m_RightHand == null)
        {
            return "";
        }

        var builder = new StringBuilder();
        for(int i = 0; i < m_aCarried.Count; ++i)
        {
            HeldItem held = m_aCarried[i];
            string sHand = held == m_LeftHand.Target ? " [L]" : held == m_RightHand.Target ? " [R]" : "";
            builder.Append(i > 0 ? "     " : "").Append(i < m_aSlotActions.Length ? $"{i + 1} " : "").Append(held.Item.sDisplayName.ToUpper()).Append(sHand);
        }

        return builder.ToString();
    }

    public string DebugPrintGun()
    {
        if(m_RightHand == null)
        {
            return "NO GUN";
        }

        var builder = new StringBuilder();
        foreach(Hand hand in new[] { m_RightHand, m_LeftHand })
        {
            if(hand.Current == null)
            {
                continue;
            }

            builder.Append(builder.Length > 0 ? "\n" : "").Append(hand == m_RightHand ? "R " : "L ").Append(hand.Current.DebugPrint());
        }

        if(m_sLastHit != null)
        {
            builder.Append($"\nLAST HIT {m_sLastHit}  ({Time.time - m_fLastHitTime:0.0}s ago)");
        }

        return builder.ToString();
    }

    // Funilly enough this should be called handle handling...
    public void HandleShooting(bool bCanShoot)
    {
        if(m_RightHand == null)
        {
            return;
        }

        Hand primary = GetPrimaryHand();
        Hand secondary = GetSecondaryHand();
        HeldItem main = primary.IsReady && primary.Current.TakesMainButton ? primary.Current : null;
        HeldItem other = secondary.IsReady && ClaimsSecondaryButton(secondary.Current) ? secondary.Current : null;

        if(bCanShoot && ReloadAction.WasPressedThisFrame())
        {
            main?.Reload();
            other?.Reload();
        }

        ShooterState shooter = new()
        {
            vPos = CameraRoot.position,
            vAim = CameraRoot.forward,
            HitMask = HitMask,
            Owner = this,
        };

        foreach(HeldItem held in m_aCarried)
        {
            InputAction trigger = held == main ? AttackAction : held == other ? ZoomAction : null;
            held.Tick(trigger != null ? ReadTrigger(trigger, bCanShoot) : default, shooter, Time.deltaTime);
        }
    }

    static GunTrigger.TriggerInput ReadTrigger(InputAction action, bool bEnabled)
    {
        return new GunTrigger.TriggerInput
        {
            bHeld = bEnabled && action.IsPressed(),
            bPressed = bEnabled && action.WasPressedThisFrame(),
        };
    }

    void ResetZoom()
    {
        if(FPSCamera == null)
        {
            return;
        }

        if(m_fBaseFieldOfView <= 0.0f)
        {
            m_fBaseFieldOfView = FPSCamera.Lens.FieldOfView;
        }

        Zoom.Reset(m_fBaseFieldOfView);
        FPSCamera.Lens.FieldOfView = m_fBaseFieldOfView;
    }

    void UpdateZoom(bool bWantZoom)
    {
        if(FPSCamera != null)
        {
            FPSCamera.Lens.FieldOfView = Zoom.Update(bWantZoom, Time.deltaTime);
        }
    }

    void UpdateHands()
    {
        if(m_RightHand == null)
        {
            return;
        }

        StepHand(m_RightHand, m_LeftHand);
        StepHand(m_LeftHand, m_RightHand);

        Vector3 vBob = ComputeHandBob();
        Hand aiming = GetPrimaryHand();
        UpdateHand(m_RightHand, vBob, aiming == m_RightHand ? Zoom.Amount : 0.0f);
        UpdateHand(m_LeftHand, new Vector3(-vBob.x, vBob.y, vBob.z), aiming == m_LeftHand ? Zoom.Amount : 0.0f);
    }

    void UpdateHand(Hand hand, Vector3 vBob, float fAim)
    {
        if(hand.Root == null)
        {
            return;
        }

        HeldItem held = hand.Current;
        HeldItem shown = hand.Current ?? hand.Target;
        float fLower = shown != null ? shown.Item.fLowerDistance * (1.0f - Mathf.SmoothStep(0.0f, 1.0f, hand.fRaise)) : 0.0f;

        hand.fKick = Mathf.MoveTowards(hand.fKick, 0.0f, Time.deltaTime * 10.0f);
        float fKick = hand.fKick * Mathf.Lerp(1.0f, fGunZoomedRecoil, fAim);


        Vector3 vPosition = Vector3.Lerp(hand.vRestPosition, vGunAimPosition, fAim) + vBob + Vector3.down * fLower;
        hand.Root.localPosition = vPosition + new Vector3(0.0f, 0.0f, -0.08f * fKick);
        hand.Root.localRotation = Quaternion.Euler(hand.fKickTarget * fKick, 0.0f, 0.0f);

        if(hand.MuzzleFlash != null)
        {
            hand.MuzzleFlash.transform.localPosition = vPosition + hand.vMuzzleOffset;
        }

        if(held != null)
        {
            Quaternion qSpin = held.GetPoseSpin(out Vector3 vPivot);
            PlaceView(hand, held, qSpin, vPivot);
        }
    }

    static void PlaceView(Hand hand, HeldItem held, Quaternion qSpin, Vector3 vPivot)
    {
        if(held.View == null)
        {
            return;
        }

        Quaternion qBase = Quaternion.Euler(held.Item.vHandRotation);
        Vector3 vPosition = held.Item.vHandOffset + qBase * (vPivot - qSpin * vPivot);
        held.View.transform.SetLocalPositionAndRotation(Mirror(vPosition, hand.fSide), Mirror(qBase * qSpin, hand.fSide));
    }

    static Vector3 Mirror(Vector3 v, float fSide)
    {
        return fSide < 0.0f ? new Vector3(-v.x, v.y, v.z) : v;
    }

    static Quaternion Mirror(Quaternion q, float fSide)
    {
        return fSide < 0.0f ? new Quaternion(q.x, -q.y, -q.z, q.w) : q;
    }

    Vector3 ComputeHandBob()
    {
        Vector3 vHorizontal = Controller.velocity;
        vHorizontal.y = 0.0f;
        float fSpeed = vHorizontal.magnitude;

        float fWalk = Controller.isGrounded ? Mathf.Clamp01(fSpeed / fWalkSpeed) : 0.0f;
        m_fGunWalkBlend = Mathf.MoveTowards(m_fGunWalkBlend, fWalk, Time.deltaTime * 4.0f);
        m_fGunBobPhase += fSpeed / fStepLength * Mathf.PI * Time.deltaTime;

        Vector3 vWalk = new(Mathf.Cos(m_fGunBobPhase) * 0.5f, -Mathf.Abs(Mathf.Sin(m_fGunBobPhase)), 0.0f);
        float fIdle = Mathf.Sin(Time.time * fGunIdleBobFrequency * 2.0f * Mathf.PI);

        Vector3 vBob = vWalk * (fGunWalkBob * m_fGunWalkBlend) + Vector3.up * (fIdle * fGunIdleBob * (1.0f - m_fGunWalkBlend));
        return vBob * Mathf.Lerp(1.0f, fGunZoomedBob, Zoom.Amount);
    }

    protected override void OnDamaged()
    {
        if(ImpulseSource != null)
        {
            ImpulseSource.GenerateImpulseWithVelocity(UnityEngine.Random.insideUnitSphere * 0.3f);
        }

        if(IsAlive())
        {
            Sfx.Play(Sfx.Sounds.PlayerHurt);
        }

        OnHurt?.Invoke();
    }

    protected override void OnDeath()
    {
        bCanMove = false;
        Sfx.Play(Sfx.Sounds.PlayerDeath);
        OnDied?.Invoke();
    }
}
