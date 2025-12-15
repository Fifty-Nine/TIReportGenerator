using System;
using System.Collections.Generic;
using System.Linq;
using PavonisInteractive.TerraInvicta;
using TIReportGenerator.Util;

namespace TIReportGenerator.Extractors
{
    public static class ProspectingExtractor
    {
        private static Protos.ResourceYieldData ExtractResourceYield(TIHabSiteState site, bool prospected, FactionResource resource)
        {
            var result = new Protos.ResourceYieldData
            {
                Grade = prospected ? site.GetActualResourceGrade(resource).ToString() :
                                     site.GetExpectedResourceGrade(resource).ToString()
            };

            if (prospected)
            {
                result.Yield = site.GetMonthlyProduction(resource);
            }

            return result;
        }

        private static IDictionary<string, Protos.ResourceYieldData> ExtractResourceYields(TIHabSiteState site, bool prospected)
        {
            return GameEnums.AllSpaceResources().Select(r => (Resource: r, Yield: ExtractResourceYield(site, prospected, r)))
                                                .ToDictionary(p => TIUtilities.GetResourceString(p.Resource), p => p.Yield);
        }

        public static Protos.SiteData ExtractSite(TIHabSiteState site, bool prospected)
        {
            var data = new Protos.SiteData
            {
                Name = Util.ExtractName(site),
            };

            if (site.hasPlannedOrOperatingBase)
            {
                data.Status = Protos.SiteStatus.SiteOccupied;
                data.Owner = Util.ExtractName(site.hab.faction);
            }
            else if (prospected)
            {
                data.Status = Protos.SiteStatus.SiteAvailable;
                data.Owner = "none";
            }
            else
            {
                data.Status = Protos.SiteStatus.SiteNotProspected;
                data.Owner = "none";
            }

            data.Resources.Add(ExtractResourceYields(site, prospected));

            return data;
        }

        public static Protos.BodyData Extract(TISpaceBodyState body)
        {
            var data = new Protos.BodyData
            {
                Name = Util.ExtractName(body),
                IsProspected = GameControl.control.activePlayer.Prospected(body)
            };

            data.Sites.Add(
                body.habSites.Select(site => ExtractSite(site, data.IsProspected))
            );
            return data;
        }
    }
}
