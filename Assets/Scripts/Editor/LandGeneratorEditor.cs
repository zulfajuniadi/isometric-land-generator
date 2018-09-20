using UnityEngine;
using UnityEditor;
using TilemapGenerator;

namespace TilemapGeneratorEditor
{

    [CustomEditor(typeof(LandGenerator))]
    public class LandGeneratorEditor : Editor
    {
        LandGenerator generator;

        void OnEnable()
        {
            generator = (LandGenerator) target;
        }

        public override void OnInspectorGUI()
        {
            EditorGUI.BeginChangeCheck();
            base.OnInspectorGUI();
            if (GUILayout.Button("Generate"))
            {
                generator.Generate();
            }
            if (GUILayout.Button("Generate Random"))
            {
                generator.Seed = Random.Range(-9999f, 9999f);
                generator.Generate();
            }
            if (EditorGUI.EndChangeCheck())
            {
                generator.Generate();
            }
        }
    }
}
