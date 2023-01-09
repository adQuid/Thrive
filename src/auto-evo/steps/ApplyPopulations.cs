using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AutoEvo;
using Godot;

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
        foreach (var speciesToAdd in results.MicheByPatch[Patch].AllOccupants())
        {
            var population = new Dictionary<Patch, long>();
            population[Patch] = 1000;

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
