using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;
using Unity.Collections;
using Random = UnityEngine.Random;
using System.Runtime.CompilerServices;

namespace TilemapGenerator
{
    [System.Serializable]
    public class LandLayerConfig
    {
        [Range(0, 100)]
        public int fill = 50;
    }

    [ExecuteInEditMode]
    public class LandGenerator : MonoBehaviour
    {
        public Grid Output;
        public int Simulations = 8;
        public int Size = 128;
        [Range(1, 4)]
        public int Scale = 4;
        public LandLayerConfig[] Layers = new LandLayerConfig[0];
        public Tilemap[] Tilemaps;
        public List<int[]> Generated = new List<int[]>();
        public int Seed = 100;
        public float DeltaHeight = 0.1f;
        public TileBase BaseGround;
        public TileBase Ground;

        int totalSize;

        private void OnEnable()
        {
            Generate();
        }

        private void Start()
        {
            Application.targetFrameRate = 0;
            QualitySettings.antiAliasing = 0;
            QualitySettings.vSyncCount = 0;
        }

        public void Generate()
        {
            Clear();
            totalSize = Size * Size;
            Output.cellSize = new Vector3(1, 0.5f, 1);
            Random.InitState(Seed);

            NativeHashMap<int, int> map = new NativeHashMap<int, int>(totalSize, Allocator.Temp);
            for (int i = 0; i < Size; i++)
            {
                for (int j = 0; j < Size; j++)
                {
                    map.TryAdd(i + Size * j, 1);
                }
            }
            CellularAutomata ca = new CellularAutomata();
            NativeHashMap<int, int> results = new NativeHashMap<int, int>(totalSize, Allocator.TempJob);
            results.Dispose();

            Tilemaps = new Tilemap[Layers.Length];
            int[] counts = new int[Layers.Length];

            GenerateBaseTileMap();

            for (int i = 0; i < Layers.Length; i++)
            {
                LandLayerConfig layer = Layers[i];
                if (layer == null) continue;

                var go = new GameObject("Ground_" + (i + 1));
                go.transform.parent = Output.transform;
                go.transform.position = new Vector3(0, DeltaHeight * (i + 1f), 0);
                var tilemap = go.AddComponent<Tilemap>();
                var renderer = go.AddComponent<TilemapRenderer>();
                renderer.sortingOrder = i + 1;
                Tilemaps[i] = tilemap;

                results = ca.Generate(ref map, Size, Size, layer.fill, Simulations, Seed, true);
                map = results;

                int[] generatedLayer = new int[totalSize];
                for (int index = 0; index < totalSize; index++)
                {
                    if (
                        index != 0 &&
                        index != Size - 1 &&
                        index != totalSize - 1 &&
                        index != totalSize - Size &&
                        map.TryGetValue(index, out int result)
                    )
                    {
                        generatedLayer[index] = result;
                        if (result == 1)
                            counts[i]++;
                    }
                    else
                    {
                        generatedLayer[index] = 0;
                    }
                }
                counts[i] *= Scale * Scale;
                Generated.Add(generatedLayer);
            }
            results.Dispose();

            for (int i = 1; i < Layers.Length; i++)
            {
                var layer = Generated[i];
                var bottom = Generated[i - 1];
                for (int index = 0; index < totalSize; index++)
                {
                    if (index == 0 || index == Size - 1 || index == totalSize - 1 || index == totalSize - Size) continue;
                    if (layer[index] == 1)
                    {
                        if (
                            // (
                            //     bottom[index - 1] == 0 &&
                            //     bottom[index - Size - 1] == 0
                            // ) ||
                            // (
                            //     bottom[index - 1] == 0 &&
                            //     bottom[index - Size - 1] == 1
                            // ) ||
                            bottom[index + 1] == 0 ||
                            bottom[index - Size - 1] == 0 ||
                            (
                                bottom[index + Size - 1] == 0
                            )
                            // bottom[index] == 0 ||
                            // ||
                            // bottom[index - Size + 1] == 0 ||
                            // bottom[index + Size] == 0
                            //  ||
                            // bottom[index + Size + 1] == 0
                        )
                        {
                            layer[index] = 0;
                        }
                    }
                }
            }
            bool hasDiscard = true;
            while (hasDiscard)
            {
                hasDiscard = false;
                for (int i = 0; i < Layers.Length; i++)
                {
                    var layer = Generated[i];
                    for (int index = 0; index < totalSize; index++)
                    {
                        if (index == 0 || index == Size - 1 || index == totalSize - 1 || index == totalSize - Size) continue;
                        if (layer[index] == 1)
                        {
                            int neighbors = 0;
                            if (layer[index - 1] == 1) neighbors++;
                            if (layer[index + 1] == 1) neighbors++;
                            if (layer[index - Size - 1] == 1) neighbors++;
                            if (layer[index - Size] == 1) neighbors++;
                            if (layer[index - Size + 1] == 1) neighbors++;
                            if (layer[index + Size - 1] == 1) neighbors++;
                            if (layer[index + Size] == 1) neighbors++;
                            if (layer[index + Size + 1] == 1) neighbors++;
                            if (
                                neighbors <= 3
                            )
                            {
                                layer[index] = 0;
                                hasDiscard = true;
                            }
                        }
                    }
                }
            }

            for (int i = Generated.Count - 1; i > -1; i--)
            {
                int count = counts[i];
                Vector3Int[] positions = new Vector3Int[count];
                TileBase[] tiles = new TileBase[count];
                if (count <= 0) continue;
                var generatedMap = Generated[i];
                for (int j = 0; j < totalSize; j++)
                {
                    if (generatedMap[j] == 1)
                    {
                        int x = Mathf.RoundToInt(j / Size);
                        int y = j % Size;
                        for (int k = 0; k < Scale; k++)
                        {
                            for (int l = 0; l < Scale; l++)
                            {
                                Vector3Int position = new Vector3Int(x * Scale + k, y * Scale + l, 0);
                                tiles[count - 1] = Ground;
                                positions[count - 1] = position;
                                count--;
                            }
                        }
                    }
                }
                Tilemaps[i].SetTiles(positions, tiles);
            }
        }

        private void GenerateBaseTileMap()
        {
            var go = new GameObject("Ground_" + 0);
            go.transform.parent = Output.transform;
            var tilemap = go.AddComponent<Tilemap>();
            var renderer = go.AddComponent<TilemapRenderer>();
            renderer.sortingOrder = 0;
            var positions = new Vector3Int[Size * Size * Scale * Scale];
            var tiles = new TileBase[Size * Size * Scale * Scale];
            for (int i = 0; i < Size * Scale; i++)
            {
                for (int j = 0; j < Size * Scale; j++)
                {
                    int key = i + Size * Scale * j;
                    positions[key] = new Vector3Int(i, j, 0);
                    tiles[key] = BaseGround;
                }
            }
            tilemap.SetTiles(positions, tiles);
        }

        private void Dump(int[] map)
        {
            string str = "";
            for (int i = 0; i < totalSize; i++)
            {
                if (i % Size == 0)
                {
                    str += "\n";
                }
                if (map[i] == 1)
                {
                    str += "X";
                }
                else
                {
                    str += " ";
                }
            }
            Debug.Log(str);
        }

        private void Clear()
        {
            while (Output.transform.childCount > 0)
            {
                DestroyImmediate(Output.transform.GetChild(0).gameObject);
            }
            Generated.Clear();
        }
    }
}
