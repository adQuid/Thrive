using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

public class MutationLogicFunctions
{
    public static void NameNewMicrobeSpecies(Species newSpecies, Species parentSpecies)
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
    public static bool SpeciesIsNewGenus(Species species1, Species species2)
    {
        if (species1 is MicrobeSpecies)
        {
            if (species2 is MicrobeSpecies)
            {
                return MicrobeSpeciesIsNewGenus((MicrobeSpecies)species1, (MicrobeSpecies)species2);
            }
            else
            {
                return false;
            }
        }
        else
        {
            if (species2 is EarlyMulticellularSpecies)
            {
                return MulticellularSpeciesIsNewGenus((EarlyMulticellularSpecies)species1, (EarlyMulticellularSpecies)species2);
            }
            else
            {
                return false;
            }
        }
    }
    
    private static bool MulticellularSpeciesIsNewGenus(EarlyMulticellularSpecies species1, EarlyMulticellularSpecies species2)
    {
        // huh, this works...
        var species1UniqueCells = species1.Organelles.Select(o => o.Definition).ToHashSet();
        var species2UniqueCells = species2.Organelles.Select(o => o.Definition).ToHashSet();

        return species1UniqueCells.Union(species2UniqueCells).Count()
            - species1UniqueCells.Intersect(species2UniqueCells).Count()
            >= Constants.DIFFERENCES_FOR_GENUS_SPLIT;
    }
    
    private static bool MicrobeSpeciesIsNewGenus(MicrobeSpecies species1, MicrobeSpecies species2)
    {
        var species1UniqueOrganelles = species1.Organelles.Select(o => o.Definition).ToHashSet();
        var species2UniqueOrganelles = species2.Organelles.Select(o => o.Definition).ToHashSet();

        return species1UniqueOrganelles.Union(species2UniqueOrganelles).Count()
            - species1UniqueOrganelles.Intersect(species2UniqueOrganelles).Count()
            >= Constants.DIFFERENCES_FOR_GENUS_SPLIT;
    }
}
