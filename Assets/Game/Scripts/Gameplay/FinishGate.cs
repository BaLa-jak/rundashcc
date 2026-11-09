using System.Text;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace RunDashC
{
    /// <summary>Meta 'F': marco vertical de texto con "return 0;" y cursor parpadeante. Trigger de toda la altura.</summary>
    public class FinishGate : MonoBehaviour
    {
        const float Width = 5f;
        const string Label = "return 0;";

        /// <summary>Altura (y del mundo) del texto "return 0;". La pone LevelBuilder.</summary>
        public float labelY = 4f;

        TextMesh label;
        Light2D glow;
        bool done;

        void Start()
        {
            var post = new StringBuilder();
            for (int i = 0; i < 18; i++) post.Append(i == 0 ? "|" : "\n|");
            foreach (var x in new[] { 0f, Width })
            {
                var tm = WorldKit.AddText(transform, post.ToString(), new Vector2(x, 0f), AsciiTheme.Green, 1f, TextAnchor.LowerCenter, 3);
                tm.lineSpacing = 0.72f;
            }
            WorldKit.AddText(transform, "{", new Vector2(Width / 2f, labelY + 1.4f), AsciiTheme.GreenDim, 1f, TextAnchor.MiddleCenter, 3);
            WorldKit.AddText(transform, "}", new Vector2(Width / 2f, labelY - 1.4f), AsciiTheme.GreenDim, 1f, TextAnchor.MiddleCenter, 3);
            label = WorldKit.AddText(transform, Label + "_", new Vector2(Width / 2f, labelY), AsciiTheme.Green, 1.3f, TextAnchor.MiddleCenter, 3);
            glow = WorldKit.AddLight(label.transform, AsciiTheme.Green, 1f, 5f);

            var box = gameObject.AddComponent<BoxCollider2D>();
            box.isTrigger = true;
            box.size = new Vector2(1f, 40f);
            box.offset = new Vector2(0.5f, 6f);
        }

        void Update()
        {
            label.text = Label + (Mathf.Repeat(Time.unscaledTime, 0.8f) < 0.4f ? "_" : " ");
            glow.intensity = 1f + 0.3f * Mathf.Sin(Time.unscaledTime * 4f);
        }

        void OnTriggerEnter2D(Collider2D other)
        {
            if (done || WorldKit.Player(other) == null) return;
            done = true;
            label.color = AsciiTheme.White;
            AsciiParticles.Burst(label.transform.position, "01{};*#", AsciiTheme.Green, 24, 7f, 0.9f, 0.5f, 6f);
            var run = RunManager.Instance;
            if (run != null) run.Complete();
        }
    }
}
