
using System.Collections.Generic;
using AutoEvo;

class MicheFactory
{
    public static List<SelectionPressure> PressuresFromPatch(Species? species, Patch patch, MutationLibrary partList, SimulationCache cache, List<SelectionPressure> niche)
    {
        // Add Selection pressures
        // TODO: move this somewhere else
        var selectionPressures = new List<SelectionPressure>();

        selectionPressures.AddRange(niche);

        selectionPressures.Add(new OsmoregulationEfficiencyPressure(patch, 0.5f));

        return selectionPressures;
    }

    public static List<Miche> AutotrophicMichesForPatch(Patch patch, SimulationCache cache)
    {
        var retval = new List<Miche>();

        // Glucose
        if (patch.GetCompoundAmount("glucose") > 0)
        {
            retval.Add(new Miche("Glucose Consumption", new ReachCompoundCloudPressure(5.0f),
                new List<Miche> { new Miche("and don't die", new MetabolicStabilityPressure(patch, 1.0f)) }
            ));
        }

        // Hydrogen Sulfide
        if (patch.GetCompoundAmount("hydrogensulfide") > 0)
        {
            retval.Add(
                new Miche("Hydrogen Sulfide Chemosynthesis", new AutotrophEnergyEfficiencyPressure(patch, Compound.ByName("hydrogensulfide"), 10.0f),
                    new List<Miche> { new Miche("Mobile Hydrogen Sulfide Chemosynthesis", new ReachCompoundCloudPressure(1.0f),
                        new List<Miche> { new Miche("and don't die", new MetabolicStabilityPressure(patch, 1.0f)) })
                    })
            );
        }

        // Sunlight
        if (patch.GetCompoundAmount("sunlight") > 0)
        {
            retval.Add(
                new Miche("Photosynthesis", new AutotrophEnergyEfficiencyPressure(patch, Compound.ByName("sunlight"), 1.0f),
                    new List<Miche> {
                        new Miche("and store energy for night", new StoreGlucosePressure(50.8f), new List<Miche>{ new Miche("and don't die", new MetabolicStabilityPressure(patch, 1.0f)) }),
                        new Miche("and don't die", new MetabolicStabilityPressure(patch, 1.0f))
                    })
            );
        }

        // Iron
        if (patch.GetCompoundAmount("iron") > 0)
        {
            retval.Add(
                new Miche("Iron Chemosynthesis", new AutotrophEnergyEfficiencyPressure(patch, Compound.ByName("iron"), 1.0f),
                    new List<Miche> { new Miche("Mobile Iron Chemosynthesis", new ReachCompoundCloudPressure(1.0f),
                        new List<Miche> { new Miche("and don't die", new MetabolicStabilityPressure(patch, 1.0f)) })
                    })
            );
        }

        return retval;
    }

    public static List<Miche> PredationMiches(Patch patch, HashSet<Species> prey, SimulationCache cache)
    {
        var retval = new List<Miche>();

        foreach (var possiblePrey in prey)
        {
            var pressure = new PredationEffectivenessPressure(possiblePrey, 10.0f);
            retval.Add(new Miche(pressure.Name(), pressure, new List<Miche> { new Miche("and don't die", new MetabolicStabilityPressure(patch, 1.0f)) }));
        }

        return retval;
    }
}
