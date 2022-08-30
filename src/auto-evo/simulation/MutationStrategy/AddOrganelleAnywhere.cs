using System.Collections.Generic;

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

        newSpecies.Organelles.Add(CommonMutationFunctions.GetRealisticPosition(Organelle, newSpecies.Organelles));

        return new List<MicrobeSpecies> { newSpecies };
    }
}
