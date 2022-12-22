using EGui;
using UnityEngine;
using UnityEngine.Rendering;

[RequireComponent(typeof(Camera))]
public class EGuiBehaviour : MonoBehaviour
{
#if UNITY_EDITOR
    public string EGuiLibPath;
#endif

    public Material material;

    [Range(30, 120)] public int fps = 60;

    private void Awake()
    {
        Application.targetFrameRate = fps;
        Input.imeCompositionMode = IMECompositionMode.On;
#if UNITY_EDITOR
        Bridge.Instance.EGuiLibPath = EGuiLibPath;
#endif
        Painter.Instance.Material = material;
        var cb = new CommandBuffer();
        cb.name = "Draw EGui";
        Painter.Instance.CommandBuffer = cb;

        if (GraphicsSettings.currentRenderPipeline)
        {
            RenderPipelineManager.endFrameRendering += delegate(ScriptableRenderContext context, Camera[] cameras)
            {
                Bridge.Instance.Update();
                context.ExecuteCommandBuffer(cb);
                context.Submit();
            };
        }
        else
        {
            var c = GetComponent<Camera>();
            c.AddCommandBuffer(CameraEvent.AfterForwardAlpha, cb);
        }
        
        Bridge.Instance.Init();
    }

    // Update is called once per frame
    private void Update()
    {
        if (!GraphicsSettings.currentRenderPipeline)
        {
            Bridge.Instance.Update();
        }
    }

    private void OnApplicationQuit()
    {
        Bridge.Instance.Free();
    }
}