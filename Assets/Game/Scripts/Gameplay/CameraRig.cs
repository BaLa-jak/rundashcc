using UnityEngine;

namespace RunDashC
{
    /// <summary>
    /// Va en la Main Camera: y fija en 6 (se ven exactamente las filas 0..11) y x sigue a Camarada con
    /// offset +6 y un suavizado leve. Shake con tiempo no escalado. Zoom suave (ease-in-out) hacia un objetivo.
    /// </summary>
    [RequireComponent(typeof(Camera))]
    public class CameraRig : MonoBehaviour
    {
        public Transform target;
        public float offsetX = 6f;
        public float fixedY = 6f;
        public float smoothing = 10f;
        public bool follow = true;
        const float ZoomLift = 0.5f; // Camarada parada mide ~1 u: centra el zoom un poco arriba del cuerpo

        Camera cam;
        Transform zoomTarget;
        float x, shakeAmplitude, shakeDuration, shakeLeft;
        float baseSize, zoomSize, zoom, zoomGoal, zoomSpeed;

        void Awake()
        {
            cam = GetComponent<Camera>();
            x = transform.position.x;
            baseSize = zoomSize = cam.orthographicSize;
        }

        void Start()
        {
            if (target != null) x = target.position.x + offsetX;
        }

        void LateUpdate()
        {
            if (follow && target != null)
                x = Mathf.Lerp(x, target.position.x + offsetX, 1f - Mathf.Exp(-smoothing * Time.deltaTime));
            x = Mathf.Max(x, baseSize * cam.aspect); // no mostrar a la izquierda del mapa

            zoom = Mathf.MoveTowards(zoom, zoomGoal, zoomSpeed * Time.unscaledDeltaTime);
            float s = Mathf.SmoothStep(0f, 1f, zoom);
            cam.orthographicSize = Mathf.Lerp(baseSize, zoomSize, s);
            var p = new Vector2(x, fixedY);
            if (zoomTarget != null) p = Vector2.Lerp(p, (Vector2)zoomTarget.position + new Vector2(0f, ZoomLift), s);

            var shake = Vector2.zero;
            if (shakeLeft > 0f)
            {
                shakeLeft -= Time.unscaledDeltaTime;
                shake = Random.insideUnitCircle * (shakeAmplitude * Mathf.Clamp01(shakeLeft / shakeDuration));
            }
            transform.position = new Vector3(p.x + shake.x, p.y + shake.y, transform.position.z);
        }

        /// <summary>Acerca la cámara a <paramref name="target"/> hasta <paramref name="size"/> (ortográfico) en <paramref name="seconds"/>.</summary>
        public void ZoomTo(Transform target, float size, float seconds)
        {
            zoomTarget = target;
            zoomSize = size;
            zoomGoal = 1f;
            zoomSpeed = 1f / Mathf.Max(0.01f, seconds);
        }

        /// <summary>Regresa suave al encuadre normal.</summary>
        public void ZoomOut(float seconds)
        {
            zoomGoal = 0f;
            zoomSpeed = 1f / Mathf.Max(0.01f, seconds);
        }

        public void Shake(float amplitude, float duration)
        {
            shakeAmplitude = amplitude;
            shakeDuration = Mathf.Max(0.01f, duration);
            shakeLeft = shakeDuration;
        }
    }
}
