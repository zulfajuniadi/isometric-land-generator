using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace TilemapGenerator
{

    public class SortedRenderer : ITileRenderer, System.IDisposable
    {
        private Mesh mesh;
        private Material material;
        private Texture3D packedTexture;
        private ComputeBuffer textureIndicesBuffer;
        private HashSet<Vector4> instances = new HashSet<Vector4>();
        private bool isDirty = true;
        private Vector3 lastPosition;
        private float lastSize;
        private float width;
        private float height;
        private Vector3[] frustumCorners = new Vector3[4];
        private Camera mainCamera;
        private Camera minimapCamera;
        private Transform mainCameraTransform;
        private List<Vector4> cachedInstances;
        private MaterialPropertyBlock block;
        private Dictionary<float, Mesh> meshes = new Dictionary<float, Mesh>();
        private static float nextRender;

        public SortedRenderer(Camera mainCamera, Camera minimapCamera, int capacity, Texture3D packedTexture, Shader shader, float meshSize)
        {
            this.mainCamera = mainCamera;
            this.minimapCamera = minimapCamera;
            this.packedTexture = packedTexture;
            this.height = (float) packedTexture.width / (float) packedTexture.height * meshSize;
            this.width = 1 * meshSize;
            this.mesh = Utils.CreatePlane(height, width);
            float delta = 1f / packedTexture.depth;
            for (float i = 0; i <= packedTexture.depth; i++)
            {
                float key = Mathf.Round(((i * delta) + (0.5f / (float) packedTexture.depth)) * 10f);
                Mesh mesh = Utils.CreatePlane(height, width);
                mesh.uv2 = new Vector2[4] { new Vector2(key / 10f, 0), new Vector2(key / 10f, 0), new Vector2(key / 10f, 0), new Vector2(key / 10f, 0) };
                meshes.Add(key, mesh);
            }
            this.material = new Material(shader);
            this.material.renderQueue = 3000;
            this.material.SetTexture("_MainTex3D", packedTexture);
            mainCameraTransform = mainCamera.transform;
            instances = new HashSet<Vector4>(new Vector4[capacity], new TileComparator());
            block = new MaterialPropertyBlock();
            cachedInstances = new List<Vector4>(capacity);
            instances.Clear();
        }

        public void Tick()
        {
            if (
                packedTexture != null &&
                instances.Count > 0
            )
            {
                if (mainCameraTransform.position != lastPosition || mainCamera.orthographicSize != lastSize)
                {
                    lastPosition = mainCameraTransform.position;
                    lastSize = mainCamera.orthographicSize;
                    isDirty = true;
                }
                UpdateCache();
                foreach (var item in cachedInstances)
                {
                    Graphics.DrawMesh(meshes[item.w], new Vector3(item.x, item.y, item.z), Quaternion.identity, material, 1, null, 0, block, false, false, false);
                }
            }
        }

        void UpdateCache()
        {
            if (!isDirty) return;
            isDirty = false;
            if (Time.time > nextRender)
            {
                minimapCamera.Render();
                nextRender = Time.time + Time.deltaTime;
            }

            // culling
            mainCamera.CalculateFrustumCorners(new Rect(0, 0, 1, 1), mainCamera.farClipPlane, Camera.MonoOrStereoscopicEye.Mono, frustumCorners);
            Vector2 min = mainCameraTransform.position + frustumCorners[0] - Vector3.one;
            Vector2 max = mainCameraTransform.position + frustumCorners[2] + Vector3.one;
            cachedInstances.Clear();
            foreach (var instance in instances)
            {
                if (
                    instance.x >= min.x &&
                    instance.x <= max.x &&
                    instance.y >= min.y &&
                    instance.y <= max.y
                )
                {
                    cachedInstances.Add(instance);
                }
            }
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

        public void Dispose() { }
    }
}
