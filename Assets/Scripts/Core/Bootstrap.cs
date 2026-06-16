using UnityEngine;

namespace Guildmaster
{
    /// <summary>
    /// The single scene's entry point (CLAUDE.md §3.1 — one MainGame scene, no
    /// transitions ever). Sits on one GameObject in MainGame and kicks off the
    /// manager bootstrap. Everything else (managers, UI shell) is created in code
    /// by <see cref="GameManager"/>, keeping the scene file minimal and robust.
    /// </summary>
    [DefaultExecutionOrder(-1000)]
    public class Bootstrap : MonoBehaviour
    {
        private void Awake()
        {
            GameManager.EnsureExists();
        }
    }
}
