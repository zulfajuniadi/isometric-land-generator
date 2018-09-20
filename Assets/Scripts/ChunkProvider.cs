using System;
using System.Collections;
using System.Collections.Generic;
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
        private Dictionary<Vector4, TileBase> landTilesLookup;
        private Dictionary<Vector4, TileBase> waterTilesLookup;
        private List<Vector4> trees;
        private int height;
        private int waterHeight;
        private int size;
        private int size1;
        private float scale;
        private Transform caddy;
        private InstancedRenderer treesRenderer;
        private System.Random prng;

        public TerrainChunk(
            Transform caddy,
            Vector2Int baseOffset,
            Vector2Int randomOffset,
            int size,
            int height,
            int waterHeight,
            float scale,
            Dictionary<Vector4, TileBase> landTilesLookup,
            Dictionary<Vector4, TileBase> waterTilesLookup,
            InstancedRenderer treesRenderer
        )
        {
            this.landTilesLookup = landTilesLookup;
            this.waterTilesLookup = waterTilesLookup;
            this.treesRenderer = treesRenderer;
            this.randomOffset = randomOffset;
            this.height = height;
            this.waterHeight = waterHeight;
            this.scale = scale;
            this.size = size;
            size1 = size + 1;
            float halfSize = size / 2f;
            this.caddy = caddy;
            noiseMap = new float[size1, size1];
            GameObject = new GameObject();
            GameObject.transform.parent = this.caddy;
            Tilemap = GameObject.AddComponent<Tilemap>();
            Renderer = GameObject.AddComponent<TilemapRenderer>();
            Renderer.detectChunkCullingBounds = TilemapRenderer.DetectChunkCullingBounds.Manual;
            Renderer.chunkCullingBounds = Vector3.one * halfSize;
            positions = new Vector3Int[size1 * size1];
            tiles = new TileBase[size1 * size1];
            trees = new List<Vector4>(size * size);
            Setup(baseOffset);
        }

        public void Setup(Vector2Int baseOffset)
        {
            this.rect = new RectInt(baseOffset, new Vector2Int(size, size));
            prng = new System.Random((randomOffset + this.rect.min).GetHashCode());
            trees.Clear();
            float halfSize = size / 2f;
            Tilemap.ClearAllTiles();
            GameObject.name = rect.min.ToString();
            BuildPositions();
            Vector2 position = new Vector2(
                (baseOffset.x / size - baseOffset.y / size) * halfSize,
                (baseOffset.x / size + baseOffset.y / size) * halfSize / 2f
            );
            GameObject.transform.localPosition = position;
            Tilemap.SetTiles(positions, tiles);
            treesRenderer.AddInstances(trees);
            GameObject.SetActive(true);
        }

        public void Disable()
        {
            GameObject.SetActive(false);
            treesRenderer.RemoveInstances(trees);
        }

        private void BuildPositions()
        {
            GenerateNoiseMap(randomOffset + rect.min);
            int halfMap = size / 2;
            Vector4 flat = new Vector4(0, 0, 0, 0);
            TileBase defaultLandTile = landTilesLookup[flat];
            TileBase defaultWaterTile = waterTilesLookup[flat];
            float waterOverHeight = ((float) waterHeight - 1) / height;
            bool hasComplained = false;
            float offsetX = (rect.min.x - rect.min.y) / 2f;
            float offsetY = (rect.min.y + rect.min.x) / 4f;
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    int key = x + size * y;
                    float c0 = Mathf.Round(noiseMap[x + 1, y + 1] * height);
                    float c1 = Mathf.Round(noiseMap[x + 1, y] * height);
                    float c2 = Mathf.Round(noiseMap[x, y] * height);
                    float c3 = Mathf.Round(noiseMap[x, y + 1] * height);
                    float highest = Mathf.Ceil(Mathf.Max(c0, c1, c2, c3));
                    float lowest = Mathf.Floor(Mathf.Min(c0, c1, c2, c3));
                    Dictionary<Vector4, TileBase> tileLookup = landTilesLookup;
                    bool isWater = false;
                    TileBase tile = defaultLandTile;
                    if (lowest < waterHeight)
                    {
                        tileLookup = waterTilesLookup;
                        isWater = true;
                        tile = defaultWaterTile;
                        if (c0 < waterHeight)
                        {
                            c0 = waterHeight - 1;
                            noiseMap[x + 1, y + 1] = waterOverHeight;
                        }
                        if (c1 < waterHeight)
                        {
                            c1 = waterHeight - 1;
                            noiseMap[x + 1, y] = waterOverHeight;
                        }
                        if (c2 < waterHeight)
                        {
                            c2 = waterHeight - 1;
                            noiseMap[x, y] = waterOverHeight;
                        }
                        if (c3 < waterHeight)
                        {
                            c3 = waterHeight - 1;
                            noiseMap[x, y + 1] = waterOverHeight;
                        }
                        lowest = Mathf.Floor(Mathf.Min(c0, c1, c2, c3));
                        highest = Mathf.Ceil(Mathf.Max(c0, c1, c2, c3));
                    }
                    if (highest != lowest)
                    {
                        float c0n = Mathf.InverseLerp(lowest, highest, c0);
                        float c1n = Mathf.InverseLerp(lowest, highest, c1);
                        float c2n = Mathf.InverseLerp(lowest, highest, c2);
                        float c3n = Mathf.InverseLerp(lowest, highest, c3);
                        Vector4 corners = new Vector4(c0n, c1n, c2n, c3n);
                        if (tileLookup.ContainsKey(corners))
                        {
                            tile = tileLookup[corners];
                        }
                        else if (!hasComplained)
                        {
                            hasComplained = true;
                            Debug.Log(isWater);
                            Debug.Log(corners);
                        }
                    }
                    if (!isWater && prng.NextDouble() > 0.75f)
                    {
                        float treeIndex = Mathf.InverseLerp(0, 0.25f, (float) prng.NextDouble());
                        float mapX = x - Mathf.InverseLerp(0f, 0.1f, (float) prng.NextDouble());
                        float mapY = y - Mathf.InverseLerp(0f, 0.1f, (float) prng.NextDouble());
                        float worldX = offsetX + ((mapX - mapY) / 2f) - (c0 / height);
                        float worldY = offsetY + ((mapX + mapY) / 4f) + (c0 / height);
                        trees.Add(new Vector4(worldX, worldY - (size / 4f - 1f), 0, treeIndex));
                    }
                    positions[key] = new Vector3Int(x - halfMap, y - halfMap, Mathf.RoundToInt(noiseMap[x, y] * height));
                    tiles[key] = tile;
                }
            }
        }

        public void GenerateNoiseMap(Vector2 offset)
        {
            for (int y = 0; y < size1; y++)
            {
                for (int x = 0; x < size1; x++)
                {
                    float sampleX = (offset.x + x) / scale;
                    float sampleY = (offset.y + y) / scale;
                    float perlinValue = Mathf.PerlinNoise(sampleX, sampleY);
                    noiseMap[x, y] = perlinValue;
                }
            }
        }
    }

    public class ChunkProvider
    {
        public int Height;
        public float NoiseScale;
        public Vector2Int RandomOffset;
        public int ChunkSize;

        private Dictionary<Vector2Int, TerrainChunk> chunks = new Dictionary<Vector2Int, TerrainChunk>();
        private Dictionary<Vector4, TileBase> landTiles;
        private Dictionary<Vector4, TileBase> waterTiles;
        private Queue<Vector2Int> chunkQueue = new Queue<Vector2Int>();
        private Queue<TerrainChunk> freeChunkQueue = new Queue<TerrainChunk>();
        private Transform caddy;
        private int waterHeight;
        private InstancedRenderer treesRenderer;

        public ChunkProvider(
            Transform caddy,
            int waterHeight,
            Dictionary<Vector4, TileBase> cachedLandTiles,
            Dictionary<Vector4, TileBase> cachedWaterTiles,
            InstancedRenderer treesRenderer
        )
        {
            landTiles = cachedLandTiles;
            waterTiles = cachedWaterTiles;
            this.caddy = caddy;
            this.waterHeight = waterHeight;
            this.treesRenderer = treesRenderer;
        }

        public IEnumerator Tick()
        {
            WaitForSeconds wait = new WaitForSeconds(0.1f);
            while (true)
            {
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

            while (chunks.Count > 9)
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
            int halfMap = ChunkSize / 2;
            int quartMap = ChunkSize / 4;
            Vector2Int caddyPosition = Vector2Int.zero;
            caddyPosition.x = -Mathf.RoundToInt((caddy.position.x / halfMap + caddy.position.y / quartMap) / 2);
            caddyPosition.y = -Mathf.RoundToInt((caddy.position.y / quartMap - (caddy.position.x / halfMap)) / 2);
            return caddyPosition;
        }

        private void CreateChunk(Vector2Int mapPosition)
        {
            if (freeChunkQueue.Count > 0)
            {
                var chunk = freeChunkQueue.Dequeue();
                chunk.Setup(mapPosition * ChunkSize);
                chunks.Add(mapPosition, chunk);
            }
            else
            {
                var chunk = new TerrainChunk(caddy, mapPosition * ChunkSize, RandomOffset, ChunkSize, Height, waterHeight, NoiseScale, landTiles, waterTiles, treesRenderer);
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
