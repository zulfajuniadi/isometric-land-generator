using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;
using TilemapGenerator.Settings;
using Random = UnityEngine.Random;

namespace TilemapGenerator.Behaviours
{
    [System.Serializable]
    public class SpawnerProbability
    {
        public InstancedSpawnerConfiguration Spawer;
        [Range(0, 1)]
        public float Probability = 0.5f;
        public int MaxCount = -1;
    }

    [System.Serializable]
    public class BiomeConfig
    {
        public TileConfiguration TileConfig;
        [Range(0, 1)]
        public float Height;
        public SpawnerProbability[] Spawners = new SpawnerProbability[0];
    }

    [ExecuteInEditMode]
    public class LandGenerator : MonoBehaviour
    {
        public float Seed = 100;
        public int Height = 80;
        public float NoiseScale = 120;
        public int ChunkSize = 64;
        [Range(9, 25)]
        public int ActiveTilemaps = 15;
        public AnimationCurve TerrainCurve = new AnimationCurve();
        public BiomeConfig[] BiomeConfigs = new BiomeConfig[0];
        public Dictionary<float, Tuple<int, Dictionary<Vector4, TileBase>>> CachedBiomes = new Dictionary<float, Tuple<int, Dictionary<Vector4, TileBase>>>();
        public Transform Output;
        public ChunkProvider ChunkProvider;

        public Dictionary<int, InstancedRenderer> CachedRenderers = new Dictionary<int, InstancedRenderer>();

        private void OnEnable()
        {
#if UNITY_EDITOR
            if (Application.isPlaying)
            {
                Application.targetFrameRate = 0;
                QualitySettings.vSyncCount = 0;
            }
#endif
            Generate();
        }

        public void Generate()
        {
            Clear();
            foreach (var biome in BiomeConfigs)
            {
                var tiles = new Dictionary<Vector4, TileBase>();
                biome.TileConfig.GetCacheData(tiles);
                CachedBiomes.Add(Mathf.Round(biome.Height * Height), new Tuple<int, Dictionary<Vector4, TileBase>>(biome.TileConfig.GetHashCode(), tiles));
                foreach (var spawner in biome.Spawners)
                {
                    if (spawner.Spawer == null) continue;
                    RegisterSpawner(spawner.Probability, spawner.Spawer);
                }
            }
            Random.InitState(Seed.GetHashCode());
            ChunkProvider.Boot();
            ChunkProvider.RandomOffset = new Vector2Int(
                Random.Range(-999999, 999999),
                Random.Range(-999999, 999999)
            );
        }

        private void Clear()
        {
            CachedBiomes.Clear();
            foreach (var renderer in CachedRenderers)
            {
                renderer.Value.Dispose();
            }
            CachedRenderers.Clear();
        }

        private void RegisterSpawner(float probability, InstancedSpawnerConfiguration configuration)
        {
            if (!CachedRenderers.ContainsKey(configuration.GetHashCode()))
            {
                var renderer = new InstancedRenderer((int) (probability * ChunkSize * ChunkSize * ActiveTilemaps), configuration.PackedTexture, configuration.Material, configuration.MeshSize);
                CachedRenderers.Add(configuration.GetHashCode(), renderer);
            }
        }

        private void LateUpdate()
        {
            foreach (var renderer in CachedRenderers)
            {
                renderer.Value.UpdateOffset(Output.localPosition);
                renderer.Value.Tick();
            }
        }

        private void OnDisable()
        {
            Clear();
        }

#if UNITY_EDITOR
        void OnValidate()
        {
            if (Height < 1)
            {
                Height = 1;
            }
            if (NoiseScale < 0)
            {
                NoiseScale = 0;
            }
        }
#endif
    }
}
