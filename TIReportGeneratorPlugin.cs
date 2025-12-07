using BepInEx;
using HarmonyLib;
using PavonisInteractive.TerraInvicta;
using UnityEngine;
using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;

namespace TIReportGenerator
{
    [BepInPlugin(PluginInfo.PLUGIN_GUID, PluginInfo.PLUGIN_NAME, PluginInfo.PLUGIN_VERSION)]
    public class TIReportGeneratorPlugin : BaseUnityPlugin
    {
        public static BepInEx.Logging.ManualLogSource Log;
        private void Awake()
        {
            Log = Logger;
            Log.LogInfo($"Plugin {PluginInfo.PLUGIN_GUID} is loaded!");

            Harmony.CreateAndPatchAll(typeof(GameControlPatch));
            Harmony.CreateAndPatchAll(typeof(LoadMenuControllerPatch));
        }
    }

    public static class PluginInfo
    {
        public const string PLUGIN_GUID = "com.trprince.tireportgenerator";
        public const string PLUGIN_NAME = "TI Report Generator";
        public const string PLUGIN_VERSION = "1.0.0";
    }

    [HarmonyPatch(typeof(GameControl))]
    public static class GameControlPatch
    {
        public static string saveName;

        [HarmonyPatch(nameof(GameControl.CompleteInit))]
        [HarmonyPostfix]
        public static void CompleteInitPostfix()
        {
            TIReportGeneratorPlugin.Log.LogInfo("GameControl initialization complete.");
            if (saveName != null) {
                GenerateResourceReport(saveName);
            }
        }
        private static void GenerateResourceReport(string saveName)
        {
            var savePath = TIUtilities.GetSaveFilePath(saveName);
            var reportPath = Path.Combine(Path.GetDirectoryName(savePath), $"report_resources_{Path.GetFileNameWithoutExtension(savePath)}.md");

            using (var writer = new StreamWriter(reportPath))
            {
                writer.WriteLine($"# Report for {Path.GetFileName(savePath)}");

                var date = TITimeState.Now();
                writer.WriteLine($"# Game date: {date}");

                var specialResources = new List<FactionResource> {
                    FactionResource.None,
                    FactionResource.MissionControl,
                    FactionResource.Research
                };
                var resourceValues = Enum.GetValues(typeof(FactionResource))
                                         .Cast<FactionResource>()
                                         .Where(value => !specialResources.Contains(value));
                var resourceNames = resourceValues.Select(value => value.ToString());
                var resourceHeaders = resourceNames.SelectMany(name => new string[] {name, $"{name}/mo"})
                                                   .ToList<string>();
                var headers = new string[] { "Name", "Player" }.Concat(resourceNames)
                                                               .AddItem("Mission Control (Usage/Capacity)")
                                                               .AddItem("Research" )
                                                               .AddItem("Control Points (Usage/Capacity)");

                writer.WriteLine(string.Join(", ", headers));

                foreach (var faction in GameStateManager.IterateByClass<TIFactionState>())
                {
                    if (faction.IsAlienFaction) continue;
                    var player = GameControl.control.activePlayer == faction ? "yes" : "no";
                    writer.WriteLine(
                        $"{faction.displayName}, {player}, " +
                        string.Join(
                            ", ",
                            resourceValues.Select(
                                resource =>
                                    $"{faction.GetCurrentResourceAmount(resource):F1}, " +
                                    $"{faction.GetMonthlyIncome(resource):+0.0;-0.0; 0.0}"
                            )
                        ) +
                        $", {faction.GetMissionControlUsage():F0}/" +
                        $"{faction.GetMonthlyIncome(FactionResource.MissionControl)}, " +
                        $"{faction.GetMonthlyIncome(FactionResource.Research):+0.0;-0.0; 0.0}, " +
                        $"{faction.GetBaselineControlPointMaintenanceCost():F0}/" +
                        $"{faction.GetControlPointMaintenanceFreebieCap():F0}"
                    );
                }
            }
            TIReportGeneratorPlugin.Log.LogInfo($"Report written to {reportPath}");
        }
    }

    [HarmonyPatch(typeof(LoadMenuController))]
    public static class LoadMenuControllerPatch
    {
        [HarmonyPatch(nameof(LoadMenuController.LoadSaveFile))]
        [HarmonyPrefix]
        public static void LoadSaveFilePrefix(LoadMenuController __instance)
        {
            var name = __instance.saveList.selectedButton.saveInfo.name;
            TIReportGeneratorPlugin.Log.LogInfo($"Save name set to {name}");
            GameControlPatch.saveName = name;
        }
    }
}
