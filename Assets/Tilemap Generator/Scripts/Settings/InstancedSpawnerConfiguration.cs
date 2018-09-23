using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace TilemapGenerator.Settings
{
    [CreateAssetMenu(fileName = "Instanced Spawner Configuration", menuName = "Tilemap Generator/Instanced Spawner Config", order = 1)]
    public class InstancedSpawnerConfiguration : ScriptableObject
    {
        [Header("General Settings")]
        public bool Enabled = true;
        public bool OnFlat = true;
        public bool OnSlopes = true;
        // @TODO
        // [Header("Prefab Spawner")]
        // public GameObject Prefab;
        [Header("Sprite Spawner")]
        public Sprite[] Sprites;
        public Texture3D PackedTexture;
        public float MeshSize = 1f;
    }
}
