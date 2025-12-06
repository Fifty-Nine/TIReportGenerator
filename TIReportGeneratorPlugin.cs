using BepInEx;
using HarmonyLib;
using PavonisInteractive.TerraInvicta;
using UnityEngine;

namespace TIReportGenerator
{
    [BepInPlugin(PluginInfo.PLUGIN_GUID, PluginInfo.PLUGIN_NAME, PluginInfo.PLUGIN_VERSION)]
    public class TIReportGeneratorPlugin : BaseUnityPlugin
    {
        private void Awake()
        {
            // Plugin startup logic
            Logger.LogInfo($"Plugin {PluginInfo.PLUGIN_GUID} is loaded!");

            Harmony.CreateAndPatchAll(typeof(GameControlPatch));
        }
    }

    public static class PluginInfo
    {
        public const string PLUGIN_GUID = "com.example.tireportgenerator";
        public const string PLUGIN_NAME = "TI Report Generator";
        public const string PLUGIN_VERSION = "1.0.0";
    }

    [HarmonyPatch(typeof(GameControl))]
    public static class GameControlPatch
    {
        [HarmonyPatch(nameof(GameControl.CompleteInit))]
        [HarmonyPostfix]
        public static void CompleteInitPostfix()
        {
            Debug.Log("Hello World - Game Loaded!");
            // Also log to BepInEx logger if possible, but Debug.Log is usually captured by BepInEx too.
            // If we wanted to access the plugin logger, we'd need to expose it static or passed.
            // For now, Debug.Log is sufficient and "Hello World" as requested.
            System.Console.WriteLine("Hello World");
        }
    }
}
