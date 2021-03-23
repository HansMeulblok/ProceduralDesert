using UnityEditor;
using UnityEngine;

[CustomEditor (typeof (MeshGenerator))]
public class MeshEditor : Editor {

    private MeshGenerator m_Mesh;

    public override void OnInspectorGUI () {
        // Here so I don't need to create a new inspector
        DrawDefaultInspector();


        // Button to generate the mesh
        if (GUILayout.Button ("Generate Mesh")) {
            m_Mesh.StartMeshGeneration ();
        }

        // Seperate button to erode the mesh, including a timer
        if (GUILayout.Button ("Aelion Erode (" + m_Mesh.NumErosionIterations + " iterations)")) {
            var sw = new System.Diagnostics.Stopwatch ();
            sw.Start ();
            m_Mesh.Erode ();
            sw.Stop ();
            Debug.Log ($"Erosion finished ({m_Mesh.NumErosionIterations} iterations; {sw.ElapsedMilliseconds}ms)");
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