using System.Collections.Generic;
using AnglersEye.Core;
using AnglersEye.Core.Model;
using UnityEngine;

namespace AnglersEye.UI
{
    /// <summary>
    /// A hookable nibble: the strip flashes BITE! for as long as it can be hooked, and a short
    /// vanilla UI sound plays in 2D through the game's GUI mixer (PLAN §2.2 item 3).
    /// </summary>
    internal static class BiteCue
    {
        // Vanilla clip names found by the runtime probe (PLAN §1.7.3), most preferred first.
        private static readonly string[] Candidates = { "UI_Craft_Finish_01", "Ui_Click_01" };

        private static float _until;
        private static bool _lookedUp;
        private static AudioClip _clip;
        private static AudioSource _source;

        public static bool Active => Time.time < _until;

        public static void Fire()
        {
            if (!Features.On(Feature.BiteCue))
                return;
            _until = Time.time + ReelPolicy.HookWindow(Features.On(Feature.HookWindow), PluginConfig.HookWindowSeconds.Value);
            if (PluginConfig.BiteSound.Value)
                Play();
        }

        private static void Play()
        {
            if (!_lookedUp)
            {
                _lookedUp = true;
                var byName = new Dictionary<string, AudioClip>();
                foreach (AudioClip c in Resources.FindObjectsOfTypeAll<AudioClip>())
                    if (c != null && !byName.ContainsKey(c.name))
                        byName[c.name] = c;
                foreach (string n in Candidates)
                    if (byName.TryGetValue(n, out _clip))
                        break;
                if (_clip == null)
                    AnglersEyePlugin.Log.LogWarning("Angler's Eye: no bite sound found; the visual cue still works.");
            }
            if (_clip == null)
                return;
            if (_source == null)
            {
                _source = AnglersEyePlugin.Instance.gameObject.AddComponent<AudioSource>();
                _source.playOnAwake = false;
                _source.spatialBlend = 0f;
            }
            if (AudioMan.instance != null && _source.outputAudioMixerGroup == null)
                _source.outputAudioMixerGroup = AudioMan.instance.m_guiMixer;
            _source.PlayOneShot(_clip, PluginConfig.BiteVolume.Value);
        }
    }
}
