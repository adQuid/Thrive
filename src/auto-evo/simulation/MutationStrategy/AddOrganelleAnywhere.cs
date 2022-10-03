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

        GD.Print("add organelle at "+position.Position.Q + ", "+ position.Position.R);

        newSpecies.Organelles.Add(position);

        return new List<MicrobeSpecies> { newSpecies };
    }
}
