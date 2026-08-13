# Feats Every X Levels

Unity Mod Manager mod for Pathfinder: Wrath of the Righteous that makes the basic
feat progression spacing configurable.

Vanilla grants a basic feat at character levels 1, 3, 5, ... (every 2 levels).
This mod rebuilds `BasicFeatsProgression` so the feat is granted at level 1 and
then every X levels, where X is set in the UMM settings window (Ctrl+F10 in game):

- **1** — a feat at every character level (mod default)
- **2** — vanilla spacing (1, 3, 5, ...)
- **3+** — sparser than vanilla (1, 4, 7, ... etc.)

Entries are generated up to level 59, matching vanilla coverage of the Legend
mythic path's raised level cap.

## Notes

- Changing the value in the UMM window takes effect immediately for future
  level-ups — no restart needed.
- Levels you have already taken are not changed retroactively; respec a character
  to re-level with the new spacing.
- Only the `BasicFeatSelection` entries of the progression are touched; the other
  level-1 features and anything added by other mods (TabletopTweaks etc.) are
  preserved, so it should be compatible with the usual mod stacks.

## Building

```
dotnet build -c Release
```

The game path defaults to the Steam install at
`C:\Games\SteamLibrary\steamapps\common\Pathfinder Second Adventure`; override
with `-p:WrathPath="..."` if needed. A successful build copies
`FeatsEveryXLevels.dll` and `Info.json` straight into the game's
`Mods\FeatsEveryXLevels` folder.
