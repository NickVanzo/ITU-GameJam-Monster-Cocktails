using UnityEngine;

public class MaterialPickup : MonoBehaviour
{
    public IngredientType Type;
    public int nCount = 1;
    [SerializeField] float fCollectRadius = 1.2f;
    [SerializeField] float fMagnetRadius = 5.0f;
    [SerializeField] float fMagnetSpeed = 14.0f;
    [SerializeField] float fSpinSpeed = 180.0f;
    [SerializeField] float fBobHeight = 0.12f;

    Spawner m_Spawner;
    Vector3 m_vBasePosition;
    bool bMagnetized;

    public void Init(IngredientType type, int count, Spawner spawner)
    {
        Type = type;
        nCount = count;
        m_Spawner = spawner;
        m_vBasePosition = transform.position;
    }

    public void Update()
    {
        transform.Rotate(0.0f, fSpinSpeed * Time.deltaTime, 0.0f, Space.World);

        FPSController target = m_Spawner != null ? m_Spawner.Target : null;
        if(target == null || !target.CanMove)
        {
            Bob();
            return;
        }

        Vector3 vTarget = target.transform.position + Vector3.up * 0.9f;
        float fDistance = Vector3.Distance(transform.position, vTarget);

        if(fDistance <= fCollectRadius)
        {
            m_Spawner.NotifyPickupCollected(this);
            return;
        }

        if(bMagnetized || fDistance <= fMagnetRadius)
        {
            bMagnetized = true;
            transform.position = Vector3.MoveTowards(transform.position, vTarget, fMagnetSpeed * Time.deltaTime);
            return;
        }

        Bob();
    }

    void Bob()
    {
        if(!bMagnetized)
        {
            transform.position = m_vBasePosition + Vector3.up * (Mathf.Sin(Time.time * 3.0f) * fBobHeight);
        }
    }
}
