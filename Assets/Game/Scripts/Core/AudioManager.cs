using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace RunDashC
{
    public enum Sfx
    {
        Click, Hover, Type, Start, Jump, DoubleJump, Land, Bit, Hit, Stomp,
        Checkpoint, Pause, Countdown, Go, LevelComplete, GameOver
    }

    public enum Music { None, Menu, Level1, Shop }

    /// <summary>
    /// Audio persistente (lo crea GameBootstrap). Clips en Resources/Audio:
    /// Sfx.DoubleJump → "sfx_double_jump", Music.Level1 → "music_level1".
    /// Usa tiempo no escalado, así que funciona en pausa.
    /// </summary>
    public class AudioManager : MonoBehaviour
    {
        const int SfxVoices = 8;
        const float SfxVolume = 0.8f;
        const float MusicVolume = 0.5f;
        const float DuckedFactor = 0.3f;
        const float CrossfadeSeconds = 0.6f;

        static AudioManager instance;

        readonly Dictionary<string, AudioClip> cache = new Dictionary<string, AudioClip>();
        readonly AudioSource[] sfx = new AudioSource[SfxVoices];
        readonly AudioSource[] music = new AudioSource[2];
        int nextVoice;
        int activeMusic;
        float fade = 1f;
        float duck = 1f;
        Music current = Music.None;

        public static string FileName(Sfx s) => "sfx_" + ToSnake(s.ToString());
        public static string FileName(Music m) => "music_" + m.ToString().ToLowerInvariant();

        static string ToSnake(string s)
        {
            var sb = new StringBuilder();
            for (int i = 0; i < s.Length; i++)
            {
                if (i > 0 && char.IsUpper(s[i])) sb.Append('_');
                sb.Append(char.ToLowerInvariant(s[i]));
            }
            return sb.ToString();
        }

        void Awake()
        {
            if (instance != null && instance != this) { Destroy(this); return; }
            instance = this;
            for (int i = 0; i < SfxVoices; i++)
            {
                sfx[i] = gameObject.AddComponent<AudioSource>();
                sfx[i].playOnAwake = false;
            }
            for (int i = 0; i < music.Length; i++)
            {
                music[i] = gameObject.AddComponent<AudioSource>();
                music[i].playOnAwake = false;
                music[i].loop = true;
                music[i].volume = 0f;
            }
        }

        void OnDestroy()
        {
            if (instance == this) instance = null;
        }

        void Update()
        {
            fade = Mathf.MoveTowards(fade, 1f, Time.unscaledDeltaTime / CrossfadeSeconds);
            float target = MusicVolume * duck;
            var active = music[activeMusic];
            var old = music[1 - activeMusic];
            active.volume = target * fade;
            old.volume = target * (1f - fade);
            if (fade >= 1f && old.isPlaying) old.Stop();
        }

        AudioClip Clip(string name)
        {
            if (cache.TryGetValue(name, out var clip)) return clip;
            clip = Resources.Load<AudioClip>("Audio/" + name);
            if (clip == null) Debug.LogWarning($"[RunDashC] Falta el audio Resources/Audio/{name}");
            cache[name] = clip;
            return clip;
        }

        /// <param name="pitchJitter">Variación aleatoria de tono (0.05 = ±5%) para que no suene repetitivo.</param>
        public static void Play(Sfx s, float pitchJitter = 0f, float volume = 1f)
        {
            if (instance == null) return;
            var clip = instance.Clip(FileName(s));
            if (clip == null) return;
            var voice = instance.sfx[instance.nextVoice];
            instance.nextVoice = (instance.nextVoice + 1) % SfxVoices;
            voice.Stop();
            voice.clip = clip;
            voice.pitch = 1f + (pitchJitter > 0f ? Random.Range(-pitchJitter, pitchJitter) : 0f);
            voice.volume = SfxVolume * volume;
            voice.Play();
        }

        /// <summary>Cambia la música con crossfade. Music.None la apaga. Repetir la misma no la reinicia.</summary>
        public static void PlayMusic(Music m)
        {
            if (instance == null || instance.current == m) return;
            instance.current = m;
            instance.activeMusic = 1 - instance.activeMusic;
            var src = instance.music[instance.activeMusic];
            src.clip = m == Music.None ? null : instance.Clip(FileName(m));
            if (src.clip != null) src.Play();
            instance.fade = 0f;
        }

        /// <summary>Baja la música (pausa) o la regresa a su volumen normal.</summary>
        public static void SetMusicDucked(bool ducked)
        {
            if (instance == null) return;
            instance.duck = ducked ? DuckedFactor : 1f;
        }
    }
}
