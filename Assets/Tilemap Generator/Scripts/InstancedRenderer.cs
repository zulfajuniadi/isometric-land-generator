using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace TilemapGenerator
{

    public struct TileComparator : IEqualityComparer<Vector4>
    {
        public bool Equals(Vector4 v1, Vector4 v2)
        {
            return v1.x == v2.x && v1.y == v2.y;
        }

        public int GetHashCode(Vector4 obj)
        {
            return (int) (obj.x * obj.y);
        }
    }

    public class InstancedRenderer : System.IDisposable
    {
        private Mesh mesh;
        private Material material;
        private Vector3 offset;
        private Texture3D packedTexture;
        private int cachedInstanceCount = -1;
        private ComputeBuffer positionBuffer;
        private ComputeBuffer argsBuffer;
        private uint[] args = new uint[5] { 0, 0, 0, 0, 0 };
        private HashSet<Vector4> instances = new HashSet<Vector4>();
        private Vector4[] instancesCache;

        public InstancedRenderer(int capacity, Texture3D packedTexture, Material material, float meshSize)
        {
            this.packedTexture = packedTexture;
            float height = (float) packedTexture.width / (float) packedTexture.height * meshSize;
            float width = 1 * meshSize;
            this.mesh = Utils.CreatePlane(height, width);
            this.material = Object.Instantiate(material);
            this.material.SetTexture("_MainTex3D", packedTexture);
            argsBuffer = new ComputeBuffer(1, args.Length * sizeof(uint), ComputeBufferType.IndirectArguments);
            positionBuffer = new ComputeBuffer(capacity, 16);
            instances = new HashSet<Vector4>(new Vector4[capacity], new TileComparator());
            instancesCache = new Vector4[capacity];
            instances.Clear();
        }

        public void Tick()
        {
            if (
                packedTexture != null &&
                instances.Count > 0
            )
            {
                UpdateBuffers();
                Graphics.DrawMeshInstancedIndirect(mesh, 0, material, new Bounds(Vector3.zero, new Vector3(100.0f, 100.0f, 100.0f)), argsBuffer);
            }
        }

        void UpdateBuffers()
        {
            cachedInstanceCount = instances.Count;
            if (
                instancesCache.Length < cachedInstanceCount
            )
            {
                instancesCache = new Vector4[cachedInstanceCount];
                if (positionBuffer.count < cachedInstanceCount)
                {
                    positionBuffer.Dispose();
                    positionBuffer = new ComputeBuffer(cachedInstanceCount, 16);
                }
            }
            int i = 0;
            foreach (var v in instances)
            {
                instancesCache[i] = new Vector4(v.x + offset.x, v.y + offset.y, v.z, v.w);
                i++;
            }
            positionBuffer.SetData(instancesCache);
            material.SetBuffer("positionBuffer", positionBuffer);
            if (mesh != null)
            {
                args[0] = (uint) mesh.GetIndexCount(0);
                args[1] = (uint) cachedInstanceCount;
                args[2] = (uint) mesh.GetIndexStart(0);
                args[3] = (uint) mesh.GetBaseVertex(0);
            }
            else
            {
                args[0] = args[1] = args[2] = args[3] = 0;
            }
            argsBuffer.SetData(args);
        }

        public void UpdateOffset(Vector3 offset)
        {
            if (offset != this.offset)
            {
                this.offset = offset;
            }
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
            instances.Clear();
        }

        public void Dispose()
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
