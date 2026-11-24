using UnityEngine;
using UnityEngine.UI;

namespace RunDashC
{
    /// <summary>
    /// Lista de habilidades en columnas (pausa y escena Habilidades): icono + nombre + estrellas + descripción.
    /// Las bloqueadas salen como "??". Se arma en runtime porque depende de las estrellas guardadas.
    /// El origen (0,0) del RectTransform es la esquina superior izquierda de la primera fila.
    /// </summary>
    public class AbilityList : MonoBehaviour
    {
        public int rows = 5;
        public float columnWidth = 820f, rowStep = 54f, iconSize = 44f;
        public int nameSize = 24, descSize = 22;

        void Start() => Build();

        /// <summary>Centro del icono de la fila <paramref name="i"/> (en coordenadas locales).</summary>
        public Vector2 IconCenter(int i) => new Vector2((i / rows) * columnWidth + iconSize * 0.5f, -(i % rows) * rowStep - iconSize * 0.5f);

        public void Build()
        {
            for (int c = transform.childCount - 1; c >= 0; c--) Destroy(transform.GetChild(c).gameObject);
            string dim = AsciiTheme.ToHex(AsciiTheme.GreenDim), cyan = AsciiTheme.ToHex(AsciiTheme.Cyan);
            var anchor = new Vector2(0.5f, 0.5f);
            for (int i = 0; i < Abilities.All.Length; i++)
            {
                var a = Abilities.All[i];
                bool open = Abilities.Has(a.Kind);
                var icon = IconCenter(i);
                if (open)
                    UiKit.SpriteImage(transform, "Icon_" + a.Id, WorldKit.LoadSprite(a.Icon), anchor, icon, Vector2.one * iconSize);
                else
                    UiKit.Label(transform, "Icon_" + a.Id, "??", Mathf.RoundToInt(iconSize * 0.7f), AsciiTheme.GreenDim, TextAnchor.MiddleCenter, anchor, icon, Vector2.one * iconSize);

                float left = icon.x + iconSize * 0.5f + 14f;
                string name = open ? $"<color=#{cyan}>{a.Nombre}</color>" : $"<color=#{dim}>??</color>";
                string stars = $"<color=#{dim}>  // {a.Stars}*</color>";
                UiKit.LeftLabel(transform, "Name_" + a.Id, name + stars, nameSize, AsciiTheme.White, left, icon.y + nameSize * 0.5f);
                UiKit.LeftLabel(transform, "Desc_" + a.Id, open ? a.Desc : $"consigue {a.Stars} estrellas", descSize,
                    open ? AsciiTheme.White : AsciiTheme.GreenDim, left, icon.y - descSize * 0.55f);
            }
        }
    }
}
