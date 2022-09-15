using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AutoEvo;

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

        var bestBySelection = new Dictionary<SelectionPressure, Tuple<Species, double>>();

        // Assign a new best for each selection pressure
        foreach (var pressure in selectionPressures)
        {
            foreach (var species in Patch.SpeciesInPatch.Keys)
            {
                if (bestBySelection[pressure] == null ||
                    pressure.Score(species, Cache) > bestBySelection[pressure].Item2)
                {
                    bestBySelection[pressure] = new Tuple<Species, double>(species, pressure.Score(species, Cache));
                }
            }
        }

        // If it's not the player or not the best at something, bump it off
        foreach (var species in Patch.SpeciesInPatch.Keys)
        {
            if (!species.PlayerSpecies && !bestBySelection.Values.Select(x => x.Item1).Contains(species))
            {
                results.KillSpeciesInPatch(species, Patch, false);
            }
        }

        return true;
    }
}
