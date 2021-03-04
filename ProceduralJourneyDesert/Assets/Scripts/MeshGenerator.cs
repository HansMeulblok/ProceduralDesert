using UnityEngine;

public class MeshGenerator : MonoBehaviour {

    [Header ("Mesh Settings")]
    [Range (2, 255)]
    [SerializeField] private int m_MapSize = 255;
    [SerializeField] private float m_Scale = 20;
    [SerializeField] private float m_ElevationScale = 10;
    [SerializeField] private Material m_Material;

    [Header ("Erosion Settings")]
    public int NumErosionIterations = 50000;

    [Header ("Animation Settings")]
    [SerializeField] private bool m_AnimateErosion;
    [SerializeField] private int m_IterationsPerFrame = 100;
    public bool ShowNumIterations;
    public int NumAnimatedErosionIterations { get; private set; }

    private float[] m_Map;
    private Mesh m_Mesh;
    private Erosion m_Erosion;
    private MeshRenderer m_MeshRenderer;
    private MeshFilter m_MeshFilter;

    void Start () {
        StartMeshGeneration ();
        // erosion = FindObjectOfType<Erosion> ();
        Application.runInBackground = true;
    }

    public void StartMeshGeneration () {
        m_Map = FindObjectOfType<HeightMapGenerator>().Generate (m_MapSize);
        GenerateMesh ();
    }

    public void Erode () {
        m_Map = FindObjectOfType<HeightMapGenerator> ().Generate (m_MapSize);
        m_Erosion = FindObjectOfType<Erosion>();
        m_Erosion.Erode (m_Map, m_MapSize, NumErosionIterations, true);
        GenerateMesh ();
    }

    void Update () {
        if (m_AnimateErosion) {
            for (int i = 0; i < m_IterationsPerFrame; i++) {
                m_Erosion.Erode (m_Map, m_MapSize);
            }
            NumAnimatedErosionIterations += m_IterationsPerFrame;
            GenerateMesh ();
        }
    }

    void GenerateMesh () {
        Vector3[] verts = new Vector3[m_MapSize * m_MapSize];
        int[] triangles = new int[(m_MapSize - 1) * (m_MapSize - 1) * 6];
        int t = 0;

        for (int y = 0; y < m_MapSize; y++) {
            for (int x = 0; x < m_MapSize; x++) {
                int i = y * m_MapSize + x;

                Vector2 percent = new Vector2 (x / (m_MapSize - 1f), y / (m_MapSize - 1f));
                Vector3 pos = new Vector3 (percent.x * 2 - 1, 0, percent.y * 2 - 1) * m_Scale;
                pos += Vector3.up * m_Map[i] * m_ElevationScale;
                verts[i] = pos;

                // Construct triangles
                if (x != m_MapSize - 1 && y != m_MapSize - 1) {

                    triangles[t + 0] = i + m_MapSize;
                    triangles[t + 1] = i + m_MapSize + 1;
                    triangles[t + 2] = i;

                    triangles[t + 3] = i + m_MapSize + 1;
                    triangles[t + 4] = i + 1;
                    triangles[t + 5] = i;
                    t += 6;
                }
            }
        }

        if (m_Mesh == null) {
            m_Mesh = new Mesh ();
        } else {
            m_Mesh.Clear ();
        }

        m_Mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
        m_Mesh.vertices = verts;
        m_Mesh.triangles = triangles;
        m_Mesh.RecalculateNormals ();

        AssignMeshComponents ();
        m_MeshFilter.sharedMesh = m_Mesh;
        m_MeshRenderer.sharedMaterial = m_Material;
        m_Material.SetFloat ("_MaxHeight", m_ElevationScale);
    }

    void AssignMeshComponents () {
        // Find/creator mesh holder object in children
        string meshHolderName = "Mesh Holder";
        Transform meshHolder = transform.Find (meshHolderName);
        if (meshHolder == null) {
            meshHolder = new GameObject (meshHolderName).transform;
            meshHolder.transform.parent = transform;
            meshHolder.transform.localPosition = Vector3.zero;
            meshHolder.transform.localRotation = Quaternion.identity;
        }

        // Ensure mesh renderer and filter components are assigned
        if (!meshHolder.gameObject.GetComponent<MeshFilter> ()) {
            meshHolder.gameObject.AddComponent<MeshFilter> ();
        }
        if (!meshHolder.GetComponent<MeshRenderer> ()) {
            meshHolder.gameObject.AddComponent<MeshRenderer> ();
        }

        m_MeshRenderer = meshHolder.GetComponent<MeshRenderer> ();
        m_MeshFilter = meshHolder.GetComponent<MeshFilter> ();
    }
}