using System.Collections.Generic;
using System.Linq;
using AutoEvo;

class ApplyPopulations : IRunStep
{
    public int TotalSteps => 1;

    public bool CanRunConcurrently => false;

    Patch Patch;
    public ApplyPopulations(Patch patch)
    {
        Patch = patch;
    }

    public bool RunStep(RunResults results)
    {
        foreach (var traversal in results.MicheByPatch[Patch].AllTraversals())
        {
            var speciesToAdd = traversal.Last().Occupant;

            if (speciesToAdd == null)
            {
                continue;
            }

            var population = new Dictionary<Patch, long>();
            population[Patch] = 0;

            // TODO: Make it so that species that split the miche split the population
            foreach (var miche in traversal)
            {
                population[Patch] += miche.Pressure.EnergyProvided;
            }

            if (results.AncestorDictionary.ContainsKey(speciesToAdd))
            {
                results.AddNewSpecies(speciesToAdd, population, RunResults.NewSpeciesType.FillNiche, results.AncestorDictionary[speciesToAdd]);
            }
            else
            {
                results.AddNewSpecies(speciesToAdd, population, RunResults.NewSpeciesType.FillNiche, null);
            }
        }

        foreach (var species in Patch.SpeciesInPatch)
        {
            if (!species.Key.PlayerSpecies && !results.MicheByPatch[Patch].AllOccupants().Contains(species.Key))
            {
                results.KillSpeciesInPatch(species.Key, Patch);
            }
        }

        return true;
    }
}
