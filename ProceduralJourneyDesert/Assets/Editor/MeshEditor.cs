using UnityEditor;
using UnityEngine;

[CustomEditor (typeof (MeshGenerator))]
public class MeshEditor : Editor {

    private MeshGenerator m_Mesh;

    public override void OnInspectorGUI () {
        DrawDefaultInspector ();

        if (GUILayout.Button ("maak die Mesh bro")) {
            m_Mesh.StartMeshGeneration ();
        }

        if (GUILayout.Button (" Aelion Erode (" + m_Mesh.NumErosionIterations + " iterations)")) {
            var sw = new System.Diagnostics.Stopwatch ();
            sw.Start ();
            m_Mesh.Erode ();
            sw.Stop ();
            Debug.Log ($"Erosion finished ({m_Mesh.NumErosionIterations} iterations; {sw.ElapsedMilliseconds}ms)");
        }
    }

    void OnSceneGUI () {
        if (m_Mesh.ShowNumIterations) {
            Handles.BeginGUI ();
            GUIStyle s = new GUIStyle (EditorStyles.boldLabel);
            s.fontSize = 40;

            string label = "Erosion iterations: " + m_Mesh.NumAnimatedErosionIterations;
            Vector2 labelSize = s.CalcSize (new GUIContent (label));

            Rect p = SceneView.currentDrawingSceneView.position;
            GUI.Label (new Rect (p.width / 2 - labelSize.x / 2, p.height - labelSize.y * 2.5f, labelSize.x, labelSize.y), label, s);
            Handles.EndGUI ();
        }
    }
    void OnEnable () {
        m_Mesh = (MeshGenerator) target;
        Tools.hidden = true;
    }

    void OnDisable () {
        Tools.hidden = false;
    }
}