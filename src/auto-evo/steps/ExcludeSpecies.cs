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
        // TODO: do this some other way
        var selectionPressures = SelectionPressure.PressuresFromPatch(null, Patch, Cache, null);

        var bestBySelection = new Dictionary<SelectionPressure, Tuple<Species, double>>();

        var allSpecies = results.results.Values.Where(x => x.NewPopulationInPatches.Keys.Contains(Patch))
            .Select(x => x.Species);

        // Assign a new best for each selection pressure
        foreach (var pressure in selectionPressures)
        {
            foreach (var species in allSpecies)
            {
                if (pressure.Score(species, Cache) > 0 && (!bestBySelection.ContainsKey(pressure) || pressure.Score(species, Cache) > bestBySelection[pressure].Item2))
                {
                    bestBySelection[pressure] = new Tuple<Species, double>(species, pressure.Score(species, Cache));
                }
            }
        }

        foreach (var bestSelection in bestBySelection.Keys)
        {
            GD.Print("Best at " + bestSelection.Name() + " is " + bestBySelection[bestSelection].Item1.FormattedName + " with " + bestBySelection[bestSelection].Item2 + " ( speed " + bestBySelection[bestSelection].Item1.BaseSpeed + ")");
        }

        // If it's not the player and not the best at something, bump it off
        foreach (var species in allSpecies)
        {
            if (!species.PlayerSpecies && !bestBySelection.Where(x => x.Key.Exclusive).Select(x => x.Value).Select(x => x.Item1).Contains(species))
            {
                GD.Print("Excluding "+ species.FormattedName);
                results.KillSpeciesInPatch(species, Patch, false);
            } else
            {
                results.results[species].BestPressures[Patch] = bestBySelection.Where(x => x.Value.Item1 == species).Select(x => x.Key).ToList();
            }
        }

        return true;
    }
}
