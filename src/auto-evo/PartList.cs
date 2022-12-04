using System.Collections.Generic;

public class PartList
{
    public Dictionary<string, OrganelleDefinition> PermittedOrganelleDefinitions;

    public PartList(Species species)
    {
        foreach (var organelleDefinition in SimulationParameters.Instance.GetAllOrganelles())
        {
            if(species is MicrobeSpecies)
            {
                var microbeSpecies = (MicrobeSpecies)species;

                if (organelleDefinition.RequiresNucleus && microbeSpecies.IsBacteria)
                {
                    continue;
                }
            }
            //TODO: make this only happen sometiems
            PermittedOrganelleDefinitions.Add(organelleDefinition.Name, organelleDefinition);
        }

    }

    public OrganelleDefinition? GetOrganelleType(string name)
    {
        if (PermittedOrganelleDefinitions.ContainsKey(name))
        {
            return PermittedOrganelleDefinitions[name];
        }

        return null;
    }

    public IEnumerable<OrganelleDefinition> GetAllOrganelles()
    {
        return PermittedOrganelleDefinitions.Values;
    }
}