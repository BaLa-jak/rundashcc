using UnityEngine;

namespace RunDashC
{
    /// <summary>Asigna Camera.main a un canvas Screen Space - Camera que no tenga cámara (p. ej. Help aditiva).</summary>
    [RequireComponent(typeof(Canvas))]
    public class CanvasCameraBinder : MonoBehaviour
    {
        void Start()
        {
            var canvas = GetComponent<Canvas>();
            if (canvas.renderMode == RenderMode.ScreenSpaceCamera && canvas.worldCamera == null)
                canvas.worldCamera = Camera.main; // si sigue en null, Unity lo dibuja como overlay
        }
    }
}
