using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

public class MutationLogicFunctions
{
    public static void NameNewMicrobeSpecies(MicrobeSpecies newSpecies, MicrobeSpecies parentSpecies)
    {
        // If for some silly reason the species are the same don't rename
        if (newSpecies == parentSpecies)
        {
            return;
        }

        if (SpeciesIsNewGenus(newSpecies, parentSpecies))
        {
            newSpecies.Genus = SimulationParameters.Instance.NameGenerator.GenerateNameSection();
        }
        else
        {
            newSpecies.Genus = parentSpecies.Genus;
        }

        newSpecies.Epithet = SimulationParameters.Instance.NameGenerator.GenerateNameSection();
    }

    /// <summary>
    ///   Used to determine if a newly mutated species needs to be in a different genus.
    /// </summary>
    /// <param name="species1">The first species. Function is not order-dependent.</param>
    /// <param name="species2">The second species. Function is not order-dependent.</param>
    /// <returns>True if the two species should be a new genus, false otherwise.</returns>
    private static bool SpeciesIsNewGenus(MicrobeSpecies species1, MicrobeSpecies species2)
    {
        var species1UniqueOrganelles = species1.Organelles.Select(o => o.Definition).ToHashSet();
        var species2UniqueOrganelles = species2.Organelles.Select(o => o.Definition).ToHashSet();

        return species1UniqueOrganelles.Union(species2UniqueOrganelles).Count()
            - species1UniqueOrganelles.Intersect(species2UniqueOrganelles).Count()
            >= Constants.DIFFERENCES_FOR_GENUS_SPLIT;
    }
}
