using UnityEngine;

public class ParticleManager : MonoBehaviour
{
    [Header("Particle Properties")]
    [Range(0, 100000)]
    public int numParticles = 64000;
    [Range(0, 1)]
    public float particleSize = 0.1f;
    [Range(0, 10)]
    public float mostEffectiveDistance = 3;
    [Range(0, 1f)]
    public float stability = 0.1f;
    [Range(0, 1f)]
    public float steepness = 0.1f;
    [Range(0, 0.5f)]
    public float frictionStrength = 0.1f;
    public float mass = 1f;
    public bool useGradient;
    public Color particleColor = Color.white;
    public Gradient particleColorGradient;

    public ComputeShader shader;
    public GameObject particlePrefab;

    Vector3 _spawnStart = new(-0.9f, 0f, -0.9f);
    Vector3 _spawnSpan = new(1.8f, 1f, 1.8f);
    Vector3 _sizeVector;
    GameObject[] _particles;
    ParticleData[] _particleDatas;
    int _oneSide;

    struct ParticleData
    {
        public Vector3 position;
        public Vector3 velocity;
    }

    // Start is called before the first frame update
    void Start()
    {
        _oneSide = (int)Mathf.Pow(numParticles, 1f / 3f);
        _particles = new GameObject[numParticles];
        _particleDatas = new ParticleData[numParticles];
        _sizeVector = new(particleSize, particleSize, particleSize);
        particlePrefab.transform.localScale = _sizeVector;
        SpawnParticles();
    }

    // Update is called once per frame
    void Update()
    {
        ComputeBuffer buffer = new(numParticles, sizeof(float) * 6);
        buffer.SetData(_particleDatas);

        shader.SetBuffer(0, Shader.PropertyToID("Particles"), buffer);
        shader.SetFloat(Shader.PropertyToID("deltaTime"), Time.deltaTime);
        shader.SetFloats(Shader.PropertyToID("xRange"), -1f, 1f);
        shader.SetFloats(Shader.PropertyToID("yRange"), 0f, 1f);
        shader.SetFloats(Shader.PropertyToID("zRange"), -1f, 1f);
        shader.SetFloat(Shader.PropertyToID("size"), particleSize);
        shader.SetFloat(Shader.PropertyToID("frictionStrength"), frictionStrength);
        shader.SetFloat(Shader.PropertyToID("mass"), mass);
        shader.SetFloat(Shader.PropertyToID("mostEffectiveDistance"), particleSize * mostEffectiveDistance);
        shader.SetFloat(Shader.PropertyToID("stability"), stability);
        shader.SetFloat(Shader.PropertyToID("steepness"), steepness);

        int groups = Mathf.CeilToInt(numParticles / 64f);
        shader.Dispatch(0, groups, 1, 1);

        buffer.GetData(_particleDatas);
        buffer.Dispose();

        UpdateParticlePositions();
    }

    void UpdateParticlePositions()
    {
        for (int i = 0; i < numParticles; i++)
        {
            _particles[i].transform.position = _particleDatas[i].position;
        }
    }

    void SpawnParticles()
    {
        Vector3 distBetween = _spawnSpan / _oneSide;
        Vector3 xDist = new(distBetween.x, 0, 0);
        Vector3 yDist = new(0, distBetween.y, 0);
        Vector3 zDist = new(0, 0, distBetween.z);

        for (int i = 0; i < _oneSide; i++)
        {
            Vector3 pos = _spawnStart + xDist * i;
            for (int j = 0; j < _oneSide; j++)
            {
                Vector3 newPos = pos + yDist * j;
                for (int k = 0; k < _oneSide; k++)
                {
                    newPos += zDist;
                    Vector3 offset = new(Random.value * _sizeVector.x, Random.value * _sizeVector.y, Random.value * _sizeVector.z);
                    int idx = i * _oneSide * _oneSide + j * _oneSide + k;
                    GameObject particle = Instantiate(particlePrefab, newPos + offset, Quaternion.identity);
                    
                    // Set Color
                    Material material = particle.GetComponent<MeshRenderer>().material;
                    if (useGradient) material.color = particleColorGradient.Evaluate((float)idx / numParticles);
                    else material.color = particleColor;

                    _particles[idx] = particle;
                    _particleDatas[idx] = new() { position = newPos, velocity = Vector3.zero };
                }
            }
        }
    }
}
