using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace TilemapGenerator
{
    [ExecuteInEditMode]
    public class InstancedRenderer : MonoBehaviour
    {
        public Material InstanceMaterial;
        public Vector3 Offset;
        public Texture2D[] SourceImages = new Texture2D[0];
        public Texture3D PackedImage;

        private Mesh instanceMesh;
        private int cachedInstanceCount = -1;
        private ComputeBuffer positionBuffer;
        private ComputeBuffer argsBuffer;
        private uint[] args = new uint[5] { 0, 0, 0, 0, 0 };
        private HashSet<Vector4> instances = new HashSet<Vector4>();
        private Vector4[] instancesCache;

        private void OnEnable()
        {

            var go = GameObject.CreatePrimitive(PrimitiveType.Quad);
            go.hideFlags = HideFlags.HideAndDontSave;
            instanceMesh = go.GetComponent<MeshFilter>().sharedMesh;

            if (
                PackedImage != null
            )
            {
                InstanceMaterial.SetTexture("_MainTex3D", PackedImage);
            }
            if (instances.Count > 0)
                UpdateBuffers();
        }

        private void Update()
        {
            if (
                PackedImage == null ||
                instances.Count == 0
            ) return;
            UpdateBuffers();
            Graphics.DrawMeshInstancedIndirect(instanceMesh, 0, InstanceMaterial, new Bounds(Vector3.zero, new Vector3(100.0f, 100.0f, 100.0f)), argsBuffer);
        }

        void UpdateBuffers()
        {
            cachedInstanceCount = instances.Count;
            if (
                instancesCache == null ||
                positionBuffer == null ||
                argsBuffer == null ||
                instancesCache.Length != cachedInstanceCount
            )
            {
                if (positionBuffer != null)
                    positionBuffer.Dispose();
                if (argsBuffer != null)
                    argsBuffer.Dispose();
                positionBuffer = new ComputeBuffer(cachedInstanceCount, 16);
                argsBuffer = new ComputeBuffer(1, args.Length * sizeof(uint), ComputeBufferType.IndirectArguments);
                instancesCache = new Vector4[cachedInstanceCount];
            }
            int i = 0;
            foreach (var v in instances)
            {
                instancesCache[i] = new Vector4(v.x + Offset.x, v.y + Offset.y, v.z, v.w);
                i++;
            }
            positionBuffer.SetData(instancesCache);
            InstanceMaterial.SetBuffer("positionBuffer", positionBuffer);
            if (instanceMesh != null)
            {
                args[0] = (uint) instanceMesh.GetIndexCount(0);
                args[1] = (uint) cachedInstanceCount;
                args[2] = (uint) instanceMesh.GetIndexStart(0);
                args[3] = (uint) instanceMesh.GetBaseVertex(0);
            }
            else
            {
                args[0] = args[1] = args[2] = args[3] = 0;
            }
            argsBuffer.SetData(args);
        }

        public void AddInstances(IEnumerable<Vector4> instances)
        {
            this.instances.UnionWith(instances);
        }

        public void RemoveInstances(IEnumerable<Vector4> instances)
        {
            this.instances.ExceptWith(instances);
        }

        public void Clear()
        {
            this.instances.Clear();
        }

        void OnDisable()
        {
            if (positionBuffer != null)
                positionBuffer.Release();
            positionBuffer = null;

            if (argsBuffer != null)
                argsBuffer.Release();
            argsBuffer = null;
        }
    }

}
