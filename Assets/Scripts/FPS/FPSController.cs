using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(CharacterController))]
public class FPSController : IDamageable
{
    const string HeadTag = "Head";

    public event System.Action OnHurt;
    public event System.Action OnDied;
    public event System.Action<bool> OnHitEnemy;

    public Transform CameraRoot;
    public Transform Gun;
    public CinemachineImpulseSource ImpulseSource;

    [Header("Movement")]
    [SerializeField] float fWalkSpeed = 7.0f;
    [SerializeField] float fSprintSpeed = 10.0f;
    [SerializeField] float fGroundAcceleration = 60.0f;
    [SerializeField] float fAirAcceleration = 15.0f;
    [SerializeField] float fJumpHeight = 1.2f;
    [SerializeField] float fGravity = -25.0f;

    [Header("Look")]
    [SerializeField] float fMouseSensitivity = 0.12f;
    [SerializeField] float fStickSensitivity = 180.0f;
    [SerializeField] float fMaxPitch = 85.0f;

    [Header("Shooting")]
    [SerializeField] int nDamage = 1;
    [SerializeField] float fFireCooldown = 0.12f;
    [SerializeField] float fRange = 100.0f;
    [SerializeField] float fRecoil = 0.8f;
    [SerializeField] LayerMask HitMask = ~0;

    [Header("Interaction")]
    [SerializeField] float fInteractRange = 2.5f;

    [Header("Audio")]
    [SerializeField] float fStepLength = 2.4f;
    [SerializeField] float fLandSoundSpeed = 4.0f;

    CharacterController Controller;
    bool bCanMove = false;
    bool bWasGrounded = true;
    Vector3 m_vVelocity;
    float m_fStepDistance;
    float m_fPitch;
    float m_fFireTimestamp;
    float m_fGunKick;
    Vector3 m_vGunRestPosition;

    InputAction MoveAction;
    InputAction LookAction;
    InputAction AttackAction;
    InputAction JumpAction;
    InputAction SprintAction;
    InputAction InteractAction;

    public bool CanMove => bCanMove;
    public ElevatorRope LookedAtRope { get; private set; }
    public ElevatorRope HeldRope { get; private set; }

    protected override void Awake()
    {
        base.Awake();
        Controller = GetComponent<CharacterController>();

        if(Gun != null)
        {
            m_vGunRestPosition = Gun.localPosition;
        }

        InputActionAsset actions = InputSystem.actions;
        MoveAction = actions.FindAction("Player/Move", true);
        LookAction = actions.FindAction("Player/Look", true);
        AttackAction = actions.FindAction("Player/Attack", true);
        JumpAction = actions.FindAction("Player/Jump", true);
        SprintAction = actions.FindAction("Player/Sprint", true);
        InteractAction = actions.FindAction("Player/Interact", true);
    }

    void OnEnable()
    {
        Controller.enabled = true;
        MoveAction.Enable();
        LookAction.Enable();
        AttackAction.Enable();
        JumpAction.Enable();
        SprintAction.Enable();
        InteractAction.Enable();
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
        m_vVelocity = Vector3.zero;
        m_fPitch = 0.0f;
        m_fGunKick = 0.0f;
        bWasGrounded = true;
        m_fStepDistance = 0.0f;
        CameraRoot.localRotation = Quaternion.identity;
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
        if(!IsAlive() || !bCanMove)
        {
            LookedAtRope = null;
            HeldRope = null;
            return;
        }

        HandleLook();
        HandleMovement();
        HandleInteraction();
        HandleShooting();
        UpdateGun();
    }

    void HandleLook()
    {
        Vector2 vLook = LookAction.ReadValue<Vector2>();
        bool bPointer = LookAction.activeControl != null && LookAction.activeControl.device is Pointer;
        vLook *= bPointer ? fMouseSensitivity : fStickSensitivity * Time.deltaTime;

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

        if(bGrounded && JumpAction.WasPressedThisFrame())
        {
            m_vVelocity.y = Mathf.Sqrt(fJumpHeight * -2.0f * fGravity);
            Sfx.Play(Sfx.Sounds.PlayerJump);
        }

        m_vVelocity.y += fGravity * Time.deltaTime;
        float fFallSpeed = -m_vVelocity.y;
        CollisionFlags flags = Controller.Move(m_vVelocity * Time.deltaTime);

        if((flags & CollisionFlags.Above) != 0 && m_vVelocity.y > 0.0f)
        {
            m_vVelocity.y = 0.0f;
        }

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

    public void HandleShooting()
    {
        if(!AttackAction.IsPressed() || Time.time < m_fFireTimestamp)
        {
            return;
        }

        m_fFireTimestamp = Time.time + fFireCooldown;
        m_fPitch -= fRecoil;
        m_fGunKick = 1.0f;
        Sfx.Play(Sfx.Sounds.GunShot);

        if(ImpulseSource != null)
        {
            ImpulseSource.GenerateImpulseWithVelocity(new Vector3(0.0f, 0.0f, -0.06f));
        }

        if(!Physics.Raycast(CameraRoot.position, CameraRoot.forward, out RaycastHit hit, fRange, HitMask, QueryTriggerInteraction.Ignore))
        {
            return;
        }

        IDamageable target = hit.collider.GetComponentInParent<IDamageable>();
        if(target == null || target == this || !target.IsAlive())
        {
            Sfx.Play(Sfx.Sounds.BulletImpact, hit.point);
            return;
        }

        bool bHeadshot = hit.collider.CompareTag(HeadTag);
        target.TakeDamage(bHeadshot ? target.Max : nDamage);
        Sfx.Play(bHeadshot ? Sfx.Sounds.HeadshotMarker : Sfx.Sounds.HitMarker);
        OnHitEnemy?.Invoke(bHeadshot);
    }

    void UpdateGun()
    {
        if(Gun == null)
        {
            return;
        }

        m_fGunKick = Mathf.MoveTowards(m_fGunKick, 0.0f, Time.deltaTime * 10.0f);
        Gun.localPosition = m_vGunRestPosition + new Vector3(0.0f, 0.0f, -0.08f * m_fGunKick);
        Gun.localRotation = Quaternion.Euler(-65.0f * m_fGunKick, 0.0f, 0.0f);
    }

    protected override void OnDamaged()
    {
        if(ImpulseSource != null)
        {
            ImpulseSource.GenerateImpulseWithVelocity(Random.insideUnitSphere * 0.3f);
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
