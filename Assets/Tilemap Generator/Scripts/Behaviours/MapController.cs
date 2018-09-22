using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace TilemapGenerator.Behaviours
{
    public class MapController : MonoBehaviour
    {
        public Camera MainCamera;
        public Vector2 MinMaxZoom = new Vector2(2, 15);

        private bool isDragging = false;
        private Vector3 lastPosition = Vector3.zero;

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
                lastPosition = Input.mousePosition;

                isDragging = true;
            }
            else if (Input.GetMouseButtonUp(1))
            {
                isDragging = false;
            }
            else if (isDragging)
            {
                Vector3 currentPosition = Input.mousePosition;
                transform.position += (lastPosition - currentPosition) * MainCamera.orthographicSize / 3f * Time.deltaTime;
                lastPosition = currentPosition;
            }
        }
    }
}
