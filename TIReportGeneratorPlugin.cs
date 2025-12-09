using BepInEx;
using BetterConsoleTables;
using HarmonyLib;
using PavonisInteractive.TerraInvicta;
using UnityEngine;
using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Runtime.CompilerServices;
using System.CodeDom.Compiler;
using PavonisInteractive.TerraInvicta.Systems;
using System.Runtime.InteropServices.ComTypes;

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
                var savePath = TIUtilities.GetSaveFilePath(saveName);
                var reportPath = Path.Combine(Path.GetDirectoryName(savePath), $"report_{saveName}");
                Directory.CreateDirectory(reportPath.ToString());
                GenerateReport(GenerateResourceReport, "faction_resources", reportPath);
                GenerateReport(GenerateNationsReport, "nations", reportPath);
                GenerateReport(GenerateHabsAndStationsReport, "habs_and_stations", reportPath);
                GenerateReport(GenerateFleetReport, "fleets", reportPath);
                GenerateReport(GenerateShipTemplateReport, "ship_templates", reportPath);
                GenerateReport(GenerateTechReport, "technology", reportPath);
                GenerateReport(GenerateProspectingReport, "prospecting", reportPath);
                /* todo:
                     - councilors/orgs
                     - army deployments
                     - alien activity
                     */
            }
        }

        private static string GetReportPath(string name, string dir)
        {
            return Path.Combine(dir, $"{name}.md");
        }

        private static void GenerateReport(Action<StreamWriter> fn, string name, string dir)
        {
            TIReportGeneratorPlugin.Log.LogInfo($"Generating report: {name}");

            var filePath = GetReportPath(name, dir);
            using var writer = new StreamWriter(filePath);
            fn(writer);
            writer.Flush();
            writer.Close();

            TIReportGeneratorPlugin.Log.LogInfo($"Report written to {filePath}");
        }

        private static void GenerateResourceReport(StreamWriter writer)
        {
            writer.WriteLine($"# Faction Resource Report as of {TITimeState.Now()}");
            foreach (var faction in GameStateManager.IterateByClass<TIFactionState>()) {
                writer.WriteLine(Renderers.RenderMarkdownDescription<TIFactionState>(
                    faction,
                    Schemas.FactionResources
                ));
            }
        }

        private static IEnumerable<(TINationState, string)> GetRelations(TINationState nation)
        {
            return nation.allies.Select(l => (l, "Ally"))
                         .Concat(nation.wars.Select(l => (l, "War")))
                         .Concat(nation.rivals.Select(l => (l, "Rival")));
        }

        private static void GenerateNationsReport(StreamWriter writer)
        {
            writer.WriteLine($"# Nations Report as of {TITimeState.Now()}");

            foreach (var nation in GameStateManager.IterateByClass<TINationState>()) {
                if (!nation.extant) continue;
                writer.Write(Renderers.RenderMarkdownDescription<TINationState>(
                    nation,
                    Schemas.Nations
                ));

                writer.WriteLine("Control Points:");
                writer.WriteLine(Renderers.RenderMarkdownTable(nation.controlPoints, Schemas.ControlPoint));
                writer.WriteLine("Faction Support:");
                writer.WriteLine(Renderers.RenderMarkdownTable(nation.publicOpinion, Schemas.PublicOpinion));
                writer.WriteLine(
                    Renderers.RenderMarkdownTable(
                        GetRelations(nation),
                        Schemas.NationalRelations
                    )
                );
            }
        }

        private static void GenerateHabsAndStationsReport(StreamWriter writer)
        {
            writer.WriteLine($"# Habs and Stations Report as of {TITimeState.Now()}");

            foreach (var hab in GameStateManager.IterateByClass<TIHabState>())
            {
                writer.Write(Renderers.RenderMarkdownDescription(hab, Schemas.HabsAndStations));
            }
        }

        private static void GenerateFleetReport(StreamWriter writer)
        {
            writer.WriteLine($"# Fleets Report as of {TITimeState.Now()}");

            foreach (var fleet in GameStateManager.IterateByClass<TISpaceFleetState>())
            {
                writer.WriteLine(Renderers.RenderMarkdownDescription(fleet, Schemas.Fleets));
                writer.WriteLine(Renderers.RenderMarkdownTable(fleet.ships, Schemas.Ships));
            }
        }

        private static void GenerateShipTemplateReport(StreamWriter writer)
        {
            writer.WriteLine($"# Ship Templates Report as of {TITimeState.Now()}");

            var allTemplates = GameStateManager.IterateByClass<TIFactionState>().SelectMany(f => f.shipDesigns);

            foreach (var template in allTemplates)
            {
                writer.WriteLine(Renderers.RenderMarkdownDescription(template, Schemas.ShipTemplate));
                writer.WriteLine("Nose Weapons:");
                writer.WriteLine(Renderers.RenderMarkdownList(template.noseWeapons, Schemas.ShipWeapon));
                writer.WriteLine("Hull Weapons:");
                writer.WriteLine(Renderers.RenderMarkdownList(template.hullWeapons, Schemas.ShipWeapon));
                writer.WriteLine("Utility modules:");
                writer.WriteLine(Renderers.RenderMarkdownList(template.utilityModules, Schemas.ShipUtilityModule));
                writer.WriteLine("Full Refuel Cost:");
                foreach (var kvp in Schemas.CollectRefuelCosts(template))
                {
                    if (kvp.Value < 0.05f && kvp.Value > -0.05f) continue;
                    writer.WriteLine($"  {TIUtilities.GetResourceString(kvp.Key)}: {TIUtilities.FormatSmallNumber(kvp.Value)}");
                }
                writer.WriteLine();
                writer.WriteLine("Build Cost (includes initial propellant):");
                foreach (var kvp in Schemas.CollectBuildCosts(template))
                {
                    if (kvp.Value < 0.05f && kvp.Value > -0.05f) continue;
                    writer.WriteLine($"  {TIUtilities.GetResourceString(kvp.Key)}: {TIUtilities.FormatSmallNumber(kvp.Value)}");
                }
                writer.WriteLine();
            }
        }

        private static bool IsAlienOnlyProject(TIProjectTemplate p)
        {
            var aliens = GameStateManager.AlienFaction();

            return p.factionPrereq.Count() == 1 && p.FactionPrereqsSatisfied(aliens) && !p.FactionPrereqsSatisfied(GameControl.control.activePlayer);
        }

        private static void GenerateTechReport(StreamWriter writer)
        {
            writer.WriteLine($"# Tech Report as of {TITimeState.Now()}");
            writer.WriteLine("Status Legend:");
            writer.WriteLine(" * Completed: You have finished this technology or project.");
            writer.WriteLine(" * Active: This technology or project is being researched.");
            writer.WriteLine(" * Available: Technology or project is ready to research.");
            writer.WriteLine(" * Locked: Prerequisites met, but project not yet rolled/unlocked by your faction.");
            writer.WriteLine(" * Blocked: Research prerequisites not met.");
            writer.WriteLine("## Global Technologies");
            var allTechs = TIGlobalResearchState.GetAllTechs().OrderBy(Schemas.GetTechStatus);
            writer.WriteLine(Renderers.RenderMarkdownTable(allTechs, Schemas.GlobalTechs));
            writer.WriteLine();
            writer.WriteLine("## Player Faction Projects");
            var filteredProjects = TIGlobalResearchState.GetAllProjects()
                                                        .Where(p => !IsAlienOnlyProject(p))
                                                        .OrderBy(Schemas.GetProjectStatus);
            writer.WriteLine(Renderers.RenderMarkdownTable(filteredProjects, Schemas.FactionProjects));
            writer.WriteLine();
        }

        private static void GenerateProspectingReport(StreamWriter writer)
        {
            writer.WriteLine($"# Prospect Hab Site Report as of {TITimeState.Now()}");

            var allBodiesAndSites = GameStateManager.AllSpaceBodies()
                                                    .Where(body => body.habSites.Any())
                                                    .SelectMany(b => b.habSites.Cast<TISpaceGameState>().Prepend(b));
            writer.WriteLine(Renderers.RenderMarkdownTable(allBodiesAndSites, Schemas.HabSitesAndBodies));
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
