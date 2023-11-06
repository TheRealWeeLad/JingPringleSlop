using UnityEngine;

public class ParticleManagerNew : MonoBehaviour
{
    [Header("Particle Properties")]
    public int numParticles = 30;
    [Range(0, 0.1f)]
    public float particleSize = 0.1f;
    [Range(0, 1)]
    public float collisionDamping = 0.1f;
    [Range(0, 3)]
    public float smoothingRadius = 0.4f;
    public float mass = 1f;
    [Range(0, 100)]
    public float targetDensity = 2;
    [Range(0, 100)]
    public float pressureMultiplier = 5f;

    [Header("References")]
    public ComputeShader particleShader;
    public GameObject particlePrefab;
    ComputeBuffer particleBuffer;
    ComputeBuffer densityBuffer;

    GameObject[] _particles;
    ParticleData[] _particleDatas;
    float[] _particleDensities;
    int _oneSide;

    Vector3 _spawnStart = new(-0.9f, 0.05f, -0.9f);
    Vector3 _spawnSpan = new(1.8f, 0.9f, 1.8f);
    Vector3 _sizeVector;

    struct ParticleData
    {
        public Vector3 position;
        public Vector3 velocity;
        public Vector3 pressure;
    }

    void OnEnable()
    {
        _particles = new GameObject[numParticles];
        _particleDatas = new ParticleData[numParticles];
        _particleDensities = new float[numParticles];
        _oneSide = Mathf.CeilToInt(Mathf.Pow(numParticles, 1f / 3f));
        _sizeVector = new(particleSize, particleSize, particleSize);
        particlePrefab.transform.localScale = _sizeVector;

        particleBuffer = new(numParticles, sizeof(float) * 9);
        densityBuffer = new(numParticles, sizeof(float));

        // Set constant shader variables
        particleShader.SetFloats("xRange", -1f, 1f);
        particleShader.SetFloats("yRange", 0f, 1f);
        particleShader.SetFloats("zRange", -1f, 1f);

        SpawnParticles();
    }

    // Update is called once per frame
    void Update()
    {
        particleBuffer.SetData(_particleDatas);

        // Set Shader Variables
        particleShader.SetFloat("deltaTime", Time.deltaTime);
        particleShader.SetFloat("collisionDamping", collisionDamping);
        particleShader.SetFloat("smoothingRadius", smoothingRadius);
        particleShader.SetFloat("mass", mass);
        particleShader.SetFloat("targetDensity", targetDensity);
        particleShader.SetFloat("pressureMultiplier", pressureMultiplier);

        // Update Density Cache
        int groups = Mathf.CeilToInt(numParticles / 64f);
        UpdateDensities(groups);
        densityBuffer.SetData(_particleDensities);

        // Dispatch Main Calculations
        particleShader.SetBuffer(0, "Particles", particleBuffer);
        particleShader.SetBuffer(0, "Densities", densityBuffer);
        particleShader.Dispatch(0, groups, 1, 1);

        particleBuffer.GetData(_particleDatas);

        UpdateParticlePositions();
    }

    void UpdateDensities(int groups)
    {
        densityBuffer.SetData(_particleDensities);

        particleShader.SetBuffer(1, "Particles", particleBuffer);
        particleShader.SetBuffer(1, "Densities", densityBuffer);

        particleShader.Dispatch(1, groups, 1, 1);

        densityBuffer.GetData(_particleDensities);
    }

    void UpdateParticlePositions()
    {
        for (int i = 0; i < numParticles; i++)
        {
            _particles[i].transform.position = _particleDatas[i].position;
            LineRenderer lr = _particles[i].GetComponent<LineRenderer>();
            Vector3 pos = _particles[i].transform.position;
            lr.SetPositions(new Vector3[] { pos, pos + _particleDatas[i].pressure.normalized / 10 });
        }
    }

    void SpawnParticles()
    {
        Vector3 distBetween = _spawnSpan / _oneSide;
        Vector3 xDist = new(distBetween.x, 0, 0);
        Vector3 yDist = new(0, distBetween.y, 0);
        Vector3 zDist = new(0, 0, distBetween.z);

        for (int j = 0; j < _oneSide; j++) // Y-direction
        {
            Vector3 pos = _spawnStart + yDist * j;
            for (int i = 0; i < _oneSide; i++) // X-direction
            {
                Vector3 newPos = pos + xDist * i;
                for (int k = 0; k < _oneSide; k++) // Z-direction
                {
                    newPos += zDist;
                    Vector3 offset = 0.3f * new Vector3(Random.value * _sizeVector.x, Random.value * _sizeVector.y, Random.value * _sizeVector.z);
                    int idx = j * _oneSide * _oneSide + i * _oneSide + k;
                    if (idx >= numParticles) break;

                    _particles[idx] = Instantiate(particlePrefab, newPos + offset, Quaternion.identity);
                    _particleDatas[idx] = new() { position = newPos + offset, velocity = Vector3.zero, pressure = Vector3.zero };
                }
            }
        }
    }

    private void OnDisable()
    {
        for (int i = 0; i < _particles.Length; i++)
        {
            Destroy(_particles[i]);
        }

        particleBuffer.Dispose();
        densityBuffer.Dispose();
    }
}
