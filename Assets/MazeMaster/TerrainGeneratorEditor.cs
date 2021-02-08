using System.Collections;
using UnityEngine;
using UnityEditor;

[CustomEditor (typeof (TerrainGenerator))]
public class TerrainGeneratorEditor : Editor
{
    [SerializeField]
    public int StepsToDraw = 140;
    public override void OnInspectorGUI() {
        TerrainGenerator terrGen = (TerrainGenerator)target;

        if (DrawDefaultInspector()) {
            if (terrGen.autoupdate) {
                //terrGen.RerollTerrain();
                terrGen.StartCoroutine("Trace");
            }
        }

        if (GUILayout.Button("Generate Terrain")) {
            //terrGen.RerollTerrain();
            terrGen.StartCoroutine("Trace");
        }
    }
}
