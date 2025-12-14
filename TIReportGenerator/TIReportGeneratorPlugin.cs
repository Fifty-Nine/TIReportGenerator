using BepInEx;
using HarmonyLib;
using PavonisInteractive.TerraInvicta;
using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using MonoMod.Utils;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;
using BetterConsoleTables;

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
            Harmony.CreateAndPatchAll(typeof(GameStateManagerPatch));
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
            GenerateReportsSafe();
        }

        public static readonly ISerializer YamlSerializer = new SerializerBuilder()
            .WithNamingConvention(PascalCaseNamingConvention.Instance)
            .DisableAliases()
            .WithTypeConverter(new Util.QuantityConverter())
            .WithTypeConverter(new Util.ModuleListConverter())
            .WithTypeConverter(new Util.SIConverter())
            .WithTypeConverter(new Util.CapacityUseConverter())
            .Build();

        public static void GenerateReportsSafe()
        {
            try
            {
                GenerateReports();
            }
            catch (Exception e)
            {
                TIReportGeneratorPlugin.Log.LogError(e.ToString());
                TIReportGeneratorPlugin.Log.LogError(e.StackTrace);
                e.LogDetailed();
            }
        }

        public static void GenerateReports()
        {
            if (saveName == null) {
                return;
            }
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
            GenerateReport(GenerateOrgsReport, "orgs", reportPath);
            GenerateReport(GenerateArmyReport, "armies", reportPath);
            GenerateReport(GenerateFactionRelationsReport, "relations", reportPath);
            GenerateReport(GenerateAlienActivityReport, "alien_activity", reportPath);
            GenerateReport(GenerateCouncilorReport, "councilors", reportPath);
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

            var factions = GameStateManager.AllFactions().Select(Extractors.FactionExtractor.Extract);
            writer.WriteLine(YamlSerializer.Serialize(factions));

            Table table = new(TableConfiguration.Unicode());
            table.From([.. factions]);
            writer.WriteLine(table);
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

            var allTemplates = GameStateManager.IterateByClass<TIFactionState>()
                                               .SelectMany(f => f.shipDesigns)
                                               .Select(Extractors.ShipTemplateExtractor.Extract);

            writer.WriteLine(YamlSerializer.Serialize(allTemplates));
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

        private static void GenerateOrgsReport(StreamWriter writer)
        {
            writer.WriteLine($"# Orgs Report as of {TITimeState.Now()}");

            var allOrgs = GameStateManager.IterateByClass<TIOrgState>()
                                          .Where(org => org.factionOrbit != null);
            foreach (var org in allOrgs) {
                writer.WriteLine(Renderers.RenderMarkdownDescription(org, Schemas.Orgs));
            }
        }

        private static void GenerateArmyReport(StreamWriter writer)
        {
            writer.WriteLine($"# World Armies Report as of {TITimeState.Now()}");

            var armies = GameStateManager.IterateByClass<TIArmyState>()
                                         .Where(a => !a.destroyed);

            writer.WriteLine(Renderers.RenderMarkdownTable(armies, Schemas.Armies));
        }

        private static void GenerateFactionRelationsReport(StreamWriter writer)
        {
            writer.WriteLine($"# Faction Relations Report as of {TITimeState.Now()}");
            var relations = FactionRelation.BuildRelationsList(GameStateManager.AllHumanFactions());

            writer.WriteLine(Renderers.RenderMarkdownTable(relations, Schemas.FactionRelations));
        }
        private static void GenerateAlienActivityReport(StreamWriter writer)
        {
            writer.WriteLine($"# Alien Activity Report as of {TITimeState.Now()}");
            var player = GameControl.control.activePlayer;
            var hate = player.GetEstimatedAlienHate();
            var warThreshold = TIGlobalConfig.globalConfig.factionHateWarThreshold;
            writer.WriteLine($"Estimated Alien Hate: {FactionRelation.CategorizeHateValue(hate)} ({hate:F0})");
            writer.WriteLine($"Note: Hate level above {warThreshold:F0} implies war footing, {warThreshold*4:F0} implies total war.");
            writer.WriteLine($"Hate last established on: {player.GetLastDateofFixedAlienHate().ToCustomTimeDateString()}");
            writer.WriteLine();
            writer.WriteLine("Known Alien councilors (if any):");
            writer.WriteLine(Renderers.RenderMarkdownTable(player.knownSpies.Where(spy => spy.faction == GameStateManager.AlienFaction()), Schemas.XenoCouncilor));
            writer.WriteLine("Known Xenoforming (if any):");
            writer.WriteLine(Renderers.RenderMarkdownTable(player.KnownXenoforming, Schemas.XenoformingSite));
        }

        private static void GenerateCouncilorReport(StreamWriter writer)
        {
            writer.WriteLine($"# Councilor Report as of {TITimeState.Now()}");

            var councilors = GameStateManager.IterateByClass<TICouncilorState>()
                                             .Where(c => c.faction != null &&
                                                         c.faction != GameStateManager.AlienFaction());

            foreach (var c in councilors)
            {
                writer.WriteLine(Renderers.RenderMarkdownDescription(c, Schemas.Councilor));
                writer.WriteLine(Renderers.RenderMarkdownTable(
                    Schemas.AllCouncilorAttributes().Where(a => a != CouncilorAttribute.Loyalty)
                                                    .Select(a => (a, Schemas.ComputeCouncilorStatValues(c, a))),
                    Schemas.CouncilorStats)
                );
                writer.WriteLine("Traits:");
                writer.WriteLine(Renderers.RenderMarkdownList(c.traits, Schemas.Trait));
            }
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

    [HarmonyPatch(typeof(GameStateManager))]
    public static class GameStateManagerPatch
    {
        [HarmonyPatch(nameof(GameStateManager.SaveAllGameStates), new Type[] { typeof(string), typeof(bool) })]
        [HarmonyPostfix]
        public static void SaveAllGameStates(string __0)
        {
            var filepath = __0;
            var name = Path.GetFileNameWithoutExtension(filepath);
            TIReportGeneratorPlugin.Log.LogInfo($"Save name set to {name}");
            GameControlPatch.saveName = name;
            GameControlPatch.GenerateReports();
        }
    }
}
