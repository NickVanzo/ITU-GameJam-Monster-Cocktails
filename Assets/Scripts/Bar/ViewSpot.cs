using UnityEngine;
using UnityEngine.Events;

public class ViewSpot : MonoBehaviour
{
    public Rooms eRoom;
    public Transform CameraAnchor;
    public ViewSpot Entry;
    // TODO : This is kind of a vague shit compromise
    public bool bEngageable;
    public UnityEvent OnFocus;
    public UnityEvent OnUnfocus;

    public Transform Anchor => CameraAnchor != null ? CameraAnchor : transform;
    public ViewSpot Parent => transform.parent != null ? transform.parent.GetComponent<ViewSpot>() : null;
    public Rooms Room => Parent != null ? Parent.Room : eRoom;

    public ViewSpot Neighbour(BarMovementDirections eDirection)
    {
        switch(eDirection)
        {
            case BarMovementDirections.Up:
                return Entry != null ? Entry : FindChild(transform, 0, 1);
            case BarMovementDirections.Down:
                return Parent;
            default:
                int nStep = eDirection == BarMovementDirections.Right ? 1 : -1;
                return transform.parent != null ? FindChild(transform.parent, transform.GetSiblingIndex() + nStep, nStep) : null;
        }
    }

    static ViewSpot FindChild(Transform parent, int nFrom, int nStep)
    {
        for(int i = nFrom; i >= 0 && i < parent.childCount; i += nStep)
        {
            if(parent.GetChild(i).TryGetComponent(out ViewSpot spot))
            {
                return spot;
            }
        }

        return null;
    }

    void OnDrawGizmos()
    {
        ViewSpot next = Neighbour(BarMovementDirections.Right);
        ViewSpot parent = Parent;

        Gizmos.color = Color.cyan;
        if(next != null)
        {
            Gizmos.DrawLine(Anchor.position, next.Anchor.position);
        }

        if(parent != null)
        {
            Gizmos.DrawLine(Anchor.position, parent.Anchor.position);
        }

        Gizmos.matrix = Anchor.localToWorldMatrix;
        Gizmos.DrawFrustum(Vector3.zero, 40.0f, 0.5f, 0.05f, 16.0f / 9.0f);
        Gizmos.matrix = Matrix4x4.identity;
    }
}
