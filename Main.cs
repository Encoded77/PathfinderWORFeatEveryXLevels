using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Classes;
using Kingmaker.Blueprints.Classes.Selection;
using Kingmaker.Blueprints.JsonSystem;
using UnityEngine;
using UnityModManagerNet;

namespace FeatsEveryXLevels
{
    public class Settings : UnityModManager.ModSettings
    {
        public int Interval = 1;

        public override void Save(UnityModManager.ModEntry modEntry) => Save(this, modEntry);
    }

    public static class Main
    {
        private const string BasicFeatsProgressionGuid = "5b72dd2ca2cb73b49903806ee8986325";
        private const string BasicFeatSelectionGuid = "247a4068296e8be42890143f451b4b45";
        // Vanilla entries run to 59 so the Legend mythic path (level cap 40) stays covered.
        private const int MaxLevel = 59;
        private const int MaxInterval = 10;

        // LevelEntry.m_Features is private; the public Features proxy wraps this same list.
        private static readonly FieldInfo FeaturesField = AccessTools.Field(typeof(LevelEntry), "m_Features");

        internal static UnityModManager.ModEntry.ModLogger Log;
        internal static Settings Settings;
        internal static bool BlueprintsLoaded;

        public static bool Load(UnityModManager.ModEntry modEntry)
        {
            Log = modEntry.Logger;
            Settings = UnityModManager.ModSettings.Load<Settings>(modEntry);
            Settings.Interval = Mathf.Clamp(Settings.Interval, 1, MaxInterval);
            modEntry.OnGUI = OnGUI;
            modEntry.OnSaveGUI = OnSaveGUI;
            new Harmony(modEntry.Info.Id).PatchAll(Assembly.GetExecutingAssembly());
            return true;
        }

        private static void OnGUI(UnityModManager.ModEntry modEntry)
        {
            int interval = Settings.Interval;

            GUILayout.BeginHorizontal();
            GUILayout.Label("Grant a basic feat every", GUILayout.ExpandWidth(false));
            GUILayout.Space(5);
            if (GUILayout.Button(" - ", GUILayout.ExpandWidth(false))) interval--;
            GUILayout.Label($" {interval} ", GUILayout.ExpandWidth(false));
            if (GUILayout.Button(" + ", GUILayout.ExpandWidth(false))) interval++;
            GUILayout.Space(5);
            GUILayout.Label("level(s)", GUILayout.ExpandWidth(false));
            GUILayout.EndHorizontal();

            interval = Mathf.Clamp(interval, 1, MaxInterval);

            GUILayout.Label(FeatLevelsPreview(interval));
            GUILayout.Label("1 = a feat at every level, 2 = vanilla progression (levels 1, 3, 5, ...).");
            GUILayout.Label("Changes apply to future level-ups right away; levels already taken keep what they have (respec to re-level with the new spacing).");

            if (interval != Settings.Interval)
            {
                Settings.Interval = interval;
                ApplyProgression();
                Settings.Save(modEntry);
            }
        }

        private static string FeatLevelsPreview(int interval)
        {
            var levels = new List<string>();
            for (int level = 1; level <= 20; level += interval) levels.Add(level.ToString());
            return $"Feat at character levels: {string.Join(", ", levels)}, ...";
        }

        private static void OnSaveGUI(UnityModManager.ModEntry modEntry) => Settings.Save(modEntry);

        internal static void ApplyProgression()
        {
            if (!BlueprintsLoaded) return;
            try
            {
                var progression = ResourcesLibrary.TryGetBlueprint<BlueprintProgression>(BasicFeatsProgressionGuid);
                var featSelection = ResourcesLibrary.TryGetBlueprint<BlueprintFeatureSelection>(BasicFeatSelectionGuid);
                if (progression == null || featSelection == null)
                {
                    Log.Error("BasicFeatsProgression or BasicFeatSelection blueprint not found; feat spacing left unchanged.");
                    return;
                }

                int interval = Mathf.Clamp(Settings.Interval, 1, MaxInterval);
                var entries = progression.LevelEntries.ToList();

                // Strip BasicFeatSelection everywhere, keep the other level-1 features
                // (Touch of Law calc features etc.), then re-add it at the chosen spacing.
                // Mutating the list in place keeps the game's Features proxy in sync.
                foreach (var entry in entries)
                    Features(entry).RemoveAll(r => r != null && r.Guid == featSelection.AssetGuid);

                for (int level = 1; level <= MaxLevel; level += interval)
                {
                    var entry = entries.FirstOrDefault(e => e.Level == level);
                    if (entry == null)
                    {
                        entry = new LevelEntry { Level = level };
                        entries.Add(entry);
                    }
                    Features(entry).Add(featSelection.ToReference<BlueprintFeatureBaseReference>());
                }

                progression.LevelEntries = entries
                    .Where(e => Features(e).Count > 0)
                    .OrderBy(e => e.Level)
                    .ToArray();

                Log.Log($"Applied: basic feat at level 1 and every {interval} level(s) after.");
            }
            catch (Exception e)
            {
                Log.Error($"Failed to apply feat spacing: {e}");
            }
        }

        private static List<BlueprintFeatureBaseReference> Features(LevelEntry entry) =>
            (List<BlueprintFeatureBaseReference>)FeaturesField.GetValue(entry);
    }

    [HarmonyPatch(typeof(BlueprintsCache), nameof(BlueprintsCache.Init))]
    internal static class BlueprintsCache_Init_Patch
    {
        private static void Postfix()
        {
            Main.BlueprintsLoaded = true;
            Main.ApplyProgression();
        }
    }
}
