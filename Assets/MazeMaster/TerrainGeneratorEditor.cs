using System.Collections;
using UnityEngine;
using UnityEditor;

[InitializeOnLoad]
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
                terrGen.StopCoroutine("GenerateMazeTerrainRealtime");
            }
        }

        if (GUILayout.Button("Generate Terrain")) {
            //terrGen.RerollTerrain();
            if (terrGen.RealTime) {
                terrGen.RerollTerrainRealtime();
            }
            else
            {
                terrGen.RerollTerrain();
            }
        }

        if (GUILayout.Button("Generate Chunky Terrain"))
        {
            terrGen.ShowChunkyView();
        }
    }
}
