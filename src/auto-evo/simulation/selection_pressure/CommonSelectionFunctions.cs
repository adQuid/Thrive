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

    public static Dictionary<List<SelectionPressure>, Species> GetBestPressures(RunResults results, Patch patch, SimulationCache cache)
    {
        // TODO: do this some other way
        var miches = SelectionPressure.MichesForPatch(patch, cache);

        var bestBySelection = new Dictionary<List<SelectionPressure>, Species>();

        var allSpecies = results.results.Values.Where(x => x.NewPopulationInPatches.Keys.Contains(patch))
            .Select(x => x.Species);

        foreach (var miche in miches)
        {
            foreach (var traversal in miche.AllTraversals())
            {
                var qualifiedSpecies = new Dictionary<Species, double>();
                foreach (var species in allSpecies)
                {
                    qualifiedSpecies[species] = 0;
                }

                foreach (var pressure in traversal)
                {
                    var remainingQualifiedSpecies = new Dictionary<Species, double>(qualifiedSpecies);

                    foreach (var species in qualifiedSpecies.Keys)
                    {
                        var score = pressure.Score(species, cache);
                        if (score > 0)
                        {
                            remainingQualifiedSpecies.Add(species, score);
                        }
                    }

                    qualifiedSpecies = remainingQualifiedSpecies;
                }

                if (qualifiedSpecies.Count > 0)
                {
                    bestBySelection[traversal] = qualifiedSpecies.OrderByDescending(x => x.Value).First().Key;
                }
            }
        }

        return bestBySelection;
    }
}