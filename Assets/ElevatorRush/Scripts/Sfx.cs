using System.Collections.Generic;
using UnityEngine;

namespace ElevatorRush
{
    public enum SfxKey
    {
        Dock,
        Perfect,
        Deliver,
        Coin,
        Angry,
        Win,
        Fail,
        Button,
        /// <summary>A purchase in a Home panel.</summary>
        Buy,
        /// <summary>An "Unlocked!" popup.</summary>
        Unlock
    }

    /// <summary>
    /// Sound effects by key, played on a small pool of audio sources. A missing clip falls back to
    /// a generated tone, so every action is audible even without downloaded audio.
    /// </summary>
    public sealed class Sfx : MonoBehaviour
    {
        const int SampleRate = 44100;

        static readonly string[] Keys =
        {
            "sfx_dock", "sfx_perfect", "sfx_deliver", "sfx_coin", "sfx_angry", "sfx_win", "sfx_fail", "sfx_button",
            // Buying reuses the bright confirmation chime; unlocking reuses the win jingle.
            "sfx_perfect", "sfx_win",
        };

        // Fallback tones: frequency (Hz) and length (s) per key, in SfxKey order.
        static readonly float[] ToneFrequencies = { 330f, 660f, 520f, 1040f, 160f, 784f, 196f, 880f, 700f, 988f };
        static readonly float[] ToneLengths = { 0.12f, 0.2f, 0.15f, 0.1f, 0.3f, 0.6f, 0.6f, 0.06f, 0.2f, 0.6f };

        readonly Dictionary<SfxKey, AudioClip> clips = new Dictionary<SfxKey, AudioClip>();
        GameConfig config;
        AssetLibrary assets;
        AudioSource[] voices;
        int nextVoice;

        public bool Muted { get; set; }

        public void Init(GameConfig config, AssetLibrary assets)
        {
            this.config = config;
            this.assets = assets;
            voices = new AudioSource[Mathf.Max(1, config.sfxVoices)];
            for (int i = 0; i < voices.Length; i++)
            {
                var source = gameObject.AddComponent<AudioSource>();
                source.playOnAwake = false;
                source.spatialBlend = 0f;
                voices[i] = source;
            }
            // Load or generate everything up front so nothing allocates during play.
            foreach (SfxKey key in System.Enum.GetValues(typeof(SfxKey)))
                Clip(key);
        }

        public void Play(SfxKey key, float pitch = 1f)
        {
            if (Muted)
                return;
            var source = voices[nextVoice];
            nextVoice = (nextVoice + 1) % voices.Length;
            source.pitch = pitch;
            source.PlayOneShot(Clip(key), config.sfxVolume);
        }

        AudioClip Clip(SfxKey key)
        {
            if (clips.TryGetValue(key, out var clip))
                return clip;
            int i = (int)key;
            clip = assets.Sound(Keys[i]);
            if (clip == null)
                clip = Tone(Keys[i], ToneFrequencies[i], ToneLengths[i]);
            clips.Add(key, clip);
            return clip;
        }

        /// <summary>A short sine blip with a fast attack and an exponential decay.</summary>
        static AudioClip Tone(string name, float frequency, float length)
        {
            int samples = Mathf.CeilToInt(SampleRate * length);
            var data = new float[samples];
            const float attack = 0.005f;
            const float level = 0.5f;
            for (int s = 0; s < samples; s++)
            {
                float t = (float)s / SampleRate;
                float envelope = Mathf.Min(1f, t / attack) * Mathf.Exp(-t * 6f / length);
                data[s] = Mathf.Sin(2f * Mathf.PI * frequency * t) * envelope * level;
            }
            var clip = AudioClip.Create(name, samples, 1, SampleRate, false);
            clip.SetData(data, 0);
            return clip;
        }
    }
}
