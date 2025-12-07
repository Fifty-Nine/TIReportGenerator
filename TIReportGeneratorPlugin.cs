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
                GenerateReport(saveName);
            }
        }
        private static void GenerateReport(string saveName)
        {
            var savePath = TIUtilities.GetSaveFilePath(saveName);
            var reportPath = Path.Combine(Path.GetDirectoryName(savePath), $"report_{Path.GetFileNameWithoutExtension(savePath)}.md");

            using (var writer = new StreamWriter(reportPath))
            {
                writer.WriteLine($"# Report for {Path.GetFileName(savePath)}");
                writer.WriteLine();

                var date = TITimeState.Now();
                writer.WriteLine($"**Date:** {date}");
                writer.WriteLine();

                var resourceValues = Enum.GetValues(typeof(FactionResource))
                                         .Cast<FactionResource>()
                                         .Where(value => value != FactionResource.None);
                var resourceNames = resourceValues.Select(value => value.ToString());

                writer.WriteLine("## Factions");
                writer.Write("| Name ");

                foreach (string resource in resourceNames) {
                    writer.Write($"| {resource} ");
                }
                writer.WriteLine("|");

                foreach (string resource in resourceNames) {
                    writer.Write("|---");
                }
                writer.WriteLine("|");

                foreach (var faction in GameStateManager.IterateByClass<TIFactionState>())
                {
                    if (faction.IsAlienFaction) continue;
                    var player = GameControl.control.activePlayer == faction ? " (player)" : " ";
                    writer.Write(
                        $"| {faction.displayName}{player}"
                    );

                    foreach (var resource in resourceValues) {
                        float amt = faction.GetCurrentResourceAmount(resource);
                        float income = faction.GetMonthlyIncome(resource);
                        writer.Write($"| {amt:F1} ({income:F1}/mo) ");
                    }
                    writer.WriteLine("|");
                }
            }
            TIReportGeneratorPlugin.Log.LogInfo($"[TIReport] Report written to {reportPath}");
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
            TIReportGeneratorPlugin.Log.LogInfo($"[TIReport] Save name set to {name}");
            GameControlPatch.saveName = name;
        }
    }
}
