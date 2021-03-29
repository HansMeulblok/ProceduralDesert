using UnityEngine;

public class Erosion : MonoBehaviour {

    [SerializeField] private int m_Seed;
    [Range (2, 8)]
    [SerializeField] private int m_ErosionRadius = 3; // Radius for how much a particle can affect
    [Range (0, 1)]
    [SerializeField] private float m_Inertia = 0.05f; // At zero, particles will instantly change direction to flow downhill. At 1, particles will never change direction. 
    [SerializeField] private float m_SedimentCapacityFactor = 4; // Multiplier for how much sediment a droplet can carry
    [SerializeField] private float m_MinSedimentCapacity = 0.01f; // Used to prevent carry capacity getting too close to zero on flatter terrain
    [Range (0, 1)]
    [SerializeField] private float m_ErodeSpeed = 0.3f; // How much sediment for time spend on a position a particle can take
    [Range (0, 1)]
    [SerializeField] private float m_DepositSpeed = 0.3f; // Amount deposited per update tic if sediment should be dropped
    [Range (0, 1)]
    [SerializeField] private float m_DisappearSpeed = 0.01f; // Life span
    [SerializeField] private float m_Gravity = 4;
    [SerializeField] private int m_MaxParticleLifetime = 30; // Max life span

    [SerializeField] private float m_InitialVolume = 1;
    [SerializeField] private float m_InitialSpeed = 1;
    [Range(0, 1)][SerializeField] private float m_Direction = 0.5f;

    // Indices and weights of erosion brush precomputed for every node
    int[][] erosionBrushIndices;
    float[][] erosionBrushWeights;
    System.Random prng;

    int currentSeed;
    int currentErosionRadius;
    int currentMapSize;

    // Initialization creates a System.Random object and precomputes indices and weights of erosion brush
    void Initialize (int mapSize, bool resetSeed) {
        if (resetSeed || prng == null || currentSeed != m_Seed) {
            prng = new System.Random (m_Seed);
            currentSeed = m_Seed;
        }

        if (erosionBrushIndices == null || currentErosionRadius != m_ErosionRadius || currentMapSize != mapSize) {
            InitializeBrushIndices (mapSize, m_ErosionRadius);
            currentErosionRadius = m_ErosionRadius;
            currentMapSize = mapSize;
        }
    }

    public void Erode (float[] map, int mapSize, int numIterations = 1, bool resetSeed = false) {
        Initialize (mapSize, resetSeed);

        for (int iteration = 0; iteration < numIterations; iteration++) {
            // Create particle at random point on map
            float posX = prng.Next (0, mapSize - 1);
            float posY = prng.Next (0, mapSize - 1);
            float dirX = 0;
            float dirY = 0;
            float speed = m_InitialSpeed;
            float volume = m_InitialVolume;
            float sediment = 0;

            for (int lifetime = 0; lifetime < m_MaxParticleLifetime; lifetime++) {
                int nodeX = (int) posX;
                int nodeY = (int) posY;
                int particleIndex = nodeY * mapSize + nodeX;
                // Calculate particles offset inside the cell (0,0) = at NW node, (1,1) = at SE node
                float cellOffsetX = posX - nodeX;
                float cellOffsetY = posY - nodeY;

                // Calculate particles height and direction of flow with bilinear interpolation of surrounding heights
                HeightAndGradient heightAndGradient = CalculateHeightAndGradient (map, mapSize, posX, posY);

                // direction
                dirX = m_Direction * m_Inertia - heightAndGradient.gradientX;
                dirY = m_Direction * m_Inertia - heightAndGradient.gradientY;

                // Normalize direction
                float len = Mathf.Sqrt (dirX * dirX + dirY * dirY);
                if (len != 0) {
                    dirX /= len;
                    dirY /= len;
                }

                //move
                posX += dirX;
                posY += dirY;

                // Stop simulating particle if it's not moving or has flowed over edge of map
                // limit if going to a high pos
                if ((dirX == 0 && dirY == 0) || posX < 0 || posX >= mapSize - 1 || posY < 0 || posY >= mapSize - 1) {
                    break;
                }

                // Find the particle's new height and calculate the deltaHeight
                float newHeight = CalculateHeightAndGradient (map, mapSize, posX, posY).height;
                float deltaHeight = newHeight - heightAndGradient.height;

                // Calculate the particle's sediment capacity (higher when moving fast down a slope)
                float sedimentCapacity = Mathf.Max (-deltaHeight * speed * volume * m_SedimentCapacityFactor, m_MinSedimentCapacity);

                // If carrying more sediment than capacity, or if going too far uphill:
                if (sediment > sedimentCapacity || deltaHeight > 2) {
                    // If moving too far uphill (deltaHeight > 2) try fill up to the current height, otherwise deposit a fraction of the excess sediment
                    float amountToDeposit = (deltaHeight > 2) ? Mathf.Min (deltaHeight, sediment) : (sediment - sedimentCapacity) * m_DepositSpeed;
                    sediment -= amountToDeposit;

                    // Add the sediment to the four nodes of the current cell using bilinear interpolation
                    // Deposition is not distributed over a radius (like erosion) so that it can fill small pits
                    map[particleIndex] += amountToDeposit * (1 - cellOffsetX) * (1 - cellOffsetY);
                    map[particleIndex + 1] += amountToDeposit * cellOffsetX * (1 - cellOffsetY);
                    map[particleIndex + mapSize] += amountToDeposit * (1 - cellOffsetX) * cellOffsetY;
                    map[particleIndex + mapSize + 1] += amountToDeposit * cellOffsetX * cellOffsetY;

                } else {
                    // Erode a fraction of the particle's current carry capacity.
                    // Clamp the erosion to the change in height so that it doesn't dig a hole in the terrain behind the particle
                    float amountToErode = Mathf.Min ((sedimentCapacity - sediment) * m_ErodeSpeed, -deltaHeight);

                    // Use erosion brush to erode from all nodes inside the particle's erosion radius
                    for (int brushPointIndex = 0; brushPointIndex < erosionBrushIndices[particleIndex].Length; brushPointIndex++) {
                        int nodeIndex = erosionBrushIndices[particleIndex][brushPointIndex];
                        float weighedErodeAmount = amountToErode * erosionBrushWeights[particleIndex][brushPointIndex];
                        float deltaSediment = (map[nodeIndex] < weighedErodeAmount) ? map[nodeIndex] : weighedErodeAmount;
                        map[nodeIndex] -= deltaSediment;
                        sediment += deltaSediment;
                    }
                }

                // Update particle's speed and  volume
                speed = Mathf.Sqrt (speed * speed + deltaHeight * m_Gravity);
                volume *= (1 - m_DisappearSpeed);
            }
        }
    }

    HeightAndGradient CalculateHeightAndGradient (float[] nodes, int mapSize, float posX, float posY) {
        int coordX = (int) posX;
        int coordY = (int) posY;

        // Calculate particle's offset inside the cell (0,0) = at NW node, (1,1) = at SE node
        float x = posX - coordX;
        float y = posY - coordY;

        // Calculate heights of the four nodes of the particle's cell
        int nodeIndexNW = coordY * mapSize + coordX;
        float heightNW = nodes[nodeIndexNW];
        float heightNE = nodes[nodeIndexNW + 1];
        float heightSW = nodes[nodeIndexNW + mapSize];
        float heightSE = nodes[nodeIndexNW + mapSize + 1];

        // Calculate particle's direction of flow with bilinear interpolation of height difference along the edges
        float gradientX = (heightNE - heightNW) * (1 - y) + (heightSE - heightSW) * y;
        float gradientY = (heightSW - heightNW) * (1 - x) + (heightSE - heightNE) * x;

        // Calculate height with bilinear interpolation of the heights of the nodes of the cell
        float height = heightNW * (1 - x) * (1 - y) + heightNE * x * (1 - y) + heightSW * (1 - x) * y + heightSE * x * y;

        return new HeightAndGradient () { height = height, gradientX = gradientX, gradientY = gradientY };
    }

    void InitializeBrushIndices (int mapSize, int radius) {
        erosionBrushIndices = new int[mapSize * mapSize][];
        erosionBrushWeights = new float[mapSize * mapSize][];

        int[] xOffsets = new int[radius * radius * 4];
        int[] yOffsets = new int[radius * radius * 4];
        float[] weights = new float[radius * radius * 4];
        float weightSum = 0;
        int addIndex = 0;

        for (int i = 0; i < erosionBrushIndices.GetLength (0); i++) {
            int centreX = i % mapSize;
            int centreY = i / mapSize;

            if (centreY <= radius || centreY >= mapSize - radius || centreX <= radius + 1 || centreX >= mapSize - radius) {
                weightSum = 0;
                addIndex = 0;
                for (int y = -radius; y <= radius; y++) {
                    for (int x = -radius; x <= radius; x++) {
                        float sqrDst = x * x + y * y;
                        if (sqrDst < radius * radius) {
                            int coordX = centreX + x;
                            int coordY = centreY + y;

                            if (coordX >= 0 && coordX < mapSize && coordY >= 0 && coordY < mapSize) {
                                float weight = 1 - Mathf.Sqrt (sqrDst) / radius;
                                weightSum += weight;
                                weights[addIndex] = weight;
                                xOffsets[addIndex] = x;
                                yOffsets[addIndex] = y;
                                addIndex++;
                            }
                        }
                    }
                }
            }

            int numEntries = addIndex;
            erosionBrushIndices[i] = new int[numEntries];
            erosionBrushWeights[i] = new float[numEntries];

            for (int j = 0; j < numEntries; j++) {
                erosionBrushIndices[i][j] = (yOffsets[j] + centreY) * mapSize + xOffsets[j] + centreX;
                erosionBrushWeights[i][j] = weights[j] / weightSum;
            }
        }
    }

    struct HeightAndGradient {
        public float height;
        public float gradientX;
        public float gradientY;
    }
}