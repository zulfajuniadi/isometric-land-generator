using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using UnityEngine;
using UnityEngine.Tilemaps;
using Random = UnityEngine.Random;

namespace TilemapGenerator
{
    public class TerrainChunk
    {
        public GameObject GameObject;
        public Tilemap Tilemap;
        public TilemapRenderer Renderer;

        private float[, ] noiseMap;
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

            noiseMap = new float[chunkSize1, chunkSize1];
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
                    spawnerPositions[item.Spawer.GetHashCode()] = new List<Vector4>(2000);
                }
            }
            Setup(mapPosition);
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
                provider.Generator.CachedRenderers[item.Key].RemoveInstances(item.Value);
            }
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
            float bottomHeight = firstBiome.Current.Key;
            float bottomOverHeight = (bottomHeight - 1f) / (float) height;
            bool hasComplained = false;
            Tuple<int, Dictionary<Vector4, TileBase>> biome = null;
            TileBase tile = null;
            for (int y = 0; y < chunkSize; y++)
            {
                for (int x = 0; x < chunkSize; x++)
                {
                    int key = x + chunkSize * y;
                    float c0 = Mathf.Round(noiseMap[x + 1, y + 1] * height);
                    float c1 = Mathf.Round(noiseMap[x + 1, y] * height);
                    float c2 = Mathf.Round(noiseMap[x, y] * height);
                    float c3 = Mathf.Round(noiseMap[x, y + 1] * height);

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
                            noiseMap[x + 1, y + 1] = bottomOverHeight;
                        }
                        if (c1 < bottomHeight)
                        {
                            c1 = bottomHeight - 1;
                            noiseMap[x + 1, y] = bottomOverHeight;
                        }
                        if (c2 < bottomHeight)
                        {
                            c2 = bottomHeight - 1;
                            noiseMap[x, y] = bottomOverHeight;
                        }
                        if (c3 < bottomHeight)
                        {
                            c3 = bottomHeight - 1;
                            noiseMap[x, y + 1] = bottomOverHeight;
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
                    positions[key] = new Vector3Int(x - halfMap, y - halfMap, Mathf.RoundToInt(noiseMap[x, y] * height));
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
                    float sampleX = (offset.x + x) / noiseScale;
                    float sampleY = (offset.y + y) / noiseScale;
                    float perlinValue = Mathf.PerlinNoise(sampleX, sampleY);
                    noiseMap[x, y] = perlinValue;
                }
            }
        }
    }

    public class ChunkProvider
    {
        public Vector2Int RandomOffset;
        public LandGenerator Generator;

        private Dictionary<Vector2Int, TerrainChunk> chunks = new Dictionary<Vector2Int, TerrainChunk>();
        private Queue<Vector2Int> chunkQueue = new Queue<Vector2Int>();
        private Queue<TerrainChunk> freeChunkQueue = new Queue<TerrainChunk>();
        private Transform output;

        public ChunkProvider(
            LandGenerator generator
        )
        {
            this.Generator = generator;
            this.output = generator.Output;
        }

        public IEnumerator Tick()
        {
            WaitForSeconds wait = new WaitForSeconds(0.1f);
            while (true)
            {
                yield return wait;
                yield return DetectBoundries();
            }
        }

        private IEnumerator DetectBoundries()
        {
            Vector2Int caddyPosition = GetCaddyPosition();

            if (!chunks.ContainsKey(caddyPosition))
            {
                chunkQueue.Enqueue(caddyPosition);
            }

            Vector2Int top = caddyPosition,
                bottom = caddyPosition,
                left = caddyPosition,
                right = caddyPosition,
                topLeft = caddyPosition,
                topRight = caddyPosition,
                bottomLeft = caddyPosition,
                bottomRight = caddyPosition;

            top.y += 1;
            bottom.y -= 1;
            left.x -= 1;
            right.x += 1;
            topLeft.x -= 1;
            topLeft.y += 1;
            topRight.x += 1;
            topRight.y += 1;
            bottomLeft.x -= 1;
            bottomLeft.y -= 1;
            bottomRight.x += 1;
            bottomRight.y -= 1;

            if (!chunks.ContainsKey(top))
                chunkQueue.Enqueue(top);

            if (!chunks.ContainsKey(bottom))
                chunkQueue.Enqueue(bottom);

            if (!chunks.ContainsKey(left))
                chunkQueue.Enqueue(left);

            if (!chunks.ContainsKey(right))
                chunkQueue.Enqueue(right);

            if (!chunks.ContainsKey(topLeft))
                chunkQueue.Enqueue(topLeft);

            if (!chunks.ContainsKey(topRight))
                chunkQueue.Enqueue(topRight);

            if (!chunks.ContainsKey(bottomLeft))
                chunkQueue.Enqueue(bottomLeft);

            if (!chunks.ContainsKey(bottomRight))
                chunkQueue.Enqueue(bottomRight);

            while (chunkQueue.Count > 0)
            {
                CreateChunk(chunkQueue.Dequeue());
                yield return null;
            }

            while (chunks.Count > Generator.ActiveTilemaps)
            {
                float farthest = float.MinValue;
                TerrainChunk farthestChunk = null;
                Vector2Int key = Vector2Int.zero;
                foreach (var chunkKV in chunks)
                {
                    float dist;
                    if ((dist = Vector2.Distance(caddyPosition, chunkKV.Key)) > farthest)
                    {
                        farthestChunk = chunkKV.Value;
                        farthest = dist;
                        key = chunkKV.Key;
                    }
                }
                if (farthestChunk != null)
                {
                    farthestChunk.Disable();
                    freeChunkQueue.Enqueue(farthestChunk);
                    chunks.Remove(key);
                }
                yield return null;
            }
        }

        private Vector2Int GetCaddyPosition()
        {
            int halfMap = Generator.ChunkSize / 2;
            int quartMap = Generator.ChunkSize / 4;
            Vector2Int caddyPosition = Vector2Int.zero;
            caddyPosition.x = -Mathf.RoundToInt((output.position.x / halfMap + output.position.y / quartMap) / 2);
            caddyPosition.y = -Mathf.RoundToInt((output.position.y / quartMap - (output.position.x / halfMap)) / 2);
            return caddyPosition;
        }

        private void CreateChunk(Vector2Int mapPosition)
        {
            if (freeChunkQueue.Count > 0)
            {
                var chunk = freeChunkQueue.Dequeue();
                chunk.Setup(mapPosition);
                chunks.Add(mapPosition, chunk);
            }
            else
            {
                var chunk = new TerrainChunk(this, mapPosition);
                chunks.Add(mapPosition, chunk);
            }
        }

        public void Clear()
        {
            chunks.Clear();
            chunkQueue.Clear();
        }
    }
}
