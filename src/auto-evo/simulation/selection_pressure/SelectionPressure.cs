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
            selectionPressures.Add(new AutotrophEnergyEfficiencyPressure(patch, 10.0f));

            foreach (var possiblePrey in patch.SpeciesInPatch.Keys)
            {
                if (possiblePrey != species)
                {
                    selectionPressures.Add(new PredationEffectivenessPressure(possiblePrey, 10.0f));
                }
            }
        }
        else
        {
            selectionPressures.AddRange(niche);
        }

        selectionPressures.Add(new OsmoregulationEfficiencyPressure(patch, 5.0f));

        return selectionPressures;
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
