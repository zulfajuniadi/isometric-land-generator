using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace TilemapGenerator
{
    [CreateAssetMenu(fileName = "Instanced Spawner Configuration", menuName = "Tilemap Generator/Instanced Spawner Config", order = 1)]
    public class InstancedSpawnerConfiguration : ScriptableObject
    {
        public Sprite[] Sprites;
        public Texture3D PackedTexture;
        public float MeshSize = 1f;
        public bool OnEdgeOnly = false;
        public Material Material;
    }
}
