using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AutoEvo;

class ModifyExistingSpecies : IRunStep
{
    public Species Species;
    public Patch Patch;

    public ModifyExistingSpecies(Species species, Patch patch)
    {
        Species = species;
        Patch = patch;
    }

    public int TotalSteps => 1;

    public bool CanRunConcurrently => true;

    public bool RunStep(RunResults results)
    {
        var variantSpecies = (MicrobeSpecies)Species.Clone();

        var selection1 = new AutotrophEnergyEfficiencyPressure(Patch);
        var selection2 = new OsmoregulationEfficiencyPressure(Patch);

        //var strategy = new AddOrganelleAnywhere(SimulationParameters.Instance.GetOrganelleType("chemoSynthesizingProteins"));

        variantSpecies = selection1.MicrobeMutations.First().MutationsOf(variantSpecies).First();
        variantSpecies = selection2.MicrobeMutations.First().MutationsOf(variantSpecies).First();

        results.AddMutationResultForSpecies(Species, variantSpecies);
        return true;
    }
}
