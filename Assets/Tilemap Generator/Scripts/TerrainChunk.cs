using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace TilemapGenerator
{
    public class TerrainChunk
    {
        public GameObject GameObject;
        public Tilemap Tilemap;
        public TilemapRenderer Renderer;

        private int[, ] noiseMap;
        private TileBase[] tiles;
        private Vector3Int[] positions;
        private RectInt rect;
        private Vector2Int randomOffset;
        private Vector2Int mapPosition;
        private Dictionary<float, Tuple<int, Dictionary<Vector4, TileBase>>> cachedBiomes;
        private Dictionary<int, List<Vector4>> spawnerPositions;
        private Dictionary<int, Dictionary<int, Tuple<int, float, bool>>> cachedSpawners;
        private int height;
        private int chunkSize;
        private int chunkSize1;
        private float noiseScale;
        private Transform output;
        private System.Random prng;
        private ChunkProvider provider;

        public TerrainChunk(
            ChunkProvider provider,
            Vector2Int mapPosition
        )
        {
            this.mapPosition = mapPosition;
            this.provider = provider;
            this.cachedBiomes = provider.Generator.CachedBiomes;
            this.randomOffset = provider.RandomOffset;
            this.height = provider.Generator.Height;
            this.noiseScale = provider.Generator.NoiseScale;
            this.chunkSize = provider.Generator.ChunkSize;
            float halfSize = chunkSize / 2f;
            this.output = provider.Generator.Output;
            this.chunkSize1 = chunkSize + 1;

            noiseMap = new int[chunkSize1, chunkSize1];
            GameObject = new GameObject();
            GameObject.transform.parent = this.output;
            Tilemap = GameObject.AddComponent<Tilemap>();
            Renderer = GameObject.AddComponent<TilemapRenderer>();
            Renderer.detectChunkCullingBounds = TilemapRenderer.DetectChunkCullingBounds.Manual;
            Renderer.chunkCullingBounds = Vector3.one * halfSize;
            positions = new Vector3Int[chunkSize1 * chunkSize1];
            tiles = new TileBase[chunkSize1 * chunkSize1];
            spawnerPositions = new Dictionary<int, List<Vector4>>();
            cachedSpawners = new Dictionary<int, Dictionary<int, Tuple<int, float, bool>>>();

            foreach (var biome in provider.Generator.BiomeConfigs)
            {
                int biomeHash = biome.TileConfig.GetHashCode();
                if (!cachedSpawners.ContainsKey(biomeHash))
                    cachedSpawners.Add(biomeHash, new Dictionary<int, Tuple<int, float, bool>>());
                foreach (var item in biome.Spawners)
                {
                    cachedSpawners[biomeHash].Add(item.Spawer.GetHashCode(), new Tuple<int, float, bool>(item.Spawer.PackedTexture.depth, item.Probability, item.Spawer.OnEdgeOnly));
                    spawnerPositions[item.Spawer.GetHashCode()] = new List<Vector4>((int) (chunkSize * chunkSize * item.Probability));
                }
            }
        }

        public void Setup(Vector2Int mapPosition)
        {
            Vector2Int baseOffset = mapPosition * chunkSize;
            this.rect = new RectInt(baseOffset, new Vector2Int(chunkSize, chunkSize));
            prng = new System.Random((randomOffset + this.rect.min).GetHashCode());
            float halfSize = chunkSize / 2f;
            Tilemap.ClearAllTiles();
            GameObject.name = rect.min.ToString();
            foreach (var item in spawnerPositions)
            {
                item.Value.Clear();
            }
            BuildPositions();
            Vector2 position = new Vector2(
                (baseOffset.x / chunkSize - baseOffset.y / chunkSize) * halfSize,
                (baseOffset.x / chunkSize + baseOffset.y / chunkSize) * halfSize / 2f
            );
            GameObject.transform.localPosition = position;
            Tilemap.SetTiles(positions, tiles);
            GameObject.SetActive(true);
            foreach (var item in spawnerPositions)
            {
                provider.Generator.CachedRenderers[item.Key].AddInstances(item.Value);
            }
        }

        public void Disable()
        {
            foreach (var item in spawnerPositions)
            {
                if (provider.Generator.CachedRenderers.ContainsKey(item.Key))
                    provider.Generator.CachedRenderers[item.Key].RemoveInstances(item.Value);
            }
            GameObject.name = "Disabled";
            GameObject.SetActive(false);
        }

        private void BuildPositions()
        {
            GenerateNoiseMap(randomOffset + rect.min);
            int halfMap = chunkSize / 2;
            Vector4 flat = new Vector4(0, 0, 0, 0);
            float offsetX = (rect.min.x - rect.min.y) / 2f;
            float offsetY = (rect.min.y + rect.min.x) / 4f;
            var firstBiome = cachedBiomes.GetEnumerator();
            firstBiome.MoveNext();
            int bottomHeight = Mathf.RoundToInt(firstBiome.Current.Key);
            int bottomMinus1 = bottomHeight - 1;
            bool hasComplained = false;
            Tuple<int, Dictionary<Vector4, TileBase>> biome = null;
            TileBase tile = null;
            for (int y = 0; y < chunkSize; y++)
            {
                for (int x = 0; x < chunkSize; x++)
                {
                    int key = x + chunkSize * y;
                    float c0 = noiseMap[x + 1, y + 1];
                    float c1 = noiseMap[x + 1, y];
                    float c2 = noiseMap[x, y];
                    float c3 = noiseMap[x, y + 1];

                    float highest = 0;
                    float lowest = 0;
                    MinMax(c0, c1, c2, c3, out highest, out lowest);
                    biome = GetTileLookup(lowest);
                    int biomeHash = biome.Item1;
                    bool onEdge = false;
                    tile = biome.Item2[flat];
                    if (lowest < bottomHeight)
                    {
                        if (c0 < bottomHeight)
                        {
                            c0 = bottomHeight - 1;
                            noiseMap[x + 1, y + 1] = bottomMinus1;
                        }
                        if (c1 < bottomHeight)
                        {
                            c1 = bottomHeight - 1;
                            noiseMap[x + 1, y] = bottomMinus1;
                        }
                        if (c2 < bottomHeight)
                        {
                            c2 = bottomHeight - 1;
                            noiseMap[x, y] = bottomMinus1;
                        }
                        if (c3 < bottomHeight)
                        {
                            c3 = bottomHeight - 1;
                            noiseMap[x, y + 1] = bottomMinus1;
                        }
                        MinMax(c0, c1, c2, c3, out highest, out lowest);
                    }
                    if (highest != lowest)
                    {
                        float c0n = Mathf.InverseLerp(lowest, highest, c0);
                        float c1n = Mathf.InverseLerp(lowest, highest, c1);
                        float c2n = Mathf.InverseLerp(lowest, highest, c2);
                        float c3n = Mathf.InverseLerp(lowest, highest, c3);
                        Vector4 corners = new Vector4(c0n, c1n, c2n, c3n);
                        if (biome.Item2.ContainsKey(corners))
                        {
                            tile = biome.Item2[corners];
                        }
                        else if (!hasComplained)
                        {
                            hasComplained = true;
                            Debug.Log(corners);
                            Debug.Log(highest);
                        }
                        onEdge = true;
                    }
                    foreach (var item in cachedSpawners[biomeHash])
                    {
                        if (prng.NextDouble() <= item.Value.Item2 && (!item.Value.Item3 || (onEdge && item.Value.Item3)))
                        {
                            float textureCount = (float) item.Value.Item1;
                            float index = Mathf.Round((float) prng.NextDouble() * textureCount) / textureCount + (0.5f * 1f / textureCount);
                            float mapX = x - Mathf.InverseLerp(0f, 0.1f, (float) prng.NextDouble());
                            float mapY = y - Mathf.InverseLerp(0f, 0.1f, (float) prng.NextDouble());
                            float worldX = offsetX + ((mapX - mapY) / 2f);
                            float worldY = offsetY + ((mapX + mapY) / 4f) + (c0 - 1) * 0.25f;
                            spawnerPositions[item.Key].Add(new Vector4(worldX, worldY - (chunkSize / 4f - 1f), 0, index));
                        }
                    }
                    positions[key] = new Vector3Int(x - halfMap, y - halfMap, noiseMap[x, y]);
                    tiles[key] = tile;
                }
            }
        }

        float[] minMaxArgs = new float[4];
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void MinMax(float a, float b, float c, float d, out float maximum, out float minimum)
        {
            minMaxArgs[0] = a;
            minMaxArgs[1] = b;
            minMaxArgs[2] = c;
            minMaxArgs[3] = d;
            float min = float.MaxValue;
            float max = float.MinValue;
            for (int i = 0; i < 4; i++)
            {
                max = minMaxArgs[i] > max ? minMaxArgs[i] : max;
                min = minMaxArgs[i] < min ? minMaxArgs[i] : min;
            }
            maximum = max;
            minimum = min;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private Tuple<int, Dictionary<Vector4, TileBase>> GetTileLookup(float height)
        {
            Tuple<int, Dictionary<Vector4, TileBase>> last = null;
            foreach (var kv in cachedBiomes)
            {
                if (height < kv.Key)
                {
                    return kv.Value;
                }
                last = kv.Value;
            }
            return last;
        }

        public void GenerateNoiseMap(Vector2 offset)
        {
            for (int y = 0; y < chunkSize1; y++)
            {
                for (int x = 0; x < chunkSize1; x++)
                {
                    SamplePoint(offset, height, y, x);
                }
            }
        }

        public void SamplePoint(Vector2 offset, float height, int y, int x)
        {
            float sampleX = (offset.x + x) / noiseScale;
            float sampleY = (offset.y + y) / noiseScale;
            float perlinValue = Mathf.PerlinNoise(sampleX, sampleY);
            noiseMap[x, y] = Mathf.RoundToInt(provider.Generator.TerrainCurve.Evaluate(perlinValue) * height);
        }
    }
}
