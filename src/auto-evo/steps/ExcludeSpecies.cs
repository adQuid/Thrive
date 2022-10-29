using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AutoEvo;
using Godot;

class ExcludeSpecies : IRunStep
{
    public int TotalSteps => 1;

    public bool CanRunConcurrently => false;

    public Patch Patch;
    public SimulationCache Cache;

    public ExcludeSpecies(Patch patch, SimulationCache cache)
    {
        Patch = patch;
        Cache = cache;
    }

    public bool RunStep(RunResults results)
    {
        var allSpecies = results.results.Values.Where(x => x.NewPopulationInPatches.Keys.Contains(Patch))
            .Select(x => x.Species);

        var bestBySelection = CommonSelectionFunctions.GetBestPressures(results, Patch, Cache);

        foreach (var bestSelection in bestBySelection.Keys)
        {
            GD.Print("Best at " + String.Join(",", bestSelection.Select(x => x.Name())) + " is " + bestBySelection[bestSelection].FormattedName);
        }

        // If it's not the player and not the best at something, bump it off
        foreach (var species in allSpecies)
        {
            if (!species.PlayerSpecies && !bestBySelection.Select(x => x.Value).Contains(species))
            {
                GD.Print("Excluding "+ species.FormattedName);
                results.KillSpeciesInPatch(species, Patch, false);
            } else
            {
                results.results[species].BestPressures[Patch] = bestBySelection.Where(x => x.Value == species).Select(x => x.Key).ToList();
            }
        }

        return true;
    }
}
