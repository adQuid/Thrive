using System.Collections.Generic;
using AutoEvo;

public abstract class SelectionPressure
{
    /// <summary>
    ///   A species will be eliminated by another if it is outcompeted in every exclusive pressure by another species
    /// </summary>
    public bool Exclusive;

    public float Strength;
    public List<IMutationStrategy<MicrobeSpecies>> MicrobeMutations;
    public List<IMutationStrategy<EarlyMulticellularSpecies>> MulticellularMutations;

    public static List<SelectionPressure> PressuresFromPatch(Species? species, Patch patch, SimulationCache cache, List<SelectionPressure>? niche)
    {
        // Add Selection pressures
        // TODO: move this somewhere else
        var selectionPressures = new List<SelectionPressure>();

        if (niche == null){
            selectionPressures.Add(new AutotrophEnergyEfficiencyPressure(patch, Compound.ByName("hydrogensulfide"), 10.0f));

            selectionPressures.AddRange(PredationPressures(patch, cache));
        }
        else
        {
            selectionPressures.AddRange(niche);
        }

        selectionPressures.Add(new OsmoregulationEfficiencyPressure(patch, 5.0f));

        return selectionPressures;
    }

    public static List<Miche> MichesForPatch(Patch patch, SimulationCache cache)
    {
        var retval = new List<Miche>();

        // Predation
        foreach (var pressure in PredationPressures(patch, cache))
        {
            retval.Add(new Miche(pressure.Name(), pressure, new List<Miche> { new Miche("and don't die", new MetabolicStabilityPressure(patch, 10.0f), null)}));
        }

        // Hydrogen Sulfide
        if (patch.GetCompoundAmount("hydrogensulfide") > 0)
        {
            retval.Add(
                new Miche("Hydrogen Sulfide Chemosynthesis", new AutotrophEnergyEfficiencyPressure(patch, Compound.ByName("hydrogensulfide"), 1.0f),
                    new List<Miche> { new Miche("Mobile Hydrogen Sulfide Chemosynthesis", new ReachCompoundCloudPressure(10.0f), 
                        new List<Miche> { new Miche("and don't die", new MetabolicStabilityPressure(patch, 10.0f), null) }) 
                    })
            );
        }

        if (patch.GetCompoundAmount("glucose") > 0)
        {
            retval.Add(new Miche("Glucose Consumption", new ReachCompoundCloudPressure(5.0f),
                new List<Miche> { new Miche("and don't die", new MetabolicStabilityPressure(patch, 10.0f), null) }
            ));
        }

        return retval;
    }

    public static List<SelectionPressure> PredationPressures(Patch patch, SimulationCache cache)
    {
        var retval = new List<SelectionPressure>();

        foreach (var possiblePrey in patch.SpeciesInPatch.Keys)
        {
            retval.Add(new PredationEffectivenessPressure(possiblePrey, 10.0f));
        }

        return retval;
    }

    public SelectionPressure(bool exclusive, float strength, List<IMutationStrategy<MicrobeSpecies>> microbeMutations, List<IMutationStrategy<EarlyMulticellularSpecies>> multicellularMutations)
    {
        Exclusive = exclusive;
        Strength = strength;
        MicrobeMutations = microbeMutations;
        MulticellularMutations = multicellularMutations;
    }

    public abstract float Score(Species species, SimulationCache cache);

    public abstract string Name();
}
