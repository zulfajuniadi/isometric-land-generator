using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace TilemapGenerator
{
    [ExecuteInEditMode]
    [RequireComponent(typeof(Camera))]
    public class FixSortMode : MonoBehaviour
    {
        Camera thisCamera;

        void OnEnable()
        {
            thisCamera = GetComponent<Camera>();
            thisCamera.transparencySortMode = TransparencySortMode.CustomAxis;
            thisCamera.transparencySortAxis = Vector3.up;
#if UNITY_EDITOR
            GameObject sceneCameraGo = GameObject.Find("SceneCamera");
            if (sceneCameraGo)
            {
                Camera sceneCamera = sceneCameraGo.GetComponent<Camera>();
                sceneCamera.transparencySortMode = TransparencySortMode.CustomAxis;
                sceneCamera.transparencySortAxis = Vector3.up;
            }
#endif
        }
    }
}
