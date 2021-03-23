using UnityEngine;

public class HeightMapGenerator : MonoBehaviour {
    [SerializeField] private int m_Seed;
    [SerializeField] private bool m_RandomizeSeed;
    [SerializeField] private int m_NumOctaves = 7; //layers of noise
    [SerializeField] private float m_Persistence = 0.5f; // range from 0 to 1, decreases octaves
    [SerializeField] private float m_Lacunarity = 2; //control the rate by which the frequency changes
    [SerializeField] private float m_InitialScale = 2;

    public float[] Generate (int mapSize) {

        var map = new float[mapSize * mapSize];

        //set seed
        m_Seed = (m_RandomizeSeed) ? Random.Range (-10000, 10000) : m_Seed;
        var prng = new System.Random (m_Seed);


        //noise layers
        Vector2[] offsets = new Vector2[m_NumOctaves];
        for (int i = 0; i < m_NumOctaves; i++) {
            offsets[i] = new Vector2 (prng.Next (-1000, 1000), prng.Next (-1000, 1000));
        }

        float minValue = float.MaxValue;
        float maxValue = float.MinValue;

        // use different layers of noise for the height
        for (int y = 0; y < mapSize; y++) {
            for (int x = 0; x < mapSize; x++) {
                float noiseValue = 0;
                float scale = m_InitialScale;
                float weight = 1;
                for (int i = 0; i < m_NumOctaves; i++) {
                    Vector2 p = offsets[i] + new Vector2 (x / (float) mapSize, y / (float) mapSize) * scale;
                    noiseValue += Mathf.PerlinNoise (p.x, p.y) * weight;
                    weight *= m_Persistence;
                    scale *= m_Lacunarity;
                }
                map[y * mapSize + x] = noiseValue;
                minValue = Mathf.Min (noiseValue, minValue);
                maxValue = Mathf.Max (noiseValue, maxValue);
            }
        }

        // Normalize
        if (maxValue != minValue) {
            for (int i = 0; i < map.Length; i++) {
                map[i] = (map[i] - minValue) / (maxValue - minValue);
            }
        }

        return map;
    }
}