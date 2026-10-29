using System.Collections.Generic;
using UnityEngine;

public class LoadoutConstraint : MonoBehaviour
{
    static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
    static readonly int TintId = Shader.PropertyToID("_Tint");
    static readonly int EmissiveColorId = Shader.PropertyToID("_EmissiveColor");

    class Packed
    {
        public EquippableObject Item;
        public Transform Holder;
        public Renderer[] aRenderers;
        // Box around the shape before turning
        public Vector2Int vSize;
        public List<Vector2Int> aShape;
        public BagSlot Slot;

        public Vector2Int Footprint => Slot.nTurns % 2 == 0 ? vSize : new Vector2Int(vSize.y, vSize.x);

        // Grid cell one of the shape's cells lands on, once turned.
        public Vector2Int CellOf(Vector2Int vShapeCell)
        {
            Vector2Int vBox = vSize;
            for(int i = 0; i < Slot.nTurns; i++)
            {
                // A quarter clockwise, with y running down
                vShapeCell = new Vector2Int(vBox.y - 1 - vShapeCell.y, vShapeCell.x);
                vBox = new Vector2Int(vBox.y, vBox.x);
            }
            return Slot.vCell + vShapeCell;
        }

        public bool Covers(Vector2Int vCell)
        {
            foreach(Vector2Int vShapeCell in aShape)
            {
                if(CellOf(vShapeCell) == vCell)
                {
                    return true;
                }
            }
            return false;
        }
    }

    public Vector2Int mConstraintResolution;
    public GameObject PreviewSlot;
    public float fPreviewScale = 1.0f;
    public float fPivotScale = 1.0f;

    [Header("Packing")]
    [Tooltip("Scales the item models in the bag. Without a BagShape on the item, it changes how many cells they take too.")]
    public float fModelScale = 1.0f;
    [SerializeField] float fDragLift = 0.15f;
    [SerializeField] float fFollowSpeed = 20.0f;

    [Header("Cells")]
    public Color EmptyColor = new(0.2f, 0.2f, 0.2f);
    public Color FitColor = new(0.25f, 0.85f, 0.3f);
    public Color BlockedColor = new(0.9f, 0.2f, 0.15f);

    readonly List<Packed> m_aPacked = new();
    readonly List<EquippableObject> m_aCarried = new();
    Renderer[,] m_aCells;
    int[,] m_aCounts;
    // Which way cell x and y run along this transform's x and z
    Vector2 m_vAxes = Vector2.one;
    float m_fCellTop;
    int m_nSpill;
    bool bShapeEdited;
    Packed m_Dragged;
    EquippableObject m_Held;
    bool bPlacing;
    bool bPacking;
    BagSlot m_HeldFrom;
    LoadoutBench m_Bench;
    MaterialPropertyBlock m_CellBlock;
    MaterialPropertyBlock m_TintBlock;

    public float CellPitch => fPreviewScale * fPivotScale;

#if UNITY_EDITOR
    void OnEnable()
    {
        EquippableObject.OnEdited += HandleItemEdited;
    }

    void OnDisable()
    {
        EquippableObject.OnEdited -= HandleItemEdited;
    }

    void HandleItemEdited()
    {
        bShapeEdited = true;
    }
#endif

    void Start()
    {
        m_Bench = GetComponentInParent<LoadoutBench>();
        m_CellBlock = new MaterialPropertyBlock();
        m_TintBlock = new MaterialPropertyBlock();
        m_TintBlock.SetColor(TintId, BlockedColor);
        m_TintBlock.SetColor(EmissiveColorId, BlockedColor);

        FaceBenchCamera();
        BuildCells();

        Loadout.GetCarried(m_aCarried);
        Restock();
    }

    public void Hold(EquippableObject item)
    {
        m_Held = item;
    }

    void TakeHeld()
    {
        m_Dragged = m_aPacked.Find(packed => packed.Item == m_Held);
        m_Held = null;
        if(m_Dragged != null)
        {
            m_HeldFrom = m_Dragged.Slot;
            bPlacing = true;
            bPacking = true;
        }
    }

    // Numbers the cells left to right and top to bottom as the bench camera sees them, the same way shapes are drawn.
    void FaceBenchCamera()
    {
        Transform view = m_Bench != null && m_Bench.Spot != null ? m_Bench.Spot.Anchor : null;
        if(view == null)
        {
            return;
        }

        m_vAxes = new Vector2(Vector3.Dot(view.right, transform.right) < 0.0f ? -1.0f : 1.0f,
                              Vector3.Dot(-view.up, transform.forward) < 0.0f ? -1.0f : 1.0f);
    }

    void BuildCells()
    {
        m_aCells = new Renderer[mConstraintResolution.x, mConstraintResolution.y];
        m_aCounts = new int[mConstraintResolution.x, mConstraintResolution.y];

        for(int x = 0; x<mConstraintResolution.x; x++)
        {
            for(int y = 0; y<mConstraintResolution.y; y++)
            {
                var cellGO = Instantiate(PreviewSlot, transform);
                cellGO.transform.localPosition = CellToLocal(new Vector2(x, y));
                cellGO.transform.localScale *= fPreviewScale;
                m_aCells[x, y] = cellGO.GetComponentInChildren<Renderer>();

                if(x == 0 && y == 0)
                {
                    m_fCellTop = MeshUtil.MeshBounds(cellGO.transform, transform).max.y;
                }
            }
        }
    }

    // Centre of a cell in this transform's space. Fractions and cells outside the grid work too.
    public Vector3 CellToLocal(Vector2 vCell)
    {
        return new Vector3(m_vAxes.x * (vCell.x - (mConstraintResolution.x - 1) * 0.5f), 0.0f, m_vAxes.y * (vCell.y - (mConstraintResolution.y - 1) * 0.5f)) * CellPitch;
    }

    Vector2 LocalToCell(Vector3 vLocal)
    {
        return new Vector2(m_vAxes.x * vLocal.x / CellPitch + (mConstraintResolution.x - 1) * 0.5f, m_vAxes.y * vLocal.z / CellPitch + (mConstraintResolution.y - 1) * 0.5f);
    }

    // Cell under a world position, possibly outside the grid.
    public Vector2Int GetCell(Vector3 vWorld)
    {
        return Vector2Int.RoundToInt(LocalToCell(transform.InverseTransformPoint(vWorld)));
    }

    bool InGrid(Vector2Int vCell)
    {
        return vCell.x >= 0 && vCell.y >= 0 && vCell.x < mConstraintResolution.x && vCell.y < mConstraintResolution.y;
    }

    void Update()
    {
        // The armory can swap the gun while the bar is loaded.
        Loadout.GetCarried(m_aCarried);
        if(CarriedChanged() || bShapeEdited)
        {
            bShapeEdited = false;
            Restock();
        }

        if(m_Bench != null && m_Bench.IsOpen())
        {
            HandleInput();
        }
        else
        {
            if(m_Dragged != null)
            {
                if(bPlacing)
                {
                    m_Dragged.Slot = m_HeldFrom;
                }
                Drop();
            }
            bPacking = false;
        }

        foreach(Packed packed in m_aPacked)
        {
            Follow(packed, false);
        }
    }

    bool CarriedChanged()
    {
        if(m_aCarried.Count != m_aPacked.Count)
        {
            return true;
        }

        for(int i = 0; i < m_aCarried.Count; i++)
        {
            if(m_aCarried[i] != m_aPacked[i].Item)
            {
                return true;
            }
        }

        return false;
    }

    // Lays out everything carried where Loadout.Packing says, packing anything new into the first free spot.
    void Restock()
    {
        foreach(Packed packed in m_aPacked)
        {
            Destroy(packed.Holder.gameObject);
        }
        m_aPacked.Clear();
        m_Dragged = null;
        bPlacing = false;
        m_nSpill = 0;

        foreach(EquippableObject item in m_aCarried)
        {
            m_aPacked.Add(Spawn(item));
        }

        foreach(Packed packed in m_aPacked)
        {
            if(!Loadout.Packing.TryGetValue(packed.Item, out packed.Slot))
            {
                packed.Slot = FindFreeSlot(packed);
                Loadout.Packing[packed.Item] = packed.Slot;
            }

            Follow(packed, true);
        }

        Validate();
    }

    // The cells it takes come from the item's BagShape, or the box around its model when it has none.
    Packed Spawn(EquippableObject item)
    {
        var holder = new GameObject(item.name).transform;
        holder.SetParent(transform, false);
        var shape = new BagShape { vSize = Vector2Int.one };
        var packed = new Packed { Item = item, Holder = holder, aRenderers = new Renderer[0] };

        if(item.PresentationModel != null)
        {
            Transform model = item.SpawnPresentation(holder);
            model.localScale *= fModelScale;
            MeshUtil.LayFlat(model, holder);

            // Centred over its cells, resting on top of them.
            Bounds bounds = MeshUtil.MeshBounds(model, holder);
            model.localPosition -= new Vector3(bounds.center.x, bounds.min.y, bounds.center.z);
            shape.vSize = new Vector2Int(CellsFor(bounds.size.x), CellsFor(bounds.size.z));
            packed.aRenderers = model.GetComponentsInChildren<Renderer>();
        }

        if(item.BagShape != null && item.BagShape.IsSet)
        {
            shape = item.BagShape;
        }

        packed.vSize = shape.vSize;
        packed.aShape = shape.TakenCells();
        return packed;
    }

    int CellsFor(float fSize)
    {
        return Mathf.Max(1, Mathf.CeilToInt(fSize / CellPitch - 0.01f));
    }

    BagSlot FindFreeSlot(Packed packed)
    {
        CountCells();
        for(int nTurns = 0; nTurns < 4; nTurns++)
        {
            for(int y = 0; y < mConstraintResolution.y; y++)
            {
                for(int x = 0; x < mConstraintResolution.x; x++)
                {
                    packed.Slot = new BagSlot { vCell = new Vector2Int(x, y), nTurns = nTurns };
                    if(IsFree(packed))
                    {
                        return packed.Slot;
                    }
                }
            }
        }

        // No room: lay it out in front of the bag, side by side with anything else left out.
        packed.Slot = new BagSlot { vCell = new Vector2Int(m_nSpill, mConstraintResolution.y + 1) };
        m_nSpill += packed.vSize.x + 1;
        return packed.Slot;
    }

    bool IsFree(Packed packed)
    {
        foreach(Vector2Int vShapeCell in packed.aShape)
        {
            Vector2Int vCell = packed.CellOf(vShapeCell);
            if(!InGrid(vCell) || m_aCounts[vCell.x, vCell.y] > 0)
            {
                return false;
            }
        }

        return true;
    }

    void HandleInput()
    {
        if(m_Held != null)
        {
            TakeHeld();
        }

        if(bPlacing && Input.GetMouseButtonDown(0))
        {
            bPlacing = false;
        }

        // Checked before the cursor, so letting go with the cursor off the bag still drops it.
        if(m_Dragged != null && !bPlacing && !Input.GetMouseButton(0))
        {
            Packed dropped = m_Dragged;
            Drop();
            if(!dropped.aShape.Exists(vShapeCell => InGrid(dropped.CellOf(vShapeCell))))
            {
                Loadout.Remove(dropped.Item);
                Loadout.GetCarried(m_aCarried);
                Restock();
            }

            if(bPacking && Loadout.bFitsBag)
            {
                m_Bench.Close();
            }
            return;
        }

        if(!TryGetCursorCell(out Vector2 vCursor))
        {
            return;
        }

        bool bTurn = Input.GetKeyDown(KeyCode.R) || Input.GetMouseButtonDown(1);

        if(m_Dragged == null)
        {
            Packed hovered = ItemAt(Vector2Int.RoundToInt(vCursor));
            if(hovered == null)
            {
                return;
            }

            if(Input.GetMouseButtonDown(0))
            {
                m_Dragged = hovered;
            }
            else if(bTurn)
            {
                Turn(hovered);
                Validate();
            }
            return;
        }

        if(bTurn)
        {
            Turn(m_Dragged);
        }

        // Keeps the cursor over the middle of the item.
        Vector2 vHalf = ((Vector2)m_Dragged.Footprint - Vector2.one) * 0.5f;
        m_Dragged.Slot.vCell = Vector2Int.RoundToInt(vCursor - vHalf);
        Validate();
    }

    static void Turn(Packed packed)
    {
        packed.Slot.nTurns = (packed.Slot.nTurns + 1) % 4;
    }

    void Drop()
    {
        m_Dragged = null;
        bPlacing = false;
        Validate();
    }

    // Fractional cell under the mouse, on the plane the items rest on.
    bool TryGetCursorCell(out Vector2 vCell)
    {
        vCell = default;
        Camera camera = Camera.main;
        if(camera == null)
        {
            return false;
        }

        Ray ray = camera.ScreenPointToRay(Input.mousePosition);
        Plane plane = new(transform.up, transform.TransformPoint(Vector3.up * m_fCellTop));
        if(!plane.Raycast(ray, out float fDistance))
        {
            return false;
        }

        vCell = LocalToCell(transform.InverseTransformPoint(ray.GetPoint(fDistance)));
        return true;
    }

    // The last one laid out wins where items overlap, same as the one picked up last.
    Packed ItemAt(Vector2Int vCell)
    {
        for(int i = m_aPacked.Count - 1; i >= 0; i--)
        {
            if(m_aPacked[i].Covers(vCell))
            {
                return m_aPacked[i];
            }
        }

        return null;
    }

    void CountCells()
    {
        System.Array.Clear(m_aCounts, 0, m_aCounts.Length);
        foreach(Packed packed in m_aPacked)
        {
            if(!Loadout.Packing.ContainsKey(packed.Item))
            {
                continue;
            }

            foreach(Vector2Int vShapeCell in packed.aShape)
            {
                Vector2Int vCell = packed.CellOf(vShapeCell);
                if(InGrid(vCell))
                {
                    m_aCounts[vCell.x, vCell.y]++;
                }
            }
        }
    }

    // Colours each cell by how many items cover it, tints items that stick out or overlap, and saves the packing.
    void Validate()
    {
        CountCells();

        Loadout.Unfit.Clear();
        foreach(Packed packed in m_aPacked)
        {
            bool bFits = Fits(packed);
            if(!bFits)
            {
                Loadout.Unfit.Add(packed.Item);
            }
            Loadout.Packing[packed.Item] = packed.Slot;

            foreach(Renderer renderer in packed.aRenderers)
            {
                renderer.SetPropertyBlock(bFits ? null : m_TintBlock);
            }
        }

        for(int x = 0; x < mConstraintResolution.x; x++)
        {
            for(int y = 0; y < mConstraintResolution.y; y++)
            {
                int nCount = m_aCounts[x, y];
                m_CellBlock.SetColor(BaseColorId, nCount == 0 ? EmptyColor : nCount == 1 ? FitColor : BlockedColor);
                m_aCells[x, y].SetPropertyBlock(m_CellBlock);
            }
        }
    }

    bool Fits(Packed packed)
    {
        foreach(Vector2Int vShapeCell in packed.aShape)
        {
            Vector2Int vCell = packed.CellOf(vShapeCell);
            if(!InGrid(vCell) || m_aCounts[vCell.x, vCell.y] > 1)
            {
                return false;
            }
        }

        return true;
    }

    // Turning goes clockwise as the bench camera sees it, which is a positive turn around up from above.
    void Follow(Packed packed, bool bSnap)
    {
        Vector2 vCentre = packed.Slot.vCell + ((Vector2)packed.Footprint - Vector2.one) * 0.5f;
        float fLift = packed == m_Dragged ? fDragLift : 0.0f;
        Vector3 vTarget = CellToLocal(vCentre) + Vector3.up * (m_fCellTop + fLift);
        Quaternion qTarget = Quaternion.Euler(0.0f, 90.0f * packed.Slot.nTurns, 0.0f);

        float t = bSnap ? 1.0f : 1.0f - Mathf.Exp(-fFollowSpeed * Time.deltaTime);
        packed.Holder.GetLocalPositionAndRotation(out Vector3 vPosition, out Quaternion qRotation);
        packed.Holder.SetLocalPositionAndRotation(Vector3.Lerp(vPosition, vTarget, t), Quaternion.Slerp(qRotation, qTarget, t));
    }
}
