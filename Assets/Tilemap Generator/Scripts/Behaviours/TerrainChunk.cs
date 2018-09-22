using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using UnityEngine;
using UnityEngine.Tilemaps;
using TilemapGenerator.Settings;

namespace TilemapGenerator.Behaviours
{
    public class TerrainChunk : MonoBehaviour
    {
        public Tilemap Tilemap;
        public TilemapRenderer Renderer;
        public ChunkProvider Provider;

        private int[, ] noiseMap;
        private TileBase[] tiles;
        private Vector3Int[] positions;
        private RectInt rect;
        private Vector3Int randomOffset;
        private Vector3Int mapPosition;
        private Dictionary<float, Tuple<int, Dictionary<Vector4, TileBase>>> cachedBiomes;
        private Dictionary<int, List<Vector4>> spawnerPositions;
        private Dictionary<int, List<Vector4>> tempSpawnerPositions;
        private Dictionary<int, Dictionary<int, SpawnerProbability>> cachedSpawners;
        private int height;
        private int chunkSize;
        private int chunkSize1;
        private int halfMap;
        private float noiseScale;
        private Transform output;
        private System.Random prng;
        Tuple<int, Dictionary<Vector4, TileBase>> biome = null;
        TileBase tile = null;
        Vector4 flat = new Vector4(0, 0, 0, 0);
        int bottomHeight;
        int bottomMinus1;
        int subChunkSize = 8;

        private void Boot()
        {
            this.cachedBiomes = Provider.Generator.CachedBiomes;
            this.spawnerPositions = new Dictionary<int, List<Vector4>>();
            this.tempSpawnerPositions = new Dictionary<int, List<Vector4>>();
            this.cachedSpawners = new Dictionary<int, Dictionary<int, SpawnerProbability>>();
            this.randomOffset = Provider.RandomOffset;
            this.height = Provider.Generator.Height;
            this.noiseScale = Provider.Generator.NoiseScale;
            this.chunkSize = Provider.Generator.ChunkSize;
            this.halfMap = chunkSize / 2;
            this.output = Provider.Generator.Output;
            this.chunkSize1 = chunkSize + 1;
            if (Application.isPlaying)
            {
                positions = new Vector3Int[subChunkSize * subChunkSize];
                tiles = new TileBase[subChunkSize * subChunkSize];
            }
            else
            {
                positions = new Vector3Int[chunkSize * chunkSize];
                tiles = new TileBase[chunkSize * chunkSize];
                subChunkSize = chunkSize;
            }
            noiseMap = new int[chunkSize1, chunkSize1];
            Renderer.detectChunkCullingBounds = TilemapRenderer.DetectChunkCullingBounds.Manual;
            Renderer.chunkCullingBounds = Vector3.one * chunkSize / 2;
            foreach (var biome in Provider.Generator.BiomeConfigs)
            {
                int biomeHash = biome.TileConfig.GetHashCode();
                if (!cachedSpawners.ContainsKey(biomeHash))
                    cachedSpawners.Add(biomeHash, new Dictionary<int, SpawnerProbability>());
                foreach (var item in biome.Spawners)
                {
                    cachedSpawners[biomeHash].Add(item.Spawer.GetHashCode(), item);
                    spawnerPositions[item.Spawer.GetHashCode()] = new List<Vector4>((int) (chunkSize * chunkSize * item.Probability));
                    tempSpawnerPositions[item.Spawer.GetHashCode()] = new List<Vector4>((int) (subChunkSize * subChunkSize * item.Probability));
                }
            }
            var firstBiome = cachedBiomes.GetEnumerator();
            firstBiome.MoveNext();
            bottomHeight = Mathf.RoundToInt(firstBiome.Current.Key);
            bottomMinus1 = bottomHeight - 1;
        }

        public void Setup(Vector3Int mapPosition)
        {
            if (cachedBiomes == null)
            {
                Boot();
            }
            Vector3Int baseOffset = mapPosition * chunkSize;
            this.rect = new RectInt(new Vector2Int(baseOffset.x, baseOffset.y), new Vector2Int(chunkSize, chunkSize));
            prng = new System.Random(((Vector2Int) randomOffset + this.rect.min).GetHashCode());
            float halfSize = chunkSize / 2f;
            Tilemap.ClearAllTiles();
            gameObject.name = rect.min.ToString();
            foreach (var item in spawnerPositions)
            {
                item.Value.Clear();
            }
            foreach (var item in tempSpawnerPositions)
            {
                item.Value.Clear();
            }
            Vector2 position = new Vector2(
                (baseOffset.x / chunkSize - baseOffset.y / chunkSize) * halfSize,
                (baseOffset.x / chunkSize + baseOffset.y / chunkSize) * halfSize / 2f
            );
            transform.position = position;

            gameObject.SetActive(true);
            BuildMap();
        }

        public void Disable()
        {
            foreach (var item in spawnerPositions)
            {
                if (Provider.Generator.CachedRenderers.ContainsKey(item.Key))
                    Provider.Generator.CachedRenderers[item.Key].RemoveInstances(item.Value);
            }
            gameObject.name = "Disabled";
            gameObject.SetActive(false);
        }

        private void BuildMap()
        {
            GenerateNoiseMap();
            if (Application.isPlaying)
            {
                StartCoroutine(BuildMapGradual());
            }
            else
            {
                BuildMapOffset(0, 0);
            }
        }

        private IEnumerator BuildMapGradual()
        {
            var direction = (transform.position - Provider.transform.position).normalized;
            if (Mathf.Abs(direction.x) > Mathf.Abs(direction.y))
            {
                if (direction.x > 0)
                {
                    for (int offsetX = 0; offsetX < chunkSize; offsetX += subChunkSize)
                    {
                        for (int offsetY = 0; offsetY < chunkSize; offsetY += subChunkSize)
                        {
                            BuildMapOffset(offsetX, offsetY);
                            yield return null;
                        }
                    }
                }
                else
                {
                    for (int offsetX = chunkSize - subChunkSize; offsetX > -1; offsetX -= subChunkSize)
                    {
                        for (int offsetY = 0; offsetY < chunkSize; offsetY += subChunkSize)
                        {
                            BuildMapOffset(offsetX, offsetY);
                            yield return null;
                        }
                    }
                }
            }
            else
            {
                if (direction.y > 0)
                {
                    for (int offsetY = 0; offsetY < chunkSize; offsetY += subChunkSize)
                    {
                        for (int offsetX = 0; offsetX < chunkSize; offsetX += subChunkSize)
                        {
                            BuildMapOffset(offsetX, offsetY);
                            yield return null;
                        }
                    }
                }
                else
                {
                    for (int offsetY = chunkSize - subChunkSize; offsetY > -1; offsetY -= subChunkSize)
                    {
                        for (int offsetX = 0; offsetX < chunkSize; offsetX += subChunkSize)
                        {
                            BuildMapOffset(offsetX, offsetY);
                            yield return null;
                        }
                    }
                }
            }
        }

        private void BuildMapOffset(int offsetX, int offsetY)
        {
            foreach (var item in tempSpawnerPositions)
            {
                item.Value.Clear();
            }
            bool hasComplained = false;
            int maxX = offsetX + subChunkSize;
            int maxY = offsetY + subChunkSize;
            for (int y = offsetY; y < maxY; y++)
            {
                for (int x = offsetX; x < maxX; x++)
                {
                    int key = x - offsetX + subChunkSize * (y - offsetY);
                    float c0 = noiseMap[x + 1, y + 1];
                    float c1 = noiseMap[x + 1, y];
                    float c2 = noiseMap[x, y];
                    float c3 = noiseMap[x, y + 1];

                    float highest = 0;
                    float lowest = 0;
                    Utils.MinMaxCorners(c0, c1, c2, c3, out highest, out lowest);
                    biome = GetTileLookup(lowest);
                    int biomeHash = biome.Item1;
                    bool isSloped = false;
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
                        Utils.MinMaxCorners(c0, c1, c2, c3, out highest, out lowest);
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
                            Debug.LogWarning("Unmatched corner: " + corners + " at height: " + highest);
                        }
                        isSloped = true;
                    }
                    foreach (var item in cachedSpawners[biomeHash])
                    {
                        if (
                            item.Value.Spawer.Enabled &&
                            prng.NextDouble() <= item.Value.Probability &&
                            (
                                (item.Value.Spawer.OnSlopes && isSloped) ||
                                (item.Value.Spawer.OnFlat && !isSloped)
                            )
                        )
                        {
                            float textureCount = (float) item.Value.Spawer.PackedTexture.depth;
                            float index = Mathf.Round((float) prng.NextDouble() * textureCount) / textureCount + (0.5f * 1f / textureCount);
                            Vector4 worldPos = Provider.Generator.MapToWorld(new Vector3(rect.min.x + x, rect.min.y + y, highest));
                            worldPos.w = index;
                            spawnerPositions[item.Key].Add(worldPos);
                            tempSpawnerPositions[item.Key].Add(worldPos);
                        }
                    }
                    positions[key] = new Vector3Int(x - halfMap, y - halfMap, noiseMap[x, y]);
                    tiles[key] = tile;
                }
            }
            Tilemap.SetTiles(positions, tiles);
            foreach (var item in tempSpawnerPositions)
            {
                Provider.Generator.CachedRenderers[item.Key].AddInstances(item.Value);
            }
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

        public void GenerateNoiseMap()
        {
            for (int y = 0; y < chunkSize1; y++)
            {
                for (int x = 0; x < chunkSize1; x++)
                {
                    Vector2 point = rect.min;
                    point.x += x;
                    point.y += y;
                    noiseMap[x, y] = Mathf.RoundToInt(Provider.Generator.SampleMapHeight(point));
                }
            }
        }
    }
}
