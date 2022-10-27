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
    public List<SelectionPressure> Niche;

    public int TotalSteps => 1;

    public bool CanRunConcurrently => false;

    public PullSpeciesForPatch(Patch patch, SimulationCache cache, List<SelectionPressure> niche)
    {
        Patch = patch;
        Cache = cache;
        Niche = niche;
    }

    public bool RunStep(RunResults results)
    {
        foreach (var species in Patch.SpeciesInPatch.Keys)
        {
            var variants = ModifyExistingSpecies.ViableVariants(results, species, Patch, Cache, Niche);

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
