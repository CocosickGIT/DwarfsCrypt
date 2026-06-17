using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace Presentation.Light
{
    [RequireComponent(typeof(Light2D))]
    public class LightSource : MonoBehaviour
    {
        [SerializeField] private float baseIntensity = 1.5f;
        [SerializeField] private float amplitude = 0.3f;   // how much it varies
        [SerializeField] private float speed = 8f;         // how fast it flickers
        [SerializeField] private float positionJitter = 0.02f;

        private Light2D _light2D;
        private Vector3 _origin;
        private float _seed;

        void Awake()
        {
            _light2D = GetComponent<Light2D>();
            _origin = transform.localPosition;
            _seed = Random.value * 100f;
        }

        void Update()
        {
            float n = Mathf.PerlinNoise(_seed, Time.time * speed); // smooth 0..1
            _light2D.intensity = baseIntensity + (n - 0.5f) * 2f * amplitude;

            if (positionJitter > 0f)
            {
                float jx = (Mathf.PerlinNoise(_seed + 10f, Time.time * speed) - 0.5f) * positionJitter;
                float jy = (Mathf.PerlinNoise(_seed + 20f, Time.time * speed) - 0.5f) * positionJitter;
                transform.localPosition = _origin + new Vector3(jx, jy, 0f);
            }
        }
    }
}