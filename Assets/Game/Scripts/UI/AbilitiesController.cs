using UnityEngine;
using UnityEngine.UI;

namespace RunDashC
{
    /// <summary>
    /// Escena Habilidades: estrellas conseguidas y las 10 habilidades (las bloqueadas salen "??", las arma AbilityList).
    /// La tarjeta de CORONA ESTELAR tiene PONER/QUITAR. VOLVER regresa a <see cref="ReturnTo"/> (menú o niveles).
    /// </summary>
    public class AbilitiesController : MonoBehaviour
    {
        /// <summary>Escena que abrió esta (la pone quien navega aquí).</summary>
        public static string ReturnTo = SceneNames.Menu;

        public Text starsText;
        public Button backButton;
        public Button coronaButton;

        bool leaving;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() => ReturnTo = SceneNames.Menu;

        void Awake()
        {
            int total = Stars.Total;
            string yellow = AsciiTheme.ToHex(AsciiTheme.Yellow), dim = AsciiTheme.ToHex(AsciiTheme.GreenDim);
            starsText.text = $"<color=#{yellow}>estrellas: {total}/{Stars.Max}</color>   " +
                             $"<color=#{dim}>// habilidades: {Abilities.UnlockedCount(total)}/{Abilities.All.Length}</color>";
            backButton.onClick.AddListener(Leave);
            coronaButton.gameObject.SetActive(Cosmetics.Owns(Cosmetics.Corona));
            coronaButton.onClick.AddListener(ToggleCorona);
            RefreshCorona();
        }

        void Start()
        {
            AudioManager.PlayMusic(Music.Menu);
            UiKit.Select(backButton.gameObject);
        }

        void Update() => UiKit.KeepSelection(backButton.gameObject);

        void ToggleCorona()
        {
            Cosmetics.Toggle(Cosmetics.Corona);
            PlayerPrefs.Save();
            RefreshCorona();
        }

        void RefreshCorona()
        {
            var ab = coronaButton.GetComponent<AsciiButton>();
            ab.label = Cosmetics.IsEquipped(Cosmetics.Corona) ? "QUITAR" : "PONER";
            ab.Refresh();
        }

        void Leave()
        {
            if (leaving) return;
            leaving = true;
            SceneFlow.Load(ReturnTo);
        }
    }
}
