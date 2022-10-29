using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AutoEvo;

class CommonSelectionFunctions
{
    public static float SpeciesOsmoregulationCost(MicrobeSpecies species)
    {
        return MicrobeInternalCalculations.OsmoregulationCost(species.Organelles.Select(x => x.Definition), species.MembraneType);
    }

    public static float EnergyGenerationScore(MicrobeSpecies species, Compound compound, Patch patch,
            SimulationCache simulationCache)
    {
        var energyCreationScore = 0.0f;

        foreach (var organelle in species.Organelles)
        {
            foreach (var process in organelle.Definition.RunnableProcesses)
            {
                if (process.Process.Inputs.TryGetValue(compound, out var inputAmount))
                {
                    var processEfficiency = simulationCache.GetProcessMaximumSpeed(process, patch.Biome).Efficiency;

                    if (process.Process.Outputs.TryGetValue(Compound.ByName("glucose"), out var glucoseAmount))
                    {
                        energyCreationScore += glucoseAmount / inputAmount
                            * processEfficiency * Constants.AUTO_EVO_GLUCOSE_USE_SCORE_MULTIPLIER;
                    }

                    if (process.Process.Outputs.TryGetValue(Compound.ByName("atp"), out var atpAmount))
                    {
                        energyCreationScore += atpAmount / inputAmount
                            * processEfficiency * Constants.AUTO_EVO_ATP_USE_SCORE_MULTIPLIER;
                    }
                }
            }
        }

        return energyCreationScore;
    }

    public static Dictionary<SelectionPressure, Tuple<Species, double>> GetBestPressures(RunResults results, Patch patch, SimulationCache cache)
    {
        // TODO: do this some other way
        var selectionPressures = SelectionPressure.PressuresFromPatch(null, patch, cache, null);

        var bestBySelection = new Dictionary<SelectionPressure, Tuple<Species, double>>();

        var allSpecies = results.results.Values.Where(x => x.NewPopulationInPatches.Keys.Contains(patch))
            .Select(x => x.Species);

        // Assign a new best for each selection pressure
        foreach (var pressure in selectionPressures)
        {
            foreach (var species in allSpecies)
            {
                // Since mutations may have occurred by now, take those into account
                var latestSpecies = results.LastestVersionForSpecies(species);
                if (pressure.Score(species, cache) > 0 && (!bestBySelection.ContainsKey(pressure) || pressure.Score(latestSpecies, cache) > bestBySelection[pressure].Item2))
                {
                    bestBySelection[pressure] = new Tuple<Species, double>(species, pressure.Score(latestSpecies, cache));
                }
            }
        }

        return bestBySelection;
    }
}

