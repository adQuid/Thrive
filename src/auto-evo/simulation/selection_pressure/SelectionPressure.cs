using System.Collections.Generic;
using AutoEvo;
using Godot;

public abstract class SelectionPressure
{
    public float Strength;
    public List<IMutationStrategy<MicrobeSpecies>> MicrobeMutations;
    public List<IMutationStrategy<EarlyMulticellularSpecies>> MulticellularMutations;
    public int EnergyProvided = 0;

    public SelectionPressure(bool exclusive, float strength, List<IMutationStrategy<MicrobeSpecies>> microbeMutations, List<IMutationStrategy<EarlyMulticellularSpecies>> multicellularMutations)
    {
        Strength = strength;
        MicrobeMutations = microbeMutations;
        MulticellularMutations = multicellularMutations;
    }

    public abstract float Score(Species species, SimulationCache cache);

    public abstract string Name();

    /// <summary>
    ///   
    /// </summary>
    /// <param name="species1"></param>
    /// <param name="species2"></param>
    /// <returns></returns>
    public float WeightedComparedScores(Species species1, Species species2, SimulationCache cache)
    {
        var score1 = Score(species1, cache);
        var score2 = Score(species2, cache);

        if (score1 == 0)
        {
            return Miche.INVIABLE_PRESSURE_RESULT;
        }

        if (score2 == 0)
        {
            return score1 > 0 ? 1 : 0;
        }

        return (score1 / score2 - 1) * Strength;
    }
}
