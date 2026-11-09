using UnityEngine;

namespace RunDashC
{
    /// <summary>
    /// Va en la Main Camera: y fija en 6 (se ven exactamente las filas 0..11) y x sigue a Camarada con
    /// offset +6 y un suavizado leve. Shake con tiempo no escalado.
    /// </summary>
    [RequireComponent(typeof(Camera))]
    public class CameraRig : MonoBehaviour
    {
        public Transform target;
        public float offsetX = 6f;
        public float fixedY = 6f;
        public float smoothing = 10f;
        public bool follow = true;

        Camera cam;
        float x, shakeAmplitude, shakeDuration, shakeLeft;

        void Awake()
        {
            cam = GetComponent<Camera>();
            x = transform.position.x;
        }

        void Start()
        {
            if (target != null) x = target.position.x + offsetX;
        }

        void LateUpdate()
        {
            if (follow && target != null)
                x = Mathf.Lerp(x, target.position.x + offsetX, 1f - Mathf.Exp(-smoothing * Time.deltaTime));
            x = Mathf.Max(x, cam.orthographicSize * cam.aspect); // no mostrar a la izquierda del mapa

            var shake = Vector2.zero;
            if (shakeLeft > 0f)
            {
                shakeLeft -= Time.unscaledDeltaTime;
                shake = Random.insideUnitCircle * (shakeAmplitude * Mathf.Clamp01(shakeLeft / shakeDuration));
            }
            transform.position = new Vector3(x + shake.x, fixedY + shake.y, transform.position.z);
        }

        public void Shake(float amplitude, float duration)
        {
            shakeAmplitude = amplitude;
            shakeDuration = Mathf.Max(0.01f, duration);
            shakeLeft = shakeDuration;
        }
    }
}
