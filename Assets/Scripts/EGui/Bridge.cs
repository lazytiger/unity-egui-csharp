using System;
using System.IO;
using System.Runtime.InteropServices;
using AOT;
using Google.Protobuf;
using UnityEngine;

namespace EGui
{
    [StructLayout(LayoutKind.Sequential)]
    public struct UnityInitializer
    {
        internal IntPtr SetTexture;
        internal IntPtr RemTexture;
        internal IntPtr BeginPaint;
        internal IntPtr PaintMesh;
        internal IntPtr EndPaint;
        internal IntPtr ShowKeyboard;
    }

    public struct EGuiInitializer
    {
        internal IntPtr Update;
        internal IntPtr App;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct Buffer
    {
        public IntPtr data;
        public ulong len;
    }

    public class Bridge
    {
#if UNITY_IPHONE
        public const string Library = "__Internal";
#else
        public const string Library = "egui";
#endif

        private delegate void SetTexture(ulong textureId, int offsetX, int offsetY, int width, int height,
            int filterMode, IntPtr data);

        private delegate void RemTexture(ulong textureId);

        private delegate void PaintMesh(ulong textureId, int vertexCount, IntPtr vBuffer, int indexCount,
            IntPtr iBuffer,
            float minX, float minY, float maxX, float maxY);

        private delegate void FuncNoArgsNoReturn();

        private delegate void ShowKeyboard(int show);

#if UNITY_EDITOR
        private delegate EGuiInitializer InitEGuiDelegate(UnityInitializer initializer);

        private InitEGuiDelegate InitEGui;


        private static class NativeMethods
        {
            [DllImport("kernel32.dll")]
            public static extern IntPtr LoadLibrary(string dllToLoad);

            [DllImport("kernel32.dll")]
            public static extern IntPtr GetProcAddress(IntPtr hModule, string procedureName);

            [DllImport("kernel32.dll")]
            public static extern bool FreeLibrary(IntPtr hModule);
        }

        private IntPtr eguiHandle = IntPtr.Zero;


        private string _eguiLibPath = "";

        public string EGuiLibPath
        {
            set => _eguiLibPath = value;
        }

#else
        [DllImport(Library, EntryPoint = "init")]
        public static extern EGuiInitializer InitEGui(UnityInitializer initializer);
#endif
        private IntPtr app = IntPtr.Zero;
        
        private delegate void UpdateEGuiDelegate(Buffer buffer, IntPtr app, uint destroy);

        private UpdateEGuiDelegate UpdateEGui;

        private static Bridge _instance;

        public static Bridge Instance
        {
            get { return _instance ??= new Bridge(); }
        }

        private MemoryStream ms = new();

        [MonoPInvokeCallback(typeof(SetTexture))]
        static void setTexture(ulong textureId, int offsetX, int offsetY, int width, int height, int filterMode,
            IntPtr data)
        {
            Painter.Instance.SetTexture(textureId, offsetX, offsetY, width, height, filterMode, data);
        }

        [MonoPInvokeCallback(typeof(RemTexture))]
        static void remTexture(ulong textureId)
        {
            Painter.Instance.RemTexture(textureId);
        }

        [MonoPInvokeCallback(typeof(PaintMesh))]
        static void paintMesh(ulong textureId, int vertexCount, IntPtr vBuffer, int indexCount, IntPtr iBuffer,
            float minX, float minY, float maxX, float maxY)
        {
            var min = new Vector2(minX, minY);
            var max = new Vector2(maxX, maxY);
            var bound = new Bounds((min + max) / 2, max - min);
            Painter.Instance.PaintMesh(textureId, vertexCount, vBuffer, indexCount, iBuffer, bound);
        }

        [MonoPInvokeCallback(typeof(FuncNoArgsNoReturn))]
        static void BeginPaint()
        {
            Painter.Instance.BeginPaint();
        }

        [MonoPInvokeCallback(typeof(FuncNoArgsNoReturn))]
        static void EndPaint()
        {
            Painter.Instance.EndPaint();
        }

        [MonoPInvokeCallback(typeof(FuncNoArgsNoReturn))]
        static void showKeyboard(int show)
        {
            InputGather.Instance.OpenKeyboard(show); 
        }

        public void Init()
        {
#if UNITY_EDITOR
            eguiHandle = NativeMethods.LoadLibrary(_eguiLibPath);
            if (eguiHandle != IntPtr.Zero)
            {
                var initPtr = NativeMethods.GetProcAddress(eguiHandle, "init");
                InitEGui = Marshal.GetDelegateForFunctionPointer<InitEGuiDelegate>(initPtr);
            }

            if (InitEGui == null)
            {
                throw new Exception("initialize egui failed");
            }

#endif

            var initializer = new UnityInitializer()
            {
                SetTexture = Marshal.GetFunctionPointerForDelegate((SetTexture) setTexture),
                RemTexture = Marshal.GetFunctionPointerForDelegate((RemTexture) remTexture),
                PaintMesh = Marshal.GetFunctionPointerForDelegate((PaintMesh) paintMesh),
                BeginPaint = Marshal.GetFunctionPointerForDelegate((FuncNoArgsNoReturn) BeginPaint),
                EndPaint = Marshal.GetFunctionPointerForDelegate((FuncNoArgsNoReturn) EndPaint),
                ShowKeyboard = Marshal.GetFunctionPointerForDelegate((ShowKeyboard)showKeyboard)
            };
            var egui = InitEGui(initializer);
            UpdateEGui = Marshal.GetDelegateForFunctionPointer<UpdateEGuiDelegate>(egui.Update);
            app = egui.App;
        }

        public void Update()
        {
            if (UpdateEGui == null)
            {
                return;
            }

            var input = InputGather.Instance.GetInput();
            ms.SetLength(0);
            input.WriteTo(ms);
            var handle = GCHandle.Alloc(ms.GetBuffer(), GCHandleType.Pinned);
            var addr = handle.AddrOfPinnedObject();
            UpdateEGui(new Buffer {data = addr, len = (ulong) ms.Length}, app, 0);
            handle.Free();
        }

        public void Free()
        {
#if UNITY_EDITOR
            if (eguiHandle != IntPtr.Zero)
            {
                UpdateEGui(new Buffer() {data = IntPtr.Zero, len = 0}, app, 1);
                NativeMethods.FreeLibrary(eguiHandle);
                eguiHandle = IntPtr.Zero;
                app = IntPtr.Zero;
                InitEGui = null;
                UpdateEGui = null;
            }
#endif
            Resources.UnloadUnusedAssets();
        }
    }
}