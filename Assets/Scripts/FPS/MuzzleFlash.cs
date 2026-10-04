using UnityEngine;

// Muzzle flash built in code, so there's no particle prefab to maintain: a soft blob, a burst of sparks
// and a spot light that flicks on for a few frames. Fires along this object's forward axis.
public class MuzzleFlash : MonoBehaviour
{
    [SerializeField] Material ParticleMaterial;
    [SerializeField] Color FlashColor = new(1.0f, 0.7f, 0.3f);

    [Header("Particles")]
    [SerializeField] float fFlashSize = 0.12f;
    [SerializeField] float fFlashLifetime = 0.05f;
    [SerializeField] int nSparks = 10;
    [SerializeField] Vector2 vSparkSpeed = new(4.0f, 9.0f);
    [SerializeField] Vector2 vSparkSize = new(0.02f, 0.05f);
    [SerializeField] float fSparkLifetime = 0.12f;
    [SerializeField] float fSparkCone = 15.0f;

    [Header("Light")]
    [SerializeField] float fLightIntensity = 20.0f;
    [SerializeField] float fLightRange = 15.0f;
    [SerializeField] float fLightSpotAngle = 90.0f;
    [SerializeField] float fLightDuration = 0.06f;

    ParticleSystem m_Particles;
    Light m_Light;
    float m_fLightTimer;
    float m_fLightPeak;

    void Awake()
    {
        BuildParticles();
        BuildLight();
    }

    public void Play()
    {
        var flash = new ParticleSystem.EmitParams
        {
            position = transform.position,
            applyShapeToPosition = false,
            velocity = Vector3.zero,
            startSize = fFlashSize,
            startLifetime = fFlashLifetime,
            startColor = FlashColor,
        };
        m_Particles.Emit(flash, 1);
        m_Particles.Emit(nSparks);

        m_fLightPeak = fLightIntensity * Random.Range(0.8f, 1.2f);
        m_fLightTimer = fLightDuration;
        m_Light.intensity = m_fLightPeak;
        m_Light.enabled = true;
    }

    void Update()
    {
        if(!m_Light.enabled)
        {
            return;
        }

        m_fLightTimer -= Time.deltaTime;
        if(m_fLightTimer <= 0.0f)
        {
            m_Light.enabled = false;
            return;
        }

        m_Light.intensity = m_fLightPeak * (m_fLightTimer / fLightDuration);
    }

    void BuildParticles()
    {
        m_Particles = gameObject.AddComponent<ParticleSystem>();
        m_Particles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

        // Nothing is emitted on its own, every particle comes from Play().
        ParticleSystem.MainModule main = m_Particles.main;
        main.playOnAwake = false;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.startLifetime = fSparkLifetime;
        main.startSpeed = new ParticleSystem.MinMaxCurve(vSparkSpeed.x, vSparkSpeed.y);
        main.startSize = new ParticleSystem.MinMaxCurve(vSparkSize.x, vSparkSize.y);
        main.startColor = FlashColor;
        main.maxParticles = 64;

        ParticleSystem.EmissionModule emission = m_Particles.emission;
        emission.enabled = false;

        ParticleSystem.ShapeModule shape = m_Particles.shape;
        shape.shapeType = ParticleSystemShapeType.Cone;
        shape.angle = fSparkCone;
        shape.radius = 0.01f;

        ParticleSystem.SizeOverLifetimeModule size = m_Particles.sizeOverLifetime;
        size.enabled = true;
        size.size = new ParticleSystem.MinMaxCurve(1.0f, AnimationCurve.Linear(0.0f, 1.0f, 1.0f, 0.2f));

        var fade = new Gradient();
        fade.SetKeys(
            new[] { new GradientColorKey(Color.white, 0.0f), new GradientColorKey(Color.white, 1.0f) },
            new[] { new GradientAlphaKey(1.0f, 0.0f), new GradientAlphaKey(0.0f, 1.0f) });
        ParticleSystem.ColorOverLifetimeModule color = m_Particles.colorOverLifetime;
        color.enabled = true;
        color.color = fade;

        ParticleSystemRenderer renderer = GetComponent<ParticleSystemRenderer>();
        renderer.sharedMaterial = ParticleMaterial;
        renderer.renderMode = ParticleSystemRenderMode.Billboard;
        renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        renderer.receiveShadows = false;

        m_Particles.Play();
    }

    void BuildLight()
    {
        m_Light = gameObject.AddComponent<Light>();
        m_Light.type = LightType.Spot;
        m_Light.spotAngle = fLightSpotAngle;
        m_Light.range = fLightRange;
        m_Light.color = FlashColor;
        m_Light.shadows = LightShadows.None;
        m_Light.enabled = false;
    }
}
