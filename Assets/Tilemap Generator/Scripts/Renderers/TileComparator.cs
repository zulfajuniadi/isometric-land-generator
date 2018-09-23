using System.Collections.Generic;
using UnityEngine;

namespace TilemapGenerator
{
    public class TileComparator : IEqualityComparer<Vector4>
    {
        public bool Equals(Vector4 v1, Vector4 v2)
        {
            return v1.Equals(v2);
        }

        public int GetHashCode(Vector4 v1)
        {
            return (int) (v1.x * v1.y);
        }
    }
}
