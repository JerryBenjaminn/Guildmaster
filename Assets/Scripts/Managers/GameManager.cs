using System;
using UnityEngine;

namespace Guildmaster
{
    /// <summary>
    /// Top-level coordinator and SAVE ORCHESTRATOR (the §3.4 manager set is fixed
    /// at 7; the task allows folding SaveManager in here). Responsibilities:
    ///  - bootstrap all manager singletons onto one DontDestroyOnLoad object,
    ///  - load BalanceConfig (the single source of balance truth),
    ///  - own the three save-data objects and bind each manager to its slice,
    ///  - process offline progress on load (D1/D2),
    ///  - persist on pause/quit so progress survives an app kill.
    ///
    /// Single scene only (D-rule §3.1): no scene loads anywhere.
    ///
    /// Public API:
    ///   EnsureExists()    - idempotent bootstrap entry (called by Bootstrap).
    ///   Balance           - the active BalanceConfig.
    ///   Persistent/Legacy/Current - the live save-data objects.
    ///   SaveGame()/LoadGame()
    ///   NewGame()         - wipe current.json for a fresh start (persistent/legacy kept).
    /// </summary>
    public class GameManager : Singleton<GameManager>
    {
        public BalanceConfig Balance { get; private set; }
        public PersistentData Persistent { get; private set; }
        public LegacyData Legacy { get; private set; }
        public CurrentData Current { get; private set; }

        /// <summary>Idempotent bootstrap. Safe to call from any scene-entry script.</summary>
        public static void EnsureExists()
        {
            if (Instance != null) return;
            var go = new GameObject("[Guildmaster]");
            DontDestroyOnLoad(go);
            go.AddComponent<GameManager>(); // Awake -> OnAwake runs the bootstrap
        }

        protected override void OnAwake()
        {
            Balance = LoadBalance();

            if (!ContentDatabase.IsLoaded) ContentDatabase.LoadAll();

            // Create the other managers as siblings on this DontDestroyOnLoad object.
            // AddComponent runs each manager's Awake immediately, setting Instance.
            gameObject.AddComponent<AudioManager>();
            gameObject.AddComponent<GuildManager>();
            gameObject.AddComponent<AdventurerManager>();
            gameObject.AddComponent<LegacyManager>();
            gameObject.AddComponent<ExpeditionManager>();
            gameObject.AddComponent<UIManager>();

            LoadGame();
            ProcessOffline();
            GrantStartingGoldIfNeeded();

            // Heal any injuries whose real-time recovery elapsed while away (D2: no
            // offline cap on recovery).
            AdventurerManager.Instance.ProcessInjuryRecovery(DateTime.UtcNow.Ticks);

            UIManager.Instance.BuildShell();
            UIManager.Instance.Refresh();

            // Poll-light recovery check (not per-frame): coarse interval, refreshes
            // UI only when something actually heals.
            InvokeRepeating(nameof(RecoveryTick), RecoveryTickInterval, RecoveryTickInterval);
        }

        // Polling cadence for injury recovery — a UI freshness interval, not a
        // balance value.
        private const float RecoveryTickInterval = 5f;

        private void RecoveryTick()
        {
            if (AdventurerManager.Instance == null) return;
            int healed = AdventurerManager.Instance.ProcessInjuryRecovery(DateTime.UtcNow.Ticks);
            if (healed > 0 && UIManager.Instance != null) UIManager.Instance.Refresh();
        }

        /// <summary>
        /// One-time starting stipend for a new guild so the first recruits are
        /// affordable. Granted to protected savings (not the at-risk pool, D4) and
        /// flagged so it never repeats. Amount is a tunable seed in BalanceConfig.
        /// </summary>
        private void GrantStartingGoldIfNeeded()
        {
            if (Persistent == null || Persistent.startingGrantGiven) return;
            Persistent.protectedGold += Balance.startingGold;
            Persistent.startingGrantGiven = true;
            SaveGame(); // persist immediately so the flag survives an app kill
        }

        private BalanceConfig LoadBalance()
        {
            // Source of truth is the authored asset under a Resources folder. Until
            // one exists, fall back to an in-memory instance using the SO's seed
            // defaults so the game still runs (CLAUDE.md §3.3 — values live in the SO).
            var asset = Resources.Load<BalanceConfig>("BalanceConfig");
            if (asset != null) return asset;

            Debug.LogWarning("[GameManager] No BalanceConfig.asset in Resources; using seed defaults. " +
                             "Create one via Assets > Create > Guildmaster > Balance Config and put it in a Resources folder.");
            return ScriptableObject.CreateInstance<BalanceConfig>();
        }

        public void LoadGame()
        {
            Persistent = SaveSystem.Load<PersistentData>(SaveSystem.PersistentFile);
            Legacy = SaveSystem.Load<LegacyData>(SaveSystem.LegacyFile);
            Current = SaveSystem.Load<CurrentData>(SaveSystem.CurrentFile);

            GuildManager.Instance.Bind(Persistent);
            LegacyManager.Instance.Bind(Legacy);
            AdventurerManager.Instance.Initialize(Balance, Current);
            ExpeditionManager.Instance.Initialize(Balance, Current);
        }

        public void SaveGame()
        {
            if (Current != null) Current.lastSeenTicksUtc = DateTime.UtcNow.Ticks;
            SaveSystem.Save(SaveSystem.PersistentFile, Persistent);
            SaveSystem.Save(SaveSystem.LegacyFile, Legacy);
            SaveSystem.Save(SaveSystem.CurrentFile, Current);
        }

        /// <summary>Fresh start: wipe only current.json (D-rule §3.5). Lineage & guild kept.</summary>
        public void NewGame()
        {
            Current = new CurrentData();
            AdventurerManager.Instance.Initialize(Balance, Current);
            ExpeditionManager.Instance.Initialize(Balance, Current);
            SaveSystem.Save(SaveSystem.CurrentFile, Current);
        }

        /// <summary>
        /// Compute offline elapsed time on load and apply CONTINUOUS accrual,
        /// capped at the 12h offline cap (D2). Expeditions resolve by wall-clock in
        /// ExpeditionManager and need no catch-up here. Injury recovery is uncapped.
        /// </summary>
        private void ProcessOffline()
        {
            if (Current == null || Current.lastSeenTicksUtc <= 0) return;

            double cappedSeconds = OfflineCalculator.CappedElapsedSeconds(
                Current.lastSeenTicksUtc, DateTime.UtcNow.Ticks, Balance.OfflineCapSeconds);

            // No continuous-accrual systems exist yet (training is deferred). The
            // capped value is computed here so those systems plug in without
            // touching the offline rule. Expeditions are wall-clock and honest.
            if (cappedSeconds > 0)
            {
                Debug.Log($"[GameManager] Offline for {cappedSeconds:0}s (capped at {Balance.offlineCapHours}h).");
            }
        }

        private void OnApplicationPause(bool paused)
        {
            // On mobile, pause is the reliable "app is leaving" signal — save here.
            if (paused) SaveGame();
        }

        private void OnApplicationQuit()
        {
            SaveGame();
        }
    }
}
