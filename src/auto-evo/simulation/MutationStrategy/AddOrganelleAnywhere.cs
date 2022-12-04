using System;
using System.Collections.Generic;
using System.Linq;

public class AddOrganelleAnywhere : IMutationStrategy<MicrobeSpecies>
{
    public Func<OrganelleDefinition, bool> Criteria;

    public AddOrganelleAnywhere(Func<OrganelleDefinition, bool> criteria)
    {
        Criteria = criteria;
    }

    public List<MicrobeSpecies> MutationsOf(MicrobeSpecies baseSpecies, PartList partList)
    {
        var viableOrganelles = partList.GetAllOrganelles().Where(x => Criteria(x));

        var retval = new List<MicrobeSpecies>();

        foreach (var organelle in viableOrganelles)
        {
            var newSpecies = (MicrobeSpecies)baseSpecies.Clone();

            var position = CommonMutationFunctions.GetRealisticPosition(organelle, newSpecies.Organelles);

            newSpecies.Organelles.Add(position);

            retval.Add(newSpecies);
        }

        return retval;
    }
}
