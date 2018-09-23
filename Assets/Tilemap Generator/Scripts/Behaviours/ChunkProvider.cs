using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace TilemapGenerator.Behaviours
{
    [ExecuteInEditMode]
    public class ChunkProvider : MonoBehaviour
    {
        public Vector3Int RandomOffset;
        public LandGenerator Generator;
        public GameObject ChunkPrefab;

        private Dictionary<Vector3Int, TerrainChunk> chunks = new Dictionary<Vector3Int, TerrainChunk>();
        private Queue<Vector3Int> chunkQueue = new Queue<Vector3Int>();
        private Queue<TerrainChunk> freeChunkQueue = new Queue<TerrainChunk>();
        private float lastTick;

        public void Boot()
        {
            while (Generator.Output.childCount > 0)
                DestroyImmediate(Generator.Output.GetChild(0).gameObject);
            chunks.Clear();
            freeChunkQueue.Clear();
            chunkQueue.Clear();

            for (int i = 0; i < Generator.ActiveTilemaps * 1.5; i++)
            {
                var chunk = CreateNewChunk(new Vector3Int(0, 0, 0));
                freeChunkQueue.Enqueue(chunk);
            }
        }

        private void Update()
        {
            Vector3Int caddyPosition = GetCameraChunkPosition();

            if (!chunks.ContainsKey(caddyPosition))
            {
                chunkQueue.Enqueue(caddyPosition);
            }

            Vector3Int top = caddyPosition,
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
            }

            while (chunks.Count > Generator.ActiveTilemaps)
            {
                float farthest = float.MinValue;
                TerrainChunk farthestChunk = null;
                Vector3Int key = Vector3Int.zero;
                foreach (var chunkKV in chunks)
                {
                    float dist;
                    if ((dist = Vector3Int.Distance(caddyPosition, chunkKV.Key)) > farthest)
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
            }
        }

        private Vector3Int GetCameraChunkPosition()
        {
            Vector3 mapPosition = Generator.WorldToMap(Generator.MainCamera.transform.position);
            Vector3Int chunkPosition = Vector3Int.zero;
            chunkPosition.x = Mathf.RoundToInt((mapPosition.x - Generator.ChunkSize / 2) / Generator.ChunkSize);
            chunkPosition.y = Mathf.RoundToInt((mapPosition.y - Generator.ChunkSize / 2) / Generator.ChunkSize);
            return chunkPosition;
        }

        private TerrainChunk CreateChunk(Vector3Int mapPosition)
        {
            if (freeChunkQueue.Count > 0)
            {
                var chunk = freeChunkQueue.Dequeue();
                chunk.Setup(mapPosition);
                chunks.Add(mapPosition, chunk);
                return chunk;
            }
            else
            {
                TerrainChunk chunk = CreateNewChunk(mapPosition);
                chunks.Add(mapPosition, chunk);
                return chunk;
            }
        }

        private TerrainChunk CreateNewChunk(Vector3Int mapPosition)
        {
            GameObject go = Instantiate(ChunkPrefab);
            go.hideFlags = HideFlags.DontSave;
            TerrainChunk chunk = go.GetComponent<TerrainChunk>();
            chunk.transform.parent = Generator.Output;
            chunk.Provider = this;
            go.name = "Disabled";
            go.SetActive(false);
            return chunk;
        }
    }
}
