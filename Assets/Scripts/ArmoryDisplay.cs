using UnityEngine;

// Shows the guns in the armory one at a time, spinning on their pivot. Up/Down (or W/S) scrolls through them
// while the camera is in the armory. Locked guns show as a black silhouette.
// The selected gun is the one the player takes down the elevator, so it has to be unlocked.
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

        Select(0);
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

        if(nStep != 0 && aGuns.Length > 1)
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
    }

    // DefaultOpaque shades as Tint * Texture * (ShadowColor + light) + Emissive, so black tint and emission is pure black.
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
