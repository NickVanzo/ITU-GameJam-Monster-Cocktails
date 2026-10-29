using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Serialization;

public enum SecondaryButton
{
    Aim,
    Use
}

public enum EquipRepress
{
    Holster,
    Use
}

public interface IWielder
{
    void Fired(HeldItem item, float fRecoil);
    void Hit(RayHit hit);
}

public abstract class HeldItem
{
    protected HeldItem(EquippableObject item, IWielder wielder)
    {
        Item = item;
        Wielder = wielder;
    }

    public virtual bool TakesMainButton => false;
    public virtual bool CanAim => false;
    public virtual void Tick(in GunTrigger.TriggerInput input, in ShooterState shooter, float fDelta) {}
    public virtual void Use() {}
    public virtual void Reload() {}
    public virtual string DebugPrint() => Item.sDisplayName.ToUpper();

    public virtual bool TryGetCounter(out int nCurrent, out int nMax)
    {
        nCurrent = nMax = 0;
        return false;
    }

    public virtual Quaternion GetPoseSpin(out Vector3 vPivot)
    {
        vPivot = Vector3.zero;
        return Quaternion.identity;
    }

    public void Attach(Transform hand)
    {
        if(View != null)
        {
            View.transform.SetParent(hand, false);
            View.SetActive(true);
        }
        OnAttach(hand);
    }

    public void Detach()
    {
        if(View != null)
        {
            View.SetActive(false);
        }
        OnDetach();
    }

    protected virtual void OnAttach(Transform hand) {}
    protected virtual void OnDetach() {}

    public readonly EquippableObject Item;
    public GameObject View;
    public bool bTapPending;
    public float fPressTime;
    protected readonly IWielder Wielder;
}

public abstract class EquippableObject : ScriptableObject
{
    public string sId; // to int32
    public string sDisplayName;

    [Tooltip("The model held in hand at FPS time")]
    public GameObject ViewModel;
    [Tooltip("The model shown in the bar: on the armory wall, in the bag and on the workbench. The ViewModel is used when left empty.")]
    public GameObject DisplayModel;

    [Header("Hand")]
    public Vector3 vHandOffset = new(0.0f, -0.193f, 0.097f);
    public Vector3 vHandRotation;

    [Header("Equip")]
    public bool bTwoHanded;
    public InputActionReference EquipAction;
    [Tooltip("Pressing the bind again while this is in hand: put it away, or use it (e.g. flashlight on/off).")]
    public EquipRepress eRepress = EquipRepress.Holster;
    [Tooltip("With Use: hold the bind this long to put it away instead")]
    public float fHoldToHolster = 0.4f;
    [Tooltip("Right click while this is the other hand's item: let the gun aim, or use this instead (e.g. fire a second gun)")]
    [FormerlySerializedAs("eOffhandButton")]
    public SecondaryButton eSecondaryButton = SecondaryButton.Use;
    public float fDrawTime = 0.15f;
    public float fLowerDistance = 0.35f;
    public Sound DrawSound = new();
    public Sound HolsterSound = new();

    [Header("Bag")]
    [Tooltip("Cells it takes in the bag at the loadout bench, drawn as the bench camera sees it. Left at 0 x 0, it's the box around the model.")]
    public BagShape BagShape = new();

    public GameObject PresentationModel => DisplayModel != null ? DisplayModel : ViewModel;

    public Transform SpawnPresentation(Transform parent)
    {
        Transform model = Instantiate(PresentationModel, parent, false).transform;

        // Only the look is wanted here, not the lights or scripts meant for the hand.
        foreach(Behaviour behaviour in model.GetComponentsInChildren<Behaviour>())
        {
            behaviour.enabled = false;
        }

        return model;
    }

#if UNITY_EDITOR
    // Lets the bag redraw while a shape is being drawn in play mode.
    public static event System.Action OnEdited;

    protected virtual void OnValidate()
    {
        OnEdited?.Invoke();
    }
#endif

    public HeldItem Materialize(Transform root, IWielder wielder)
    {
        HeldItem held = CreateHeld(wielder);
        if(ViewModel != null)
        {
            held.View = Instantiate(ViewModel, root);
            held.View.SetActive(false);
        }
        return held;
    }

    protected abstract HeldItem CreateHeld(IWielder wielder);
}
