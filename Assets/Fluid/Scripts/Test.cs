using UnityEngine;

public class Test : MonoBehaviour
{
    public GameObject prefab;

    public int numParticles = 3;
    public float maxEffective = 0.1f;
    public float D = 1;
    public float a = 1;

    Transform[] _particles;
    Vector3[] _velocities;

    void Start()
    {
        _particles = new Transform[numParticles];
        _velocities = new Vector3[numParticles];
        for (int i = 0; i < numParticles; i++)
        {
            Vector3 pos = new(Random.value, Random.value, Random.value);
            _particles[i] = Instantiate(prefab, pos, Quaternion.identity).transform;
            _velocities[i] = Vector3.zero;
        }
    }

    // Update is called once per frame
    void Update()
    {
        for (int i = 0; i < numParticles; i++)
        {
            Transform particle = _particles[i];
            Vector3 accel = Vector3.zero;
            for (int j = 0; j < numParticles; j++)
            {
                Transform part2 = _particles[j];
                if (particle.position == part2.position) continue;

                Vector3 diff = part2.transform.position - particle.transform.position;
                float distance = diff.magnitude;
                float r = distance - maxEffective;

                float strength = 2 * a * D * Mathf.Exp(-a * r) * (1 - Mathf.Exp(-a * r));
                Vector3 dir = diff.normalized;

                //particle.position += strength * dir;

                accel += strength * dir;
            }

            _velocities[i] += accel * Time.deltaTime;
            particle.position += _velocities[i] * Time.deltaTime;
        }
    }
}
