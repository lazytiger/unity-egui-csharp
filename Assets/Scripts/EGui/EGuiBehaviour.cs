using System;
using System.Collections;
using System.Collections.Generic;
using EGui;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

[RequireComponent(typeof(Camera))]
public class EGuiBehaviour : MonoBehaviour
{
#if UNITY_EDITOR
    public string EGuiLibPath;
#endif

    public Material material;

    [Range(30, 120)]
    public int fps = 60;
    
    private void Awake()
    {
        Application.targetFrameRate = fps;
        Input.imeCompositionMode = IMECompositionMode.On;
        var c = GetComponent<Camera>();
        var cb = new CommandBuffer();
        cb.name = "Draw EGui";
        c.AddCommandBuffer(CameraEvent.AfterForwardAlpha, cb);
#if UNITY_EDITOR
        Bridge.Instance.EGuiLibPath = EGuiLibPath;
#endif
        Painter.Instance.CommandBuffer = cb;
        Painter.Instance.Material = material;
        Bridge.Instance.Init();
    }

    // Update is called once per frame
    private void Update()
    {
        Bridge.Instance.Update();
    }

    private void OnApplicationQuit()
    {
        Bridge.Instance.Free();
    }
}