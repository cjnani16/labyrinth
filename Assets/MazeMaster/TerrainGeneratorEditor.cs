using System.Collections;
using UnityEngine;
using UnityEditor;

[CustomEditor (typeof (TerrainGenerator))]
public class TerrainGeneratorEditor : Editor
{
    public override void OnInspectorGUI() {
        TerrainGenerator terrGen = (TerrainGenerator)target;

        if (DrawDefaultInspector()) {
            if (terrGen.autoupdate) {
                terrGen.RerollTerrain();
            }

        }

        if (GUILayout.Button("Generate Terrain")) {
            terrGen.RerollTerrain();
        }

    }
}
