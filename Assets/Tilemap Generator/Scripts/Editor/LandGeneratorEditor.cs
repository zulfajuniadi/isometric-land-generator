using UnityEngine;
using UnityEditor;
using TilemapGenerator.Behaviours;

namespace TilemapGeneratorEditor
{
    [CustomEditor(typeof(LandGenerator))]
    public class LandGeneratorEditor : Editor
    {
        private LandGenerator generator;

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
                generator.Seed = Random.Range(-999999f, 999999f);
                generator.Generate();
            }
            if (generator.AutoGenerate && EditorGUI.EndChangeCheck())
            {
                generator.Generate();
            }
        }

    }
}
