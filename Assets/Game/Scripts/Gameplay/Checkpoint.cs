using System.Collections;
using UnityEngine;

namespace RunDashC
{
    /// <summary>Checkpoint 'C': poste "|" con "// save();" tenue; al pasar se vuelve cian y es el nuevo punto de reaparición.</summary>
    public class Checkpoint : MonoBehaviour
    {
        const string Label = "// save();";

        TextMesh pole, label;
        SpriteRenderer flag;
        bool reached;

        void Start()
        {
            var code = ProdSkin.Split(transform, out var prod);
            if (prod != null) // bandera: asta y paño (rojo; verde al pasar)
            {
                WorldKit.AddRect(prod, new Vector2(0f, 0.6f), new Vector2(0.1f, 2.2f), AsciiTheme.ProdPole, 2);
                flag = WorldKit.AddRect(prod, new Vector2(0.4f, 1.45f), new Vector2(0.7f, 0.45f), AsciiTheme.ProdFlag, 2);
            }
            pole = WorldKit.AddText(code, "|\n|\n|", new Vector2(0f, -0.5f), AsciiTheme.GreenDim, 0.8f, TextAnchor.LowerCenter, 2);
            pole.lineSpacing = 0.72f; // '|' mide 0.72 em: las líneas se tocan
            label = WorldKit.AddText(code, Label, new Vector2(0.3f, 1.3f), AsciiTheme.GreenDim, 0.7f, TextAnchor.MiddleLeft, 2);
            var box = gameObject.AddComponent<BoxCollider2D>();
            box.isTrigger = true;
            box.size = new Vector2(0.6f, 40f); // toda la columna: pasar por arriba también cuenta
        }

        void OnTriggerEnter2D(Collider2D other)
        {
            if (reached || WorldKit.Player(other) == null) return;
            reached = true;
            var run = RunManager.Instance;
            if (run != null) run.SetCheckpoint(new Vector2(transform.position.x, transform.position.y - 0.5f + 0.32f));
            pole.color = AsciiTheme.Cyan;
            if (flag != null) flag.color = AsciiTheme.ProdGrass;
            AudioManager.Play(Sfx.Checkpoint);
            RunEvents.RaiseCheckpoint();
            AsciiParticles.Burst(transform.position + Vector3.up * 0.8f, "#*+01", AsciiTheme.Cyan, 10, 4f, 0.5f, 0.4f, 6f);
            StartCoroutine(TypeLabel());
        }

        IEnumerator TypeLabel()
        {
            label.color = AsciiTheme.Cyan;
            for (int i = 0; i <= Label.Length; i++)
            {
                label.text = Label.Substring(0, i) + "_";
                yield return new WaitForSeconds(0.035f);
            }
            label.text = Label;
        }
    }
}
