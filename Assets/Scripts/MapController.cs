using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace TilemapGenerator
{
    public class MapController : MonoBehaviour
    {
        public LandGenerator LandGenerator;
        public InstancedRenderer TreesRenderer;
        public Camera MainCamera;
        public Vector2 MinMaxZoom = new Vector2(2, 15);

        private bool isDragging = false;
        private Vector3 lastPosition = Vector3.zero;
        private Quaternion rotation = Quaternion.Euler(0, 0, -45);

        private void Update()
        {
            Vector2 scrollDelta = Input.mouseScrollDelta;
            if (scrollDelta != Vector2.zero)
            {
                MainCamera.orthographicSize += scrollDelta.y;
                if (MainCamera.orthographicSize > MinMaxZoom.y)
                {
                    MainCamera.orthographicSize = MinMaxZoom.y;
                }
                else if (MainCamera.orthographicSize < MinMaxZoom.x)
                {
                    MainCamera.orthographicSize = MinMaxZoom.x;
                }
            }
            if (Input.GetMouseButtonDown(1))
            {
                lastPosition = MainCamera.ScreenToWorldPoint(Input.mousePosition);

                isDragging = true;
            }
            else if (Input.GetMouseButtonUp(1))
            {
                isDragging = false;
            }
            else if (isDragging)
            {
                Vector3 currentPosition = MainCamera.ScreenToWorldPoint(Input.mousePosition);
                LandGenerator.Caddy.position -= lastPosition - currentPosition;
                lastPosition = currentPosition;
                TreesRenderer.Offset = LandGenerator.Caddy.position;
            }
        }
    }
}
