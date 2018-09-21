using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace TilemapGenerator
{

    [CreateAssetMenu(fileName = "Tile Configuration", menuName = "Tilemap Generator/Tile Config", order = 0)]
    public class TileConfiguration : ScriptableObject
    {
        public TileBase _0000;

        public TileBase _1000;
        public TileBase _0100;
        public TileBase _0010;
        public TileBase _0001;

        public TileBase _1100;
        public TileBase _0110;
        public TileBase _0011;
        public TileBase _1001;

        public TileBase _1110;
        public TileBase _0111;
        public TileBase _1011;
        public TileBase _1101;

        public TileBase _1010;
        public TileBase _0101;

        public TileBase _5150;
        public TileBase _5051;
        public TileBase _0515;
        public TileBase _1505;

        public void GetCacheData(Dictionary<Vector4, TileBase> data)
        {
            data.Clear();
            data.Add(new Vector4(0, 0, 0, 0), _0000);
            data.Add(new Vector4(1, 0, 0, 0), _1000);
            data.Add(new Vector4(0, 1, 0, 0), _0100);
            data.Add(new Vector4(0, 0, 1, 0), _0010);
            data.Add(new Vector4(0, 0, 0, 1), _0001);
            data.Add(new Vector4(1, 1, 0, 0), _1100);
            data.Add(new Vector4(0, 1, 1, 0), _0110);
            data.Add(new Vector4(0, 0, 1, 1), _0011);
            data.Add(new Vector4(1, 0, 0, 1), _1001);
            data.Add(new Vector4(1, 1, 1, 0), _1110);
            data.Add(new Vector4(0, 1, 1, 1), _0111);
            data.Add(new Vector4(1, 0, 1, 1), _1011);
            data.Add(new Vector4(1, 1, 0, 1), _1101);
            data.Add(new Vector4(1, 0, 1, 0), _1010);
            data.Add(new Vector4(0, 1, 0, 1), _0101);
            data.Add(new Vector4(0.5f, 1, 0.5f, 0), _5150);
            data.Add(new Vector4(0.5f, 0, 0.5f, 1), _5051);
            data.Add(new Vector4(0, 0.5f, 1, 0.5f), _0515);
            data.Add(new Vector4(1, 0.5f, 0, 0.5f), _1505);
        }
    }
}
