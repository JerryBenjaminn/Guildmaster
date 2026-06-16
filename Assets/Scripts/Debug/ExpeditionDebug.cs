#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Guildmaster
{
    /// <summary>
    /// THROWAWAY editor-only debug hooks for exercising the send -> collect loop
    /// by hand before the real Quests / Expedition Prep UI exists (task 03+).
    /// Editor-only (#if UNITY_EDITOR), so it never ships. To use: enter Play mode,
    /// recruit a few adventurers on the Roster tab, select the "[Guildmaster]"
    /// object in the Hierarchy, then right-click the ExpeditionManager component
    /// header and pick one of the DEBUG menu items.
    ///
    /// Safe to delete this whole file (and the `partial` keyword on the class)
    /// once real expedition UI lands.
    /// </summary>
    public partial class ExpeditionManager
    {
        // Throwaway test convenience, NOT balance: short timer so a manual
        // send can be collected seconds later instead of minutes.
        private const long DebugDurationSeconds = 5;
        private const string DebugStubDungeonId = "stub_dungeon";

        // The stub dungeon lives under an Editor/ folder so it CANNOT ship in a
        // build; it isn't in Resources, so we load it via AssetDatabase (editor
        // only) and register it into ContentDatabase on demand.
        private const string DebugStubDungeonAssetPath = "Assets/Editor/DevTest/Dungeon_StubTest.asset";

        private void EnsureStubDungeonRegistered()
        {
            if (ContentDatabase.GetDungeon(DebugStubDungeonId) != null) return;
            var d = AssetDatabase.LoadAssetAtPath<DungeonData>(DebugStubDungeonAssetPath);
            if (d != null) ContentDatabase.RegisterDungeon(d);
            else Debug.LogWarning($"[DEBUG] Stub dungeon asset not found at {DebugStubDungeonAssetPath}.");
        }

        [ContextMenu("DEBUG ▸ Send stub expedition (up to 4 Healthy)")]
        private void DebugSendStubExpedition()
        {
            if (_current == null || _balance == null)
            {
                Debug.LogWarning("[DEBUG] Enter Play mode first (managers not initialized).");
                return;
            }

            EnsureStubDungeonRegistered();

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

            var exp = SendExpedition(DebugStubDungeonId, team, tier);
            if (exp == null) return;

            exp.durationSeconds = DebugDurationSeconds; // shorten for quick manual collect
            Debug.Log($"[DEBUG] Sent stub expedition with {team.Count} member(s). " +
                      $"Ready in ~{DebugDurationSeconds}s. Locked outcome: band={exp.outcome.band}, " +
                      $"success={exp.outcome.success}, chance={exp.outcome.successChance:0.00}, gold={exp.outcome.goldReward}.");
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
                          $"injured={o.injuredMemberIds.Count}, dead={o.deadMemberIds.Count}.");
            }

            if (UIManager.Instance != null) UIManager.Instance.Refresh();
        }
    }
}
#endif
