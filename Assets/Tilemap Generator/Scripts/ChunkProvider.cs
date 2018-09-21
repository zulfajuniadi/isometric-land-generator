using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace TilemapGenerator
{
    public class ChunkProvider
    {
        public Vector2Int RandomOffset;
        public LandGenerator Generator;

        private Dictionary<Vector2Int, TerrainChunk> chunks = new Dictionary<Vector2Int, TerrainChunk>();
        private Queue<Vector2Int> chunkQueue = new Queue<Vector2Int>();
        private Queue<TerrainChunk> freeChunkQueue = new Queue<TerrainChunk>();
        private Transform output;
        private bool HasWarmedUp = false;

        public ChunkProvider(
            LandGenerator generator
        )
        {
            this.Generator = generator;
            this.output = generator.Output;
        }

        public IEnumerator Tick()
        {
            if (!HasWarmedUp)
            {
                for (int i = 0; i < Generator.ActiveTilemaps * 1.5; i++)
                {
                    var chunk = new TerrainChunk(this, new Vector2Int(0, 0));
                    chunk.Disable();
                    freeChunkQueue.Enqueue(chunk);
                }
                HasWarmedUp = true;
            }
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

        private TerrainChunk CreateChunk(Vector2Int mapPosition)
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
                var chunk = new TerrainChunk(this, mapPosition);
                chunks.Add(mapPosition, chunk);
                return chunk;
            }
        }

        public void Clear()
        {
            chunks.Clear();
            chunkQueue.Clear();
        }
    }
}
