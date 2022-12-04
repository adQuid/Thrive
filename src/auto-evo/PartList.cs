using System.Collections.Generic;

public class PartList
{
    public Dictionary<string, OrganelleDefinition> PermittedOrganelleDefinitions;

    public PartList(Species species)
    {
        foreach (var organelleDefinition in SimulationParameters.Instance.GetAllOrganelles())
        {
            var isValid = true;
            if (species is MicrobeSpecies)
            {
                var microbeSpecies = (MicrobeSpecies)species;

                if (organelleDefinition.RequiresNucleus && microbeSpecies.IsBacteria)
                {
                    isValid = false;
                }
            }
            
            // TODO: Add chance here

            if (isValid)
            {
                PermittedOrganelleDefinitions.Add(organelleDefinition.Name, organelleDefinition);
            }
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