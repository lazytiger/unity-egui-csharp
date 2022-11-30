using System;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using UnityEngine;
using UnityEngine.Rendering;

namespace EGui
{
    [System.Runtime.InteropServices.StructLayout(System.Runtime.InteropServices.LayoutKind.Sequential)]
    public struct VertexData
    {
        private readonly Vector2 position;
        private readonly Color32 color;
        private readonly Vector2 uv;
    }

    public class Painter
    {
        private static Painter _instance;

        public static Painter Instance
        {
            get { return _instance ??= new Painter(); }
        }

        private CommandBuffer _cb;

        public CommandBuffer CommandBuffer
        {
            set => _cb = value;
        }


        private Material _material;

        public Material Material
        {
            set => _material = value;
        }

        private readonly Dictionary<ulong, Material> _materials = new Dictionary<ulong, Material>();

        public void SetTexture(ulong textureId, int offsetX, int offsetY, int width, int height, int filterMode,
            IntPtr data)
        {
            var tex = new Texture2D(width, height, TextureFormat.RGBA32, false, true)
            {
                filterMode = filterMode == 1 ? FilterMode.Point : FilterMode.Bilinear
            };
            tex.LoadRawTextureData(data, width * height * 4);
            tex.Apply();

            if (_materials.TryGetValue(textureId, out var m))
            {
                if ((offsetX + width) <= m.mainTexture.width && (offsetY + height) <= m.mainTexture.height)
                {
                    _cb.CopyTexture(tex, 0, 0, 0, 0, width, height,
                        m.mainTexture, 0, 0, offsetX, offsetY);
                }
                else if (offsetX == 0 && offsetY == 0)
                {
                    m.mainTexture = tex;
                }
                else
                {
                    Debug.LogError("invalid message update");
                    Debug.Log($"width:{width}, height:{height}, offsetX:{offsetX}, offsetY:{offsetY}");
                    Debug.Log($"texture:{m.mainTexture.width},{m.mainTexture.height}");
                }
            }
            else
            {
                var material = GameObject.Instantiate(_material);
                material.mainTexture = tex;
                _materials.Add(textureId, material);
            }
        }

        public void RemTexture(ulong textureId)
        {
            _materials.Remove(textureId);
        }

        private Queue<Mesh> last = new();
        private Queue<Mesh> current = new();

        public void BeginPaint()
        {
            _cb.Clear();
            _cb.BeginSample("Draw");
            if (current.Count > 0)
            {
                current.Clear();
                Resources.UnloadUnusedAssets();
            }
        }

        public void PaintMesh(ulong textureId, int vertexCount, IntPtr vBuffer, int indexCount, IntPtr iBuffer,
            Bounds bound)
        {
            if (!_materials.TryGetValue(textureId, out var material))
            {
                return;
            }

            if (last.TryDequeue(out var mesh))
            {
                mesh.Clear();
            }
            else
            {
                mesh = new Mesh();
            }

            mesh.bounds = bound;
            var vad = new[]
            {
                new VertexAttributeDescriptor(VertexAttribute.Position, VertexAttributeFormat.Float32, 2),
                new VertexAttributeDescriptor(VertexAttribute.Color, VertexAttributeFormat.UNorm8, 4),
                new VertexAttributeDescriptor(VertexAttribute.TexCoord0, VertexAttributeFormat.Float32, 2),
            };
            mesh.SetVertexBufferParams(vertexCount, vad);
            var vertexBuffer = IntPtrToNativeArray<VertexData>(vBuffer, vertexCount);
            mesh.SetVertexBufferData(vertexBuffer, 0,
                0, vertexCount, 0, MeshUpdateFlags.DontRecalculateBounds);

            mesh.SetIndexBufferParams(indexCount, IndexFormat.UInt32);
            var indexBuffer = IntPtrToNativeArray<uint>(iBuffer, indexCount);
            mesh.SetIndexBufferData(indexBuffer, 0, 0,
                indexCount, MeshUpdateFlags.DontRecalculateBounds);

            mesh.SetSubMesh(0, new SubMeshDescriptor(0, indexCount),
                MeshUpdateFlags.DontRecalculateBounds);

            mesh.UploadMeshData(false);
            _cb.DrawMesh(mesh, Matrix4x4.identity, material);
            current.Enqueue(mesh);
        }

        public void EndPaint()
        {
            (last, current) = (current, last);
            _cb.EndSample("Draw");
        }

        private static unsafe NativeArray<T> IntPtrToNativeArray<T>(IntPtr ptr, int count) where T : struct
        {
            var array =
                NativeArrayUnsafeUtility.ConvertExistingDataToNativeArray<T>(
                    ptr.ToPointer(), count, Allocator.None);
            NativeArrayUnsafeUtility.SetAtomicSafetyHandle(ref array,
                AtomicSafetyHandle.GetTempUnsafePtrSliceHandle());
            return array;
        }
    }
}