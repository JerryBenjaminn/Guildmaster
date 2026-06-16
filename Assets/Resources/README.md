# Resources/

Runtime-loaded assets live here (loaded via `Resources.Load` / `ContentDatabase`).

**Required for tuned balance:** create a `BalanceConfig` asset and place it in this
folder named `BalanceConfig` so `GameManager` loads it at startup:

  Assets > Create > Guildmaster > Balance Config  →  rename to `BalanceConfig`

Until that asset exists the game runs on the SEED defaults baked into
`BalanceConfig.cs` (a warning is logged). The asset, once created, is the single
source of truth for all balance numbers (CLAUDE.md §3.3 / §7).

Content ScriptableObjects (DungeonData, AdventurerClassData, ItemData, etc.) can
live anywhere under any `Resources` folder; `ContentDatabase.LoadAll()` indexes
them by their `id` field at startup.
