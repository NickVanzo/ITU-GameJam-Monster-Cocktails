using System.Collections.Generic;
using System.Numerics;
using UnityEngine;
using UnityEngine.Rendering.Universal;

public class GraphicsManager : MonoBehaviour
{
    public static GraphicsManager Instance { get; private set; }
    UniversalRenderPipelineAsset m_Asset;
 
    // Pixelizer / internal scaling
    [SerializeField] int nTargetRes = 4;
    [SerializeField] List<Vector2Int> aTargetRes = new List<Vector2Int> {
        new(256, 144),
        new(320, 180),
        new(480, 270),
        new(640, 360),
        new(800, 450),
        new(960, 540),
        new(1280, 720),
        new(1920, 1080),
        new(-1, -1)
    };

    float fOriginalScale;
    int nCurrentTargetIndex = -1;

    void Awake()
    {
        Instance = this;
    }

    void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
        
    }

    void OnEnable()
    {
        m_Asset = UniversalRenderPipeline.asset;
        fOriginalScale = m_Asset.renderScale;
        nCurrentTargetIndex = -1;
    }

    void OnDisable()
    {
        if(m_Asset != null)
        {
            m_Asset.renderScale = fOriginalScale;
        }
    }

    // [Tweak("Graphics")]
    public static void SetTargetResIndex(int nTargetIndex)
    {
       Instance.nTargetRes = nTargetIndex;
    }

    bool IsValidIndex(int nIndex)
    {
        return nIndex > 0 && nIndex < aTargetRes.Count;
    }

    void Update()
    {
        if(nCurrentTargetIndex == nTargetRes || !IsValidIndex(nTargetRes))
            return;

        nCurrentTargetIndex = nTargetRes;
        
        Vector2Int res = aTargetRes[nCurrentTargetIndex];
        if(res.x == -1) 
        {
            m_Asset.renderScale = 1;
            return;
        }

        m_Asset.renderScale = Mathf.Clamp(
            (float) res.y / Screen.height,
            0.1f,
            1.0f
        );
    }
}
