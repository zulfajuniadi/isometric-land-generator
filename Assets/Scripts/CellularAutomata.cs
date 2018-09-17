using UnityEngine;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;

/// <summary>
/// Cellular Automata using Unity Job System
/// </summary>
/// <example> 
/// This sample shows how to call the <see cref="Run"/> method.
/// <code>
/// class TestClass : MonoBehaviour
/// {
///     void Start() 
///     {
///         var cellularAutomata = new CellularAutomata ();
///         var trees = cellularAutomata.Run (194, 194, 55, 5, 1.5f, new Vector2 (1.5f, 1.5f), 100, false);
///         for (int i = 0; i < trees.Length; i++)
///         {
///             var tree = trees[i];
///             generateTrees(new Vector3(tree.x, 0, tree.y));
///         }
///         trees.Dispose ();
///     }
/// }
/// </code>
/// </example>
public class CellularAutomata
{
    [BurstCompile]
    private struct CellularAutomataJob : IJobParallelFor
    {
        [ReadOnly] public NativeHashMap<int, int> input;
        [WriteOnly] public NativeHashMap<int, int>.Concurrent output;
        public CellularAutomata.GridUtils gridUtils;
        public void Execute(int key)
        {
            var position = gridUtils.GetPosition(key);
            var neighbors = GetNeighbors(position);
            if (neighbors > 4)
            {
                output.TryAdd(key, 1);
            }
            else if (neighbors < 4)
            {
                output.TryAdd(key, 0);
            }
            else if (input.TryGetValue(key, out int exsiting))
            {
                output.TryAdd(key, exsiting);
            }
            else
            {
                output.TryAdd(key, 0);
            }
        }

        int GetNeighbors(Vector2Int position)
        {
            int wallCount = 0;
            for (int neighbourX = position.x - 1; neighbourX <= position.x + 1; neighbourX++)
            {
                for (int neighbourY = position.y - 1; neighbourY <= position.y + 1; neighbourY++)
                {
                    if (neighbourX >= 0 && neighbourX < gridUtils.gridWidth && neighbourY >= 0 && neighbourY < gridUtils.gridLength)
                    {
                        var key = gridUtils.GetKey(neighbourX, neighbourY);
                        if (neighbourX != position.x || neighbourY != position.y)
                        {
                            if (input.TryGetValue(key, out int existing))
                            {
                                wallCount += existing;
                            }
                        }
                    }
                    else
                    {
                        wallCount++;
                    }
                }
            }
            return wallCount;
        }
    }

    /// <summary>
    /// Run the cellular automator job
    /// </summary>
    /// <param name="gridWidth">The grid width</param>
    /// <param name="gridLength">The grid length</param>
    /// <param name="fillPercentage">Initial fill percentage. 50 +- 10 is a good range</param>
    /// <param name="iterations">How many iterations the cleanup cycle is ran. 5 +- 2 is a good range</param>
    /// <param name="seed">The random seed of the generation</param>
    /// <param name="inverse">Inverse the job's results. Set false to generate Caves, true to generate Islands</param>
    /// <returns>NativeList<Vector2></returns>
    public NativeHashMap<int, int> Generate(
        ref NativeHashMap<int, int> map,
        int gridWidth,
        int gridLength,
        int fillPercentage = 50,
        int iterations = 5,
        int seed = 100,
        bool inverse = false
    )
    {

        System.Random pseudoRandom = new System.Random(seed.GetHashCode());

        int keyLength = gridWidth * gridLength;
        var gridUtils = new GridUtils { gridWidth = gridWidth, gridLength = gridLength, keyLength = keyLength };
        NativeHashMap<int, int> input = new NativeHashMap<int, int>(keyLength, Allocator.TempJob);

        for (int x = 0; x < gridWidth; x++)
        {
            for (int y = 0; y < gridLength; y++)
            {
                var key = gridUtils.GetKey(x, y);
                map.TryGetValue(key, out int result);
                if (
                    x == 0 ||
                    x == gridWidth - 1 ||
                    y == 0 ||
                    y == gridLength - 1 ||
                    x == 1 ||
                    x == gridWidth - 2 ||
                    y == 1 ||
                    y == gridLength - 2
                )
                {
                    if (inverse)
                    {
                        input.TryAdd(key, 0);
                    }
                    else if (result == 1)
                    {
                        input.TryAdd(key, 1);
                    }
                }
                else if (
                    result == 1 &&
                    (
                        pseudoRandom.Next(0, 100) <= fillPercentage
                    )
                )
                {
                    input.TryAdd(key, 1);
                }
                else
                {
                    input.TryAdd(key, 0);
                }
            }
        }

        map.Dispose();

        NativeHashMap<int, int> output = new NativeHashMap<int, int>(keyLength, Allocator.TempJob);

        if (iterations == 0)
        {
            output.Dispose();
            output = input;
        }
        else
        {
            for (int i = 1; i <= iterations; i++)
            {
                new CellularAutomataJob { input = input, output = output, gridUtils = gridUtils }.Schedule(keyLength, 64).Complete();
                input.Dispose();
                input = output;
                if (i != iterations)
                {
                    output = new NativeHashMap<int, int>(keyLength, Allocator.TempJob);
                }
            }
        }

        //     for (int k = 0; k < keyLength; k++)
        //     {
        //         if (output.TryGetValue(k, out int result))
        //         {
        //             output.TryAdd(k, result == 1 ? 0 : 1);
        //         }
        //     }

        // be sure to dispose the results!!!
        return output;
    }

    /// <summary>
    /// Generic grid utils functions
    /// </summary>
    public struct GridUtils
    {
        public int gridWidth;
        public int gridLength;
        public int keyLength;

        public Vector2Int GetPosition(int key)
        {
            var pos = new Vector2Int();
            pos.x = key % gridWidth;
            pos.y = Mathf.FloorToInt(key / gridWidth);
            return pos;
        }

        public int GetKey(Vector2Int position)
        {
            return position.y * gridWidth + position.x;
        }

        public int GetKey(int x, int y)
        {
            return y * gridWidth + x;
        }
    }
}
