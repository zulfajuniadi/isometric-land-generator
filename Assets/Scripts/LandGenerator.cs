using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;
using Unity.Collections;
using Random = UnityEngine.Random;

namespace TilemapGenerator
{
    [System.Serializable]
    public class LandLayerConfig
    {
        [Range(0, 100)]
        public int fill = 50;
    }

    public class LandGenerator : MonoBehaviour
    {
        public Grid Output;
        public int Simulations = 8;
        public Vector2Int Size = new Vector2Int(512, 512);
        public Vector2Int Scale = new Vector2Int(8, 8);
        public LandLayerConfig[] Layers = new LandLayerConfig[0];
        public Tilemap[] Tilemaps;
        public List<int[]> Generated = new List<int[]>();
        public int Seed = 100;
        public float DeltaHeight = 0.1f;
        public TileBase Ground;

        int totalSize;

        public void Generate()
        {
            Clear();
            totalSize = Size.x * Size.y;
            Tilemaps = new Tilemap[Layers.Length];
            Output.cellSize = new Vector3(1, 0.5f, 1);
            Random.InitState(Seed);

            NativeHashMap<int, int> map = new NativeHashMap<int, int>(totalSize, Allocator.Temp);
            for (int i = 0; i < Size.x; i++)
            {
                for (int j = 0; j < Size.y; j++)
                {
                    map.TryAdd(i + Size.x * j, 1);
                }
            }
            CellularAutomata ca = new CellularAutomata();
            NativeHashMap<int, int> results = new NativeHashMap<int, int>(totalSize, Allocator.TempJob);
            results.Dispose();
            for (int i = 0; i < Layers.Length; i++)
            {
                var go = new GameObject("Layer_" + i);
                go.transform.parent = Output.transform;
                // go.transform.position = new Vector3(0, DeltaHeight * i, 0);
                var tilemap = go.AddComponent<Tilemap>();
                var renderer = go.AddComponent<TilemapRenderer>();
                renderer.sortingOrder = i;
                renderer.mode = TilemapRenderer.Mode.Individual;

                Tilemaps[i] = tilemap;
                LandLayerConfig layer = Layers[i];
                if (layer == null) continue;

                results = ca.Generate(ref map, Size.x, Size.y, layer.fill, 8, Seed);
                map = results;

                int[] generatedLayer = new int[totalSize];
                int count = 0;
                for (int x = 0; x < totalSize; x++)
                {
                    if (map.TryGetValue(x, out int result))
                    {
                        generatedLayer[x] = result == 1 ? 0 : 1;
                        if (generatedLayer[x] == 1)
                            count++;
                    }
                    else
                    {
                        generatedLayer[x] = 0;
                    }
                }
                // Debug.Log(i + " " + count);
                // Dump(generatedLayer);
                Generated.Add(generatedLayer);
            }
            results.Dispose();

            bool[] placed = new bool[totalSize];
            for (int i = 0; i < Generated.Count; i++)
            {
                var baseMap = Tilemaps[Generated.Count - i - 1];
                var generatedMap = Generated[i];
                for (int j = 0; j < totalSize; j++)
                {
                    if (generatedMap[j] == 1 && !placed[j])
                    {
                        placed[j] = true;
                        int x = Mathf.RoundToInt(j / Size.x);
                        int y = j % Size.x;
                        Vector3Int position = new Vector3Int(x, y, 0);
                        baseMap.SetTile(position, Ground);
                    }
                }
            }
        }

        private void Dump(int[] map)
        {
            string str = "";
            for (int i = 0; i < totalSize; i++)
            {
                if (i % Size.x == 0)
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
