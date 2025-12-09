/*
 * Contains classes and methods that perform additional analysis not trivially provided by existing game objects.
 */
using System;
using System.Collections.Generic;
using System.Linq;
using PavonisInteractive.TerraInvicta;

public enum TreatyType
{
    Truce,
    NAP,
    IntelSharing
};

public enum InformalRelationType
{
    Pleased = -2,
    Tolerant = -1,
    Wary = 0,
    Annoyed = 10,
    Displeased = 20,
    Aggrieved = 30,
    Angry = 40,
    Furious = 50,
    Outraged = 60,
    Hate = 70
};

public class FactionRelation
{
    public TIFactionState From { get; }
    public TIFactionState To { get; }
    public List<TreatyType> Treaties { get; }

    public InformalRelationType Relationship { get; }
    public bool AtWar { get; }

    public static string TreatyName(TreatyType type)
    {
        return type switch
        {
            TreatyType.IntelSharing => "Intel Sharing",
            TreatyType.NAP => "Non-Aggression Pact",
            _ => type.ToString()
        };
    }

    public static IEnumerable<FactionRelation> BuildRelationsList(IEnumerable<TIFactionState> allFactions)
    {
        return allFactions.SelectMany(
            first => allFactions.Where(second => second != first)
                                .Select(second => new FactionRelation(first, second))
        );
    }

    public static InformalRelationType CategorizeHateValue(float hate)
    {
        return Enum.GetValues(typeof(InformalRelationType)).Cast<InformalRelationType>().OrderByDescending(v => (float)v).First(v => hate > (float)v);
    }

    public FactionRelation(TIFactionState from, TIFactionState to)
    {
        From = from;
        To = to;

        var hate = from.GetFactionHate(to);
        if (From.permanentAlly(to))
        {
            Relationship = InformalRelationType.Pleased;
            AtWar = false;
        }
        else
        {
            Relationship = CategorizeHateValue(hate);
            AtWar = from.AI_AtWarWithFaction(to);
        }

        Treaties = [];
        if (from.HasTruce(to))
        {
            Treaties.Add(TreatyType.Truce);
        }

        if (from.HasNAP(to))
        {
            Treaties.Add(TreatyType.NAP);
        }

        if (from.intelSharingFactions.Contains(to))
        {
            Treaties.Add(TreatyType.IntelSharing);
        }
    }
}