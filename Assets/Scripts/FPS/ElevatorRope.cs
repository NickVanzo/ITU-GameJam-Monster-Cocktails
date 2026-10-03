using UnityEngine;

public class ElevatorRope : MonoBehaviour
{
    public FPSLevel Level;
    public bool bDescend = true;
    public Transform Rope;
    public Transform Handle;

    [SerializeField] float fRopeLength = 1.2f;
    [SerializeField] float fRopeThickness = 0.03f;
    [SerializeField] float fMaxPull = 0.45f;
    [SerializeField] float fHoldPullSpeed = 1.2f;
    [SerializeField] float fReturnSpeed = 2.5f;

    float m_fPull;
    float m_fLastHoldTime = float.NegativeInfinity;
    bool bDragging;
    bool bTriggered;
    Plane m_DragPlane;
    float m_fDragStartY;
    float m_fDragStartPull;

    public void BeginDrag()
    {
        Camera camera = Camera.main;
        if(camera == null)
        {
            return;
        }

        m_DragPlane = new Plane(-camera.transform.forward, Handle.position);
        if(!TryGetCursorHeight(camera, out m_fDragStartY))
        {
            return;
        }

        m_fDragStartPull = m_fPull;
        bDragging = true;
    }

    public void Hold()
    {
        m_fPull = Mathf.MoveTowards(m_fPull, fMaxPull, fHoldPullSpeed * Time.deltaTime);
        m_fLastHoldTime = Time.time;
    }

    public void Update()
    {
        if(bDragging)
        {
            UpdateDrag();
        }
        else if(Time.time - m_fLastHoldTime > 0.1f)
        {
            m_fPull = Mathf.MoveTowards(m_fPull, 0.0f, fReturnSpeed * Time.deltaTime);
        }

        if(!bTriggered && m_fPull >= fMaxPull)
        {
            bTriggered = true;
            bDragging = false;
            Trigger();
        }
        else if(m_fPull <= 0.0f)
        {
            bTriggered = false;
        }

        Refresh();
    }

    public void Refresh()
    {
        float fLength = fRopeLength + m_fPull;
        Rope.localPosition = new Vector3(0.0f, -fLength * 0.5f, 0.0f);
        Rope.localScale = new Vector3(fRopeThickness, fLength * 0.5f, fRopeThickness);
        Handle.localPosition = new Vector3(0.0f, -fLength - Handle.localScale.y * 0.5f, 0.0f);
    }

    void UpdateDrag()
    {
        Camera camera = Camera.main;
        if(camera == null || !Input.GetMouseButton(0) || !TryGetCursorHeight(camera, out float fHeight))
        {
            bDragging = false;
            return;
        }

        m_fPull = Mathf.Clamp(m_fDragStartPull + m_fDragStartY - fHeight, 0.0f, fMaxPull);
    }

    bool TryGetCursorHeight(Camera camera, out float fHeight)
    {
        Ray ray = camera.ScreenPointToRay(Input.mousePosition);
        if(m_DragPlane.Raycast(ray, out float fEnter))
        {
            fHeight = ray.GetPoint(fEnter).y;
            return true;
        }

        fHeight = 0.0f;
        return false;
    }

    void Trigger()
    {
        if(bDescend)
        {
            Level.Enter();
        }
        else
        {
            Level.Leave();
        }
    }
}
