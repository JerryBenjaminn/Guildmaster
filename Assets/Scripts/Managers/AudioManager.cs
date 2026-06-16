using UnityEngine;

namespace Guildmaster
{
    /// <summary>
    /// Audio entry point (CLAUDE.md §9). Exists as a working skeleton from day one
    /// but plays NOTHING in MVP — audio content is a later polish pass directed by
    /// Jerry. The hook is real so call-sites can be wired now.
    ///
    /// Public API:
    ///   Play(id)        - request a one-shot sound by id (no-op until content).
    ///   PlayMusic(id)   - request a music track by id (no-op until content).
    ///   StopMusic()     - stop current music (no-op until content).
    /// </summary>
    public class AudioManager : Singleton<AudioManager>
    {
        [Tooltip("When true, logs every Play() call — handy while wiring game feel.")]
        public bool logCalls = false;

        public void Play(string id)
        {
            if (logCalls) Debug.Log($"[AudioManager] Play('{id}') (no content yet)");
        }

        public void PlayMusic(string id)
        {
            if (logCalls) Debug.Log($"[AudioManager] PlayMusic('{id}') (no content yet)");
        }

        public void StopMusic()
        {
            if (logCalls) Debug.Log("[AudioManager] StopMusic() (no content yet)");
        }
    }
}
