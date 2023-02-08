using System;
using System.Collections.Generic;

public class MutationLibrary
{
    public Dictionary<string, OrganelleDefinition> PermittedOrganelleDefinitions = new Dictionary<string, OrganelleDefinition>();

    public MutationLibrary(Species species)
    {

        foreach (var organelleDefinition in SimulationParameters.Instance.GetAllOrganelles())
        {
            var shouldAdd = true;
            if (species is MicrobeSpecies)
            {
                var microbeSpecies = (MicrobeSpecies)species;

                if (organelleDefinition.RequiresNucleus && microbeSpecies.IsBacteria)
                {
                    shouldAdd = false;
                }
            }

            // TODO: Make this use a shared random and based on a property in the organelle definition
            if (new Random().NextDouble() < 0.6)
            {
                shouldAdd = false;
            }

            if (shouldAdd)
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