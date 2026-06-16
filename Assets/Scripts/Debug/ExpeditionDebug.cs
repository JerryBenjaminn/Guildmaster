#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Guildmaster
{
    /// <summary>
    /// THROWAWAY editor-only debug hooks for exercising the send -> collect loop
    /// by hand before the real Quests / Expedition Prep UI exists (task 04+).
    /// Editor-only (#if UNITY_EDITOR), so it never ships. To use: enter Play mode,
    /// recruit a few adventurers on the Roster tab, select the "[Guildmaster]"
    /// object in the Hierarchy, then right-click the ExpeditionManager component
    /// header and pick a DEBUG menu item.
    ///
    /// Two stub dungeons (both under Assets/Editor, so they can't ship): an EASY
    /// one (clears reliably) and a HARD one (success clamps near 5%, so Failure /
    /// injury / death paths fire on demand). Safe to delete this whole file (and
    /// the `partial` keyword on the class) once real expedition UI lands.
    /// </summary>
    public partial class ExpeditionManager
    {
        // Throwaway test convenience, NOT balance: short timer so a manual send can
        // be collected seconds later instead of minutes. (Set after the real D8
        // duration is computed and logged, so D8 is still observable.)
        private const long DebugDurationSeconds = 5;

        private const string DebugEasyDungeonId = "stub_dungeon";
        private const string DebugHardDungeonId = "stub_dungeon_hard";
        private const string DebugEasyDungeonPath = "Assets/Editor/DevTest/Dungeon_StubTest.asset";
        private const string DebugHardDungeonPath = "Assets/Editor/DevTest/Dungeon_StubHard.asset";

        private void EnsureStubDungeon(string id, string path)
        {
            if (ContentDatabase.GetDungeon(id) != null) return;
            var d = AssetDatabase.LoadAssetAtPath<DungeonData>(path);
            if (d != null) ContentDatabase.RegisterDungeon(d);
            else Debug.LogWarning($"[DEBUG] Stub dungeon asset not found at {path}.");
        }

        [ContextMenu("DEBUG ▸ Send EASY stub expedition (up to 4 Healthy)")]
        private void DebugSendEasy() => DebugSend(DebugEasyDungeonId, DebugEasyDungeonPath, "EASY");

        [ContextMenu("DEBUG ▸ Send HARD stub expedition (forces failures/deaths)")]
        private void DebugSendHard() => DebugSend(DebugHardDungeonId, DebugHardDungeonPath, "HARD");

        private void DebugSend(string dungeonId, string assetPath, string label)
        {
            if (_current == null || _balance == null)
            {
                Debug.LogWarning("[DEBUG] Enter Play mode first (managers not initialized).");
                return;
            }
            EnsureStubDungeon(dungeonId, assetPath);

            var team = new List<string>();
            foreach (var a in _current.roster)
            {
                if (a.status == AdventurerStatus.Healthy)
                {
                    team.Add(a.id);
                    if (team.Count == 4) break;
                }
            }
            if (team.Count == 0)
            {
                Debug.LogWarning("[DEBUG] No Healthy adventurers. Recruit some on the Roster tab first.");
                return;
            }

            ExpeditionTier tier = (_balance.expeditionTiers != null && _balance.expeditionTiers.Count > 0)
                ? _balance.expeditionTiers[0]
                : new ExpeditionTier("DEV", 10, 240);

            var exp = SendExpedition(dungeonId, team, tier);
            if (exp == null) return;

            long d8Duration = exp.durationSeconds; // duration after D8 Speed reduction
            int teamSpeed = ComputeTeamSpeed(team);
            float reduction = ExpeditionTiming.SpeedReduction(teamSpeed, _balance);

            exp.durationSeconds = DebugDurationSeconds; // shorten for a quick manual collect

            Debug.Log($"[DEBUG] Sent {label} stub with {team.Count} member(s). " +
                      $"D8: teamSpeed={teamSpeed}, reduction={reduction:P1}, " +
                      $"duration {tier.DurationSeconds}s -> {d8Duration}s " +
                      $"(overridden to {DebugDurationSeconds}s for quick test). " +
                      $"Locked outcome: band={exp.outcome.band}, success={exp.outcome.success}, " +
                      $"chance={exp.outcome.successChance:0.00}, gold={exp.outcome.goldReward}, " +
                      $"injuries={exp.outcome.injuries.Count}, deaths={exp.outcome.deadMemberIds.Count}.");
        }

        [ContextMenu("DEBUG ▸ Collect ready expeditions")]
        private void DebugCollectReady()
        {
            if (_current == null)
            {
                Debug.LogWarning("[DEBUG] Enter Play mode first.");
                return;
            }

            var ready = _current.activeExpeditions.FindAll(e => !e.collected && CanComplete(e));
            if (ready.Count == 0)
            {
                Debug.Log("[DEBUG] Nothing ready to collect yet (wait for the timer).");
                return;
            }

            foreach (var e in ready)
            {
                var o = Collect(e.id);
                if (o == null) continue;
                Debug.Log($"[DEBUG] Collected: band={o.band}, gold={o.goldReward}, xp={o.xpReward}, " +
                          $"injuries={o.injuries.Count}, deaths={o.deadMemberIds.Count}.");
            }

            if (UIManager.Instance != null) UIManager.Instance.Refresh();
        }
    }
}
#endif
