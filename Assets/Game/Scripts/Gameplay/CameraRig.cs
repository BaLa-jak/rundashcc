using System.Collections.Generic;
using UnityEngine;

namespace RunDashC
{
    /// <summary>
    /// Va en la Main Camera: y fija en 6 (se ven exactamente las filas 0..11) y x sigue a Camarada con
    /// offset +6 y un suavizado leve. Shake con tiempo no escalado. Zoom suave (ease-in-out) hacia un objetivo.
    /// Mundo al revés (<see cref="WorldMode.Flipped"/>): voltea la proyección en Y con una animación de "giro".
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
        const float FlipSeconds = 0.6f;

        Camera cam;
        Transform zoomTarget;
        float x, shakeAmplitude, shakeDuration, shakeLeft;
        float baseSize, zoomSize, zoom, zoomGoal, zoomSpeed;
        float flip; // 0 = derecho, 1 = al revés
        readonly List<Canvas> overlaid = new List<Canvas>();

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
            UpdateFlip();
        }

        void UpdateFlip()
        {
            float goal = WorldMode.Flipped ? 1f : 0f;
            if (flip == 0f && goal == 0f && overlaid.Count == 0) return; // camino normal: proyección de Unity intacta
            flip = Mathf.MoveTowards(flip, goal, Time.deltaTime / FlipSeconds); // escalado: se congela en pausa
            // La pantalla se "aplana" hasta una línea y se abre al revés; nunca exactamente 0 (matriz degenerada).
            float k = Mathf.Cos(Mathf.PI * Mathf.SmoothStep(0f, 1f, flip));
            if (Mathf.Abs(k) < 0.02f) k = flip < 0.5f ? 0.02f : -0.02f;
            cam.ResetProjectionMatrix();
            if (flip > 0f) cam.projectionMatrix = Matrix4x4.Scale(new Vector3(1f, k, 1f)) * cam.projectionMatrix;
            SetHudOverlay(flip > 0f);
        }

        /// <summary>
        /// Los canvas Screen Space - Camera se dibujan con esta cámara: mientras esté volteada pasan a overlay
        /// para que el HUD siga derecho. shortcut: en overlay el HUD pierde el bloom; si molesta, una cámara de UI aparte.
        /// </summary>
        void SetHudOverlay(bool on)
        {
            if (on == overlaid.Count > 0) return;
            if (on)
            {
                foreach (var c in FindObjectsByType<Canvas>(FindObjectsSortMode.None))
                    if (c.isRootCanvas && c.renderMode == RenderMode.ScreenSpaceCamera && c.worldCamera == cam)
                    {
                        c.renderMode = RenderMode.ScreenSpaceOverlay;
                        overlaid.Add(c);
                    }
                return;
            }
            foreach (var c in overlaid)
                if (c != null)
                {
                    c.renderMode = RenderMode.ScreenSpaceCamera;
                    c.worldCamera = cam;
                }
            overlaid.Clear();
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
