using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AutoEvo;

class PullSpeciesForPatch : IRunStep
{
    public Patch Patch;
    public SimulationCache Cache;

    public int TotalSteps => 1;

    public bool CanRunConcurrently => false;

    public PullSpeciesForPatch(Patch patch, SimulationCache cache)
    {
        Patch = patch;
        Cache = cache;
    }

    public bool RunStep(RunResults results)
    {
        foreach (var species in Patch.SpeciesInPatch.Keys)
        {
            var variants = ModifyExistingSpecies.ViableVariants(species, Patch, Cache, null);

            if (variants.Count > 0)
            {
                results.AddNewSpecies(
                    variants.First(),
                    new[]
                    {
                    new KeyValuePair<Patch, long>(Patch, 100),
                    },
                    RunResults.NewSpeciesType.FillNiche,
                    species
                );
            }
        }

        return true;
    }
}
