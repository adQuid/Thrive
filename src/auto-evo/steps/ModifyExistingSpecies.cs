using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AutoEvo;

class ModifyExistingSpecies : IRunStep
{
    public Species Species;

    public ModifyExistingSpecies(Species species)
    {
        Species = species;
    }

    public int TotalSteps => 1;

    public bool CanRunConcurrently => true;

    public bool RunStep(RunResults results)
    {
        var variantSpecies = (MicrobeSpecies)Species.Clone();

        var strategy = new AddOrganelleAnywhere(SimulationParameters.Instance.GetOrganelleType("chemoSynthesizingProteins"));

        variantSpecies = strategy.MutationsOf(variantSpecies).First();

        results.AddMutationResultForSpecies(Species, variantSpecies);
        return true;
    }
}
