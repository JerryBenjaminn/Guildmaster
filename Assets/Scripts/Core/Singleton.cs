using UnityEngine;

namespace Guildmaster
{
    /// <summary>
    /// Lightweight base for the manager singletons (CLAUDE.md §3.4). Managers are
    /// created by <see cref="GameManager"/> as components on a single
    /// DontDestroyOnLoad object, so this does not self-instantiate — it only
    /// enforces the single-instance invariant and exposes <c>Instance</c>.
    /// </summary>
    public abstract class Singleton<T> : MonoBehaviour where T : Singleton<T>
    {
        public static T Instance { get; private set; }

        protected virtual void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(this);
                return;
            }

            Instance = (T)this;
            OnAwake();
        }

        /// <summary>Override for one-time setup instead of hiding Awake.</summary>
        protected virtual void OnAwake() { }

        protected virtual void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
        }
    }
}
