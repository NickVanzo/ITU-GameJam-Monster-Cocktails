using UnityEngine;

public class ArmoryDisplay : MonoBehaviour
{
    static readonly int TintId = Shader.PropertyToID("_Tint");
    static readonly int EmissiveColorId = Shader.PropertyToID("_EmissiveColor");

    [System.Serializable]
    public class Gun
    {
        public string sName;
        [Tooltip("Spins on its Y axis. Centre the gun model on it.")]
        public Transform Pivot;
        public bool bUnlocked = true;
        [Tooltip("Stats and attachment slots. This is what the player takes down the elevator when the gun is selected.")]
        public GunObject Definition;
    }

    public Player Player;
    public Gun[] aGuns;

    [SerializeField] float fSpinSpeed = 60.0f;

    int m_nSelected;

    public Gun SelectedGun => aGuns.Length > 0 ? aGuns[m_nSelected] : null;
    public bool HasValidSelection => SelectedGun != null && SelectedGun.bUnlocked;

    void Awake()
    {
        foreach(Gun gun in aGuns)
        {
            if(!gun.bUnlocked)
            {
                SetSilhouette(gun);
            }
        }

        int nTaken = System.Array.FindIndex(aGuns, gun => gun.Definition != null && gun.Definition == Loadout.Frame);
        Select(Mathf.Max(nTaken, 0));
    }

    void Update()
    {
        if(Player.CurrentRoom == Rooms.Kitchen)
        {
            HandleScrollKeys();
        }

        if(SelectedGun != null)
        {
            SelectedGun.Pivot.Rotate(0.0f, fSpinSpeed * Time.deltaTime, 0.0f);
        }
    }

    void HandleScrollKeys()
    {
        bool bUp = Input.GetKeyDown(KeyCode.W) || Input.GetKeyDown(KeyCode.UpArrow);
        bool bDown = Input.GetKeyDown(KeyCode.S) || Input.GetKeyDown(KeyCode.DownArrow);
        int nStep = (bDown ? 1 : 0) - (bUp ? 1 : 0);

        if(nStep != 0)
        {
            Step(nStep);
        }
    }
    public void Step(int nStep)
    {
        if(aGuns.Length > 1)
        {
            Select((m_nSelected + nStep + aGuns.Length) % aGuns.Length);
            Sfx.Play(Sfx.Sounds.GunSelect);
        }
    }

    void Select(int nIndex)
    {
        m_nSelected = nIndex;
        for(int i = 0; i < aGuns.Length; i++)
        {
            aGuns[i].Pivot.gameObject.SetActive(i == m_nSelected);
        }

        if(HasValidSelection && SelectedGun.Definition != null)
        {
            Loadout.Frame = SelectedGun.Definition;
        }
    }

    static void SetSilhouette(Gun gun)
    {
        MaterialPropertyBlock block = new();
        block.SetColor(TintId, Color.black);
        block.SetColor(EmissiveColorId, Color.black);

        foreach(Renderer renderer in gun.Pivot.GetComponentsInChildren<Renderer>(true))
        {
            renderer.SetPropertyBlock(block);
        }
    }
}
