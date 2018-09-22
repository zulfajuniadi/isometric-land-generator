using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace TilemapGenerator
{

    public class TileComparator : IEqualityComparer<Vector4>
    {
        public bool Equals(Vector4 v1, Vector4 v2)
        {
            return v1.Equals(v2);
        }

        public int GetHashCode(Vector4 v1)
        {
            return (int) (v1.x * v1.y);
        }
    }

    public class Matrix4x4Sorter : IComparer<Matrix4x4>
    {
        public int Compare(Matrix4x4 m1, Matrix4x4 m2)
        {
            Vector4 c1 = m1.GetColumn(3);
            Vector4 c2 = m2.GetColumn(3);
            return Mathf.RoundToInt((c2.y - c1.y) * 1000f);
        }
    }

    public class InstancedRenderer : ITileRenderer, System.IDisposable
    {
        private Mesh mesh;
        private Material material;
        private Texture3D packedTexture;
        private int cachedInstanceCount = -1;
        private ComputeBuffer textureIndicesBuffer;
        private HashSet<Vector4> instances = new HashSet<Vector4>();
        private bool isDirty = false;
        Camera mainCamera;
        List<Matrix4x4> transformsCache;
        Matrix4x4[] tempTransforms = new Matrix4x4[1023];
        // Matrix4x4Sorter sorter;

        public InstancedRenderer(Camera mainCamera, int capacity, Texture3D packedTexture, Material material, float meshSize)
        {
            this.mainCamera = mainCamera;
            this.packedTexture = packedTexture;
            float height = (float) packedTexture.width / (float) packedTexture.height * meshSize;
            float width = 1 * meshSize;
            this.mesh = Utils.CreatePlane(height, width);
            this.material = new Material(Shader.Find("Sprites/Instanced"));
            this.material.enableInstancing = true;
            this.material.SetTexture("_MainTex", Resources.Load<Texture2D>("Tree"));
            instances = new HashSet<Vector4>(new Vector4[capacity], new TileComparator());
            transformsCache = new List<Matrix4x4>(capacity);
            instances.Clear();
            // sorter = new Matrix4x4Sorter();
        }

        public void Tick()
        {
            if (
                packedTexture != null &&
                instances.Count > 0
            )
            {
                int length = transformsCache.Count;
                UpdateBuffers();
                for (int i = length - 1; i > -1; i -= 1023)
                {
                    int count = i > 1023 ? 1023 : i;
                    transformsCache.CopyTo(i - count, tempTransforms, 0, count);
                    Graphics.DrawMeshInstanced(mesh, 0, material, tempTransforms, count);
                }
            }
        }

        void UpdateBuffers()
        {
            if (!isDirty) return;
            isDirty = false;
            cachedInstanceCount = instances.Count;
            transformsCache.Clear();
            foreach (var instance in instances)
            {
                Matrix4x4 matrix = Matrix4x4.identity;
                matrix.SetColumn(3, new Vector4(instance.x, instance.y, instance.z, 1));
                transformsCache.Add(matrix);
            }
            // transformsCache.Sort(sorter);
        }

        public void AddInstances(IEnumerable<Vector4> instances)
        {
            this.instances.UnionWith(instances);
            isDirty = true;
        }

        public void RemoveInstances(IEnumerable<Vector4> instances)
        {
            this.instances.ExceptWith(instances);
            isDirty = true;
        }

        public void Clear()
        {
            instances.Clear();
            isDirty = true;
        }

        public void Dispose()
        {
            if (textureIndicesBuffer != null)
                textureIndicesBuffer.Release();
            textureIndicesBuffer = null;
        }
    }
}
