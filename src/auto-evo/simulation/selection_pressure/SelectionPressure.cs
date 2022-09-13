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
