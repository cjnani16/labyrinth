using System.Collections;
using UnityEngine;
using UnityEditor;

[CustomEditor (typeof (MazeGenerator))]
public class MazeGeneratorEditor : Editor
{
    public override void OnInspectorGUI() {
        MazeGenerator mazeGen = (MazeGenerator)target;

        if (DrawDefaultInspector()) {
            if (mazeGen.autoupdate) {
                mazeGen.GenerateMaze();
            }

        }

        if (GUILayout.Button("Generate Maze")) {
            mazeGen.GenerateMaze();
        }

    }
}
