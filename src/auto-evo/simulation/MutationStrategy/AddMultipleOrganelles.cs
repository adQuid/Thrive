using System.Collections.Generic;
using System.Linq;

class AddMultipleOrganelles : IMutationStrategy<MicrobeSpecies>
{
    List<AddOrganelleAnywhere> organelleAdditions;

    public AddMultipleOrganelles(List<AddOrganelleAnywhere> addOrganelleAnywheres)
    {
        organelleAdditions = addOrganelleAnywheres;
    }

    public List<MicrobeSpecies> MutationsOf(MicrobeSpecies baseSpecies, MutationLibrary partList)
    {
        List<MicrobeSpecies> retval = new List<MicrobeSpecies>();

        foreach(AddOrganelleAnywhere mutationStrat in organelleAdditions)
        {
            retval = retval.SelectMany(x => mutationStrat.MutationsOf(x, partList)).ToList();
        }

        return retval;
    }
}
