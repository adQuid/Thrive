using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AutoEvo;

class CommonSelectionFunctions
{
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
}

