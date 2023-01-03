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
            GD.Print("Found a species");
            var population = new Dictionary<Patch, long>();
            population[Patch] = 1000;

            results.AddNewSpecies(speciesToAdd, population, RunResults.NewSpeciesType.FillNiche, speciesToAdd);
        }

        foreach (var species in Patch.SpeciesInPatch)
        {
            if (!results.MicheByPatch[Patch].AllOccupants().Contains(species.Key))
            {
                results.KillSpeciesInPatch(species.Key, Patch);
            }
        }

        return true;
    }
}
