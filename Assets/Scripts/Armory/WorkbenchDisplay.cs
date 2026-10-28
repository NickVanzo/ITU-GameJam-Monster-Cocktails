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
    Vector3 m_vRestPosition;

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
        GameObject prefab = frame != null && frame.DisplayModel != null ? frame.DisplayModel : PlaceHolderModel;

        var holder = new GameObject(prefab.name);
        holder.transform.SetParent(transform, false);

        Transform model = Instantiate(prefab, holder.transform, false).transform;
        Bounds bounds = MeshUtil.MeshBounds(model, holder.transform);
        model.localPosition -= bounds.center;

        // todo : we can bake collider within the preview prefab
        if(model.GetComponentInChildren<Collider>() == null)
        {
            BoxCollider box = holder.AddComponent<BoxCollider>();
            box.size = bounds.size;
            box.material = GunDropMaterial;
        }

        holder.transform.localPosition = Vector3.up * bounds.extents.y;
        m_vRestPosition = holder.transform.position;

        m_Body = holder.AddComponent<Rigidbody>();
        m_Body.interpolation = RigidbodyInterpolation.Interpolate;
        m_Body.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;
        m_Body.constraints = RigidbodyConstraints.FreezePositionX | RigidbodyConstraints.FreezePositionZ;
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
            m_Body.MovePosition(Vector3.Lerp(vFrom, m_vRestPosition, fBlend));
            m_Body.MoveRotation(Quaternion.Slerp(qFrom, transform.rotation, fBlend));
            yield return new WaitForFixedUpdate();
        }

        m_Body.isKinematic = false;
        m_Body.position = m_vRestPosition;
        m_Body.rotation = transform.rotation;
        m_Body.linearVelocity = Vector3.up * fHopSpeed;
        m_Body.angularVelocity = Random.onUnitSphere * fHopSpin;
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
}
