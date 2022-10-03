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

    public bool CanRunConcurrently => true;

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
        var selectionPressures = new List<SelectionPressure>
        {
            new AutotrophEnergyEfficiencyPressure(Patch, 10.0f),
            new OsmoregulationEfficiencyPressure(Patch, 5.0f),
        };

        foreach (var possiblePrey in Patch.SpeciesInPatch.Keys)
        {
            selectionPressures.Add(new PredationEffectivenessPressure(possiblePrey, 10.0f));
        }

        var bestBySelection = new Dictionary<SelectionPressure, Tuple<Species, double>>();

        var allSpecies = Patch.SpeciesInPatch.Keys.ToList();
        allSpecies.AddRange(results.results.Values.Where(x => x.NewPopulationInPatches.Keys.Contains(Patch)).Select(x => x.Species));

        // Assign a new best for each selection pressure
        foreach (var pressure in selectionPressures)
        {
            foreach (var species in allSpecies)
            {
                if (!bestBySelection.ContainsKey(pressure) ||
                    pressure.Score(species, Cache) > bestBySelection[pressure].Item2)
                {
                    bestBySelection[pressure] = new Tuple<Species, double>(species, pressure.Score(species, Cache));
                }
            }
        }

        foreach (var bestSelection in bestBySelection.Keys)
        {
            GD.Print("Best at " + bestSelection.Name() + " is " + bestBySelection[bestSelection].Item1.FormattedName);
        }

        // If it's not the player or not the best at something, bump it off
        foreach (var species in allSpecies)
        {
            if (!species.PlayerSpecies && !bestBySelection.Values.Select(x => x.Item1).Contains(species))
            {
                GD.Print("Excluding "+species.FormattedName);
                results.KillSpeciesInPatch(species, Patch, false);
            } else
            {
                results.results[species].BestPressures[Patch] = bestBySelection.Where(x => x.Value.Item1 == species).Select(x => x.Key).ToList();
            }
        }

        return true;
    }
}
