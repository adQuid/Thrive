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

        var selectionPressures = new List<SelectionPressure>
        {
            new OsmoregulationEfficiencyPressure(Patch, 10.0f),
            new AutotrophEnergyEfficiencyPressure(Patch, 0.8f),
        };

        foreach (var curPressure in selectionPressures)
        {
            variantSpecies = curPressure.MicrobeMutations.First().MutationsOf(variantSpecies).First();
        }

        results.AddMutationResultForSpecies(Species, variantSpecies);
        return true;
    }
}
