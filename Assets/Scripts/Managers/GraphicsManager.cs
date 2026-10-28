using UnityEngine;
using UnityEngine.Rendering.Universal;

public class GraphicsManager : MonoBehaviour
{
    UniversalRenderPipelineAsset m_Asset;
 
    // Pixelizer / internal scaling
    [SerializeField] Vector2Int nTargetRes = new Vector2Int(480, 270);
    float fOriginalScale;
    int nLastHeight;

    void OnEnable()
    {
        m_Asset = UniversalRenderPipeline.asset;
        fOriginalScale = m_Asset.renderScale;
        nLastHeight = 0;
    }

    void OnDisable()
    {
        if(m_Asset != null)
        {
            m_Asset.renderScale = fOriginalScale;
        }
    }

    void Update()
    {
        if(Screen.height == nLastHeight)
        {
            return;
        }

        nLastHeight = Screen.height;
        int nFactor = Mathf.Max(1, Mathf.RoundToInt((float) Screen.height / nTargetRes.y));
        m_Asset.renderScale = 1.0f / nFactor;
    }
}
