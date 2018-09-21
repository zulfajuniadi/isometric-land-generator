using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace TilemapGenerator
{
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

        public InstancedRenderer(Texture3D packedTexture, Material material, float meshSize)
        {
            this.packedTexture = packedTexture;
            float height = (float) packedTexture.width / (float) packedTexture.height * meshSize;
            float width = 1 * meshSize;
            this.mesh = CreateMesh(height, width);
            this.material = Object.Instantiate(material);
            this.material.SetTexture("_MainTex3D", packedTexture);
            argsBuffer = new ComputeBuffer(1, args.Length * sizeof(uint), ComputeBufferType.IndirectArguments);
        }

        private Mesh CreateMesh(float width, float length)
        {
            Mesh mesh = new Mesh();
            int resX = 2;
            int resY = 2;

            Vector3[] vertices = new Vector3[resX * resY];
            for (int y = 0; y < resY; y++)
            {
                float yPos = ((float) y / (resY - 1) - .5f) * length;
                for (int x = 0; x < resX; x++)
                {
                    float xPos = ((float) x / (resX - 1) - .5f) * width;
                    vertices[x + y * resX] = new Vector3(xPos, yPos, 0);
                }
            }

            Vector2[] uvs = new Vector2[vertices.Length];
            for (int v = 0; v < resY; v++)
            {
                for (int u = 0; u < resX; u++)
                {
                    uvs[u + v * resX] = new Vector2((float) u / (resX - 1), (float) v / (resY - 1));
                }
            }

            int nbFaces = (resX - 1) * (resY - 1);
            int[] triangles = new int[nbFaces * 6];
            int t = 0;
            for (int face = 0; face < nbFaces; face++)
            {
                // Retrieve lower left corner from face ind
                int i = face % (resX - 1) + (face / (resY - 1) * resX);

                triangles[t++] = i + resX;
                triangles[t++] = i + 1;
                triangles[t++] = i;

                triangles[t++] = i + resX;
                triangles[t++] = i + resX + 1;
                triangles[t++] = i + 1;
            }

            mesh.vertices = vertices;
            mesh.uv = uvs;
            mesh.triangles = triangles;

            mesh.RecalculateBounds();
            return mesh;
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
                instancesCache == null ||
                positionBuffer == null ||
                instancesCache.Length < cachedInstanceCount
            )
            {
                if (positionBuffer != null)
                    positionBuffer.Dispose();
                positionBuffer = new ComputeBuffer(cachedInstanceCount, 16);
                instancesCache = new Vector4[cachedInstanceCount];
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
