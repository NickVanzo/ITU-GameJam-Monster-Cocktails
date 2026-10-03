using UnityEngine;

public class LightFlicker : MonoBehaviour
{
    public Light Light;
    public float fMin = 0.85f;
    public float fMax = 1.0f;
    public float fSpeed = 5.0f;

    private float m_fStartIntensity;
    private float m_fSeed;

    void Awake()
    {
        if(Light == null)
        {
            Light = GetComponent<Light>();
        }

        m_fStartIntensity = Light.intensity;

        // Picked once so each light flickers differently, instead of re-rolling every frame.
        m_fSeed = Random.Range(0.0f, 100.0f);
    }

    void Update()
    {
        float t = Mathf.PerlinNoise(m_fSeed, Time.time * fSpeed);
        Light.intensity = m_fStartIntensity * Mathf.Lerp(fMin, fMax, t);
    }
}
