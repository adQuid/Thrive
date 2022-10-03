using System.Collections.Generic;
using Godot;

class AddOrganelleAnywhere : IMutationStrategy<MicrobeSpecies>
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
}
