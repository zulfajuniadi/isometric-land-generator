using System.IO;
using UnityEngine;
using UnityEditor;
using TilemapGenerator;

namespace TilemapGeneratorEditor
{
    [CustomEditor(typeof(InstancedRenderer))]
    public class InstancedRendererEditor : Editor
    {

        InstancedRenderer instance;

        private void OnEnable()
        {
            instance = (InstancedRenderer) target;
        }

        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();
            if (GUILayout.Button("Generate Packed Image"))
            {
                if (instance.SourceImages.Length == 0)
                {
                    Debug.LogError("Add some images into the Source Images");
                    return;
                }
                string basePath = AssetDatabase.GetAssetPath(instance.SourceImages[0]);
                if (string.IsNullOrEmpty(basePath) || string.IsNullOrWhiteSpace(basePath))
                {
                    Debug.LogError("Can't get base path");
                    return;
                }
                int index = basePath.LastIndexOf('/');
                basePath = basePath.Substring(0, index);
                string path = EditorUtility.SaveFilePanel("Save To", basePath, instance.SourceImages[0].name, "asset");
                if (path.Contains(Application.dataPath))
                {
                    path = path.Substring(Application.dataPath.Length - 6);
                    Texture3D results = BuildTexture(instance.SourceImages);
                    if (results == null)
                    {
                        Debug.LogError("Error generating texture");
                        return;
                    }
                    AssetDatabase.CreateAsset(results, path);
                    AssetDatabase.Refresh();
                    results = AssetDatabase.LoadAssetAtPath<Texture3D>(path);
                    instance.PackedImage = results;
                    EditorUtility.SetDirty(target);
                }
            }
        }

        public Texture3D BuildTexture(Texture2D[] sourceImages)
        {
            int width = 0;
            int height = 0;
            int depth = sourceImages.Length;

            for (int i = 0; i < depth; i++)
            {
                if (sourceImages[i].width > width)
                {
                    width = sourceImages[i].width;
                }
                if (sourceImages[i].height > height)
                {
                    height = sourceImages[i].height;
                }
            }
            width = Mathf.NextPowerOfTwo(width);
            height = Mathf.NextPowerOfTwo(height);
            int texSize = width * height;
            Texture3D sourceImage = new Texture3D(width, height, depth, TextureFormat.ARGB32, false);
            Color[] textureColors = new Color[texSize * depth];
            Color[] transparent = new Color[texSize];
            Texture2D blank = new Texture2D(width, height, TextureFormat.ARGB32, false);
            for (int i = 0; i < texSize; i++)
            {
                transparent[i] = new Color(0, 0, 0, 0);
            }
            for (int i = 0; i < depth; i++)
            {
                int offset = i * texSize;
                blank.SetPixels(transparent);
                blank.Apply();
                Graphics.CopyTexture(sourceImages[i], 0, 0, 0, 0, sourceImages[i].width, sourceImages[i].height, blank, 0, 0, 0, 0);
                blank.Apply();
                System.Array.Copy(blank.GetPixels(), 0, textureColors, offset, texSize);
            }
            sourceImage.SetPixels(textureColors);
            sourceImage.Apply();
            return sourceImage;
        }
    }
}
