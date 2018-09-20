using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;
using Random = UnityEngine.Random;

namespace TilemapGenerator
{
    [System.Serializable]
    public struct TileConfiguration
    {
        public TileBase SelectedTile;
        public Vector4 Corners;
    }

    [ExecuteInEditMode]
    public class LandGenerator : MonoBehaviour
    {
        public float Seed = 100;
        public int Viewport = 128;
        public int Height = 1;
        public int WaterHeight = 4;
        public float NoiseScale;
        public TileConfiguration[] TileConfigurations;
        public TileConfiguration[] WaterTiles;
        public Transform Caddy;
        public Camera GameCamera;
        public int ChunkSize = 64;
        public InstancedRenderer TreesRenderer;

        private Dictionary<Vector4, TileBase> cachedLandTiles = new Dictionary<Vector4, TileBase>();
        private Dictionary<Vector4, TileBase> cachedWaterTiles = new Dictionary<Vector4, TileBase>();
        private ChunkProvider chunkProvider;

        private void OnEnable()
        {
            Application.targetFrameRate = 0;
            QualitySettings.antiAliasing = 0;
            QualitySettings.vSyncCount = 0;
            chunkProvider = new ChunkProvider(Caddy, WaterHeight, cachedLandTiles, cachedWaterTiles, TreesRenderer);
            Generate();
        }

        public void Generate()
        {
            Clear();
            cachedWaterTiles.Clear();
            cachedLandTiles.Clear();
            chunkProvider.Clear();
            foreach (TileConfiguration config in TileConfigurations)
            {
                cachedLandTiles.Add(config.Corners, config.SelectedTile);
            }
            foreach (TileConfiguration config in WaterTiles)
            {
                cachedWaterTiles.Add(config.Corners, config.SelectedTile);
            }
            chunkProvider.ChunkSize = ChunkSize;
            chunkProvider.Height = Height;
            chunkProvider.NoiseScale = NoiseScale;
            Random.InitState(Seed.GetHashCode());
            chunkProvider.RandomOffset = new Vector2Int(
                Random.Range(-999, 999),
                Random.Range(-999, 999)
            );
            StartCoroutine(chunkProvider.Tick());
        }

        private void Clear()
        {
            while (Caddy.childCount > 0)
                DestroyImmediate(Caddy.GetChild(0).gameObject);
            StopAllCoroutines();
            TreesRenderer.Clear();
        }

#if UNITY_EDITOR
        void OnValidate()
        {
            if (Height < 1)
            {
                Height = 1;
            }
            if (WaterHeight > Height)
            {
                WaterHeight = Height;
            }
            if (Viewport < 3)
            {
                Viewport = 3;
            }
            if (NoiseScale < 0)
            {
                NoiseScale = 0;
            }
        }
#endif
    }
}
