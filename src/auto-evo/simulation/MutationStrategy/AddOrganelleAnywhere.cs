using System;
using System.Collections.Generic;
using Godot;

public class AddOrganelleAnywhere : IMutationStrategy<MicrobeSpecies>
{
    public OrganelleDefinition Organelle;

    public AddOrganelleAnywhere(OrganelleDefinition organelle)
    {
        Organelle = organelle;
    }

    public List<MicrobeSpecies> MutationsOf(MicrobeSpecies baseSpecies)
    {
        var newSpecies = (MicrobeSpecies)baseSpecies.Clone();

        var position = CommonMutationFunctions.GetRealisticPosition(Organelle, newSpecies.Organelles);

        newSpecies.Organelles.Add(position);

        return new List<MicrobeSpecies> { newSpecies };
    }

    public static List<IMutationStrategy<MicrobeSpecies>> ForOrganellesMatching(Func<OrganelleDefinition, bool> criteria)
    {
        var retval = new List<IMutationStrategy<MicrobeSpecies>>();

        foreach (var organelle in SimulationParameters.Instance.GetAllOrganelles())
        {
            if (criteria(organelle))
            {
                retval.Add( new AddOrganelleAnywhere(organelle));
            }
        }

        return retval;
    }
}
