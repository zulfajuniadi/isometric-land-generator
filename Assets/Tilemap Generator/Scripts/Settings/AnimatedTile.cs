using System;
using System.Collections;
using System.Collections.Generic;

#if UNITY_EDITOR
using UnityEditor;
#endif

using UnityEngine;
using UnityEngine.Tilemaps;
using Random = UnityEngine.Random;

namespace TilemapGenerator.Settings
{
    [Serializable]
    [CreateAssetMenu(fileName = "New Animated Tile", menuName = "Tilemap Generator/Animated Tile")]
    public class AnimatedTile : TileBase
    {
        public Sprite DefaultSprite;
        public Sprite[] m_AnimatedSprites = new Sprite[0];
        public float Speed = 1f;
        public Tile.ColliderType m_TileColliderType;
        public Color tileColor = Color.white;
        public Matrix4x4 tileTransform = Matrix4x4.identity;

        public override void GetTileData(Vector3Int location, ITilemap tileMap, ref TileData tileData)
        {
            tileData.transform = tileTransform;
            tileData.color = tileColor;
            tileData.colliderType = m_TileColliderType;
            tileData.sprite = DefaultSprite;
        }

        public override bool GetTileAnimationData(Vector3Int location, ITilemap tileMap, ref TileAnimationData tileAnimationData)
        {
            if (m_AnimatedSprites.Length > 0)
            {
                tileAnimationData.animatedSprites = m_AnimatedSprites;
                tileAnimationData.animationSpeed = Speed;
                tileAnimationData.animationStartTime = location.y + location.x;
                return true;
            }
            return false;
        }
    }
}
