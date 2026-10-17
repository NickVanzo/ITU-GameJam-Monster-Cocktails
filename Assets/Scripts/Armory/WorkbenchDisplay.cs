using System.Collections;
using UnityEngine;

public class WorkbenchDisplay : MonoBehaviour
{
    [Tooltip("Upward speed of the hop, in m/s.")]
    [SerializeField] float fHopSpeed = 1.0f;
    [Tooltip("Spin around a random axis during the hop, in radians per second.")]
    [SerializeField] float fHopSpin = 1.5f;
    [Tooltip("Time to slide back to the spot before hopping.")]
    [SerializeField] float fReturnDuration = 0.12f;
    [Tooltip("Put back on its spot when it ends up this far below it, e.g. after rolling off the table.")]
    [SerializeField] float fFallDistance = 1.0f;

    [SerializeField] GunObject m_Frame;
    [SerializeField] GameObject PlaceHolderModel;
    public PhysicsMaterial GunDropMaterial;
    Rigidbody m_Body;
    Coroutine m_Pulse;

    public void Show(GunObject frame)
    {
        if(frame == m_Frame && m_Body != null)
        {
            return;
        }

        if(m_Pulse != null)
        {
            StopCoroutine(m_Pulse);
            m_Pulse = null;
        }

        if(m_Body != null)
        {
            m_Body.gameObject.SetActive(false);
            Destroy(m_Body.gameObject);
            m_Body = null;
        }

        m_Frame = frame;
        GameObject model;

        if(frame == null || frame.DisplayModel == null)
        {
            model = Instantiate(PlaceHolderModel, transform.position, transform.rotation, transform);
        }
        else
        {
            model = Instantiate(frame.DisplayModel, transform.position, transform.rotation, transform);
        }

        AddColliderIfMissing(model);

        if(!model.TryGetComponent(out m_Body))
        {
            m_Body = model.AddComponent<Rigidbody>();
        }
        m_Body.interpolation = RigidbodyInterpolation.Interpolate;
        m_Body.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;

    }

    public void Pulse()
    {
        if(m_Body == null)
        {
            return;
        }

        if(m_Pulse != null)
        {
            StopCoroutine(m_Pulse);
        }

        // TODO : No coroutines here
        m_Pulse = StartCoroutine(PulseRoutine());
    }

    IEnumerator PulseRoutine()
    {
        m_Body.isKinematic = true;
        Vector3 vFrom = m_Body.position;
        Quaternion qFrom = m_Body.rotation;

        for(float t = 0.0f; t < 1.0f; t += Time.fixedDeltaTime / fReturnDuration)
        {
            float fBlend = Mathf.SmoothStep(0.0f, 1.0f, t);
            m_Body.MovePosition(Vector3.Lerp(vFrom, transform.position, fBlend));
            m_Body.MoveRotation(Quaternion.Slerp(qFrom, transform.rotation, fBlend));
            yield return new WaitForFixedUpdate();
        }

        m_Body.isKinematic = false;
        m_Body.position = transform.position;
        m_Body.rotation = transform.rotation;
        m_Body.linearVelocity = Vector3.up * fHopSpeed;
        m_Body.angularVelocity = Random.onUnitSphere * fHopSpin;
        m_Body.constraints = RigidbodyConstraints.FreezePositionX | RigidbodyConstraints.FreezePositionZ;
        m_Pulse = null;
    }

    void FixedUpdate()
    {
        if(m_Body != null && m_Pulse == null && m_Body.position.y < transform.position.y - fFallDistance)
        {
            Pulse();
        }
    }

    void OnDisable()
    {
        // Disabling stops the pulse, which would leave the gun kinematic and floating.
        m_Pulse = null;
        if(m_Body != null)
        {
            m_Body.isKinematic = false;
        }
    }

    void AddColliderIfMissing(GameObject model)
    {
        if(model.GetComponentInChildren<Collider>() != null)
        {
            return;
        }

        Transform root = model.transform;
        Bounds bounds = default;
        bool bAny = false;

        foreach(Renderer renderer in model.GetComponentsInChildren<Renderer>())
        {
            // TODO: Bake this
            Bounds local = renderer.localBounds;
            for(int i = 0; i < 8; ++i)
            {
                Vector3 vCorner = local.center + Vector3.Scale(local.extents, new Vector3((i & 1) == 0 ? -1 : 1, (i & 2) == 0 ? -1 : 1, (i & 4) == 0 ? -1 : 1));
                Vector3 vInRoot = root.InverseTransformPoint(renderer.transform.TransformPoint(vCorner));

                if(bAny)
                {
                    bounds.Encapsulate(vInRoot);
                }
                else
                {
                    bounds = new Bounds(vInRoot, Vector3.zero);
                    bAny = true;
                }
            }
        }

        if(bAny)
        {
            BoxCollider box = model.AddComponent<BoxCollider>();
            box.center = bounds.center;
            box.size = bounds.size;
            box.material = GunDropMaterial;
        }
    }
}
