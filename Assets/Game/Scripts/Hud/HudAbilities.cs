using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace RunDashC
{
    /// <summary>
    /// Franja inferior del HUD: las 3 estrellas del nivel (atenuadas si faltan) y los iconos de las habilidades
    /// desbloqueadas. El dash y el escudo TRY/CATCH se atenúan mientras no están listos. Se rehace al recoger una estrella.
    /// </summary>
    public class HudAbilities : MonoBehaviour
    {
        const float StarSize = 46f, IconSize = 44f, Gap = 8f, GroupGap = 30f;
        static readonly Color Off = new Color(1f, 1f, 1f, 0.22f);

        readonly List<(Ability kind, Image img)> icons = new List<(Ability, Image)>();

        void OnEnable()
        {
            Stars.Collected += OnStarCollected;
            Build();
        }

        void OnDisable() => Stars.Collected -= OnStarCollected;

        void OnStarCollected(int level, int index, int before) => Build();

        void Build()
        {
            for (int c = transform.childCount - 1; c >= 0; c--) Destroy(transform.GetChild(c).gameObject);
            icons.Clear();

            bool stars = !GameSession.IsEndless;
            var open = new List<Abilities.Info>();
            foreach (var a in Abilities.All)
                if (Abilities.Has(a.Kind)) open.Add(a);
            float width = (stars ? Stars.PerLevel * (StarSize + Gap) - Gap : 0f)
                          + (stars && open.Count > 0 ? GroupGap : 0f)
                          + (open.Count > 0 ? open.Count * (IconSize + Gap) - Gap : 0f);

            var anchor = new Vector2(0.5f, 0.5f);
            float x = -width * 0.5f;
            if (stars)
            {
                var sprite = WorldKit.LoadSprite("estrella");
                for (int i = 0; i < Stars.PerLevel; i++, x += StarSize + Gap)
                {
                    var img = UiKit.SpriteImage(transform, "Star" + i, sprite, anchor, new Vector2(x + StarSize * 0.5f, 0f), Vector2.one * StarSize);
                    if (img != null && !Stars.Has(GameSession.Level, i)) img.color = Off;
                }
                x += GroupGap - Gap;
            }
            foreach (var a in open)
            {
                var img = UiKit.SpriteImage(transform, a.Id, WorldKit.LoadSprite(a.Icon), anchor, new Vector2(x + IconSize * 0.5f, 0f), Vector2.one * IconSize);
                if (img != null) icons.Add((a.Kind, img));
                x += IconSize + Gap;
            }
        }

        void Update()
        {
            var run = RunManager.Instance;
            if (run == null || run.player == null) return;
            foreach (var (kind, img) in icons)
            {
                bool ready = kind == Ability.Dash || kind == Ability.SudoDash ? run.player.DashReady || run.player.Dashing
                    : kind != Ability.TryCatch || run.ShieldReady;
                img.color = ready ? Color.white : Off;
            }
        }
    }
}
