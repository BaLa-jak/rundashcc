using UnityEngine;

namespace RunDashC
{
    /// <summary>
    /// Asigna Camera.main a un canvas Screen Space - Camera que no tenga cámara (p. ej. Help aditiva).
    /// Con el mundo al revés la cámara está volteada: entonces el canvas va como overlay para verse derecho.
    /// </summary>
    [RequireComponent(typeof(Canvas))]
    public class CanvasCameraBinder : MonoBehaviour
    {
        void Start()
        {
            var canvas = GetComponent<Canvas>();
            if (canvas.renderMode != RenderMode.ScreenSpaceCamera || canvas.worldCamera != null) return;
            if (WorldMode.Flipped) canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            else canvas.worldCamera = Camera.main; // si sigue en null, Unity lo dibuja como overlay
        }
    }
}
