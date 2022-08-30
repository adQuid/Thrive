using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AutoEvo;

public abstract class SelectionPressure
{
    public float Strength;
    public List<IMutationStrategy<MicrobeSpecies>> MicrobeMutations;
    public List<IMutationStrategy<EarlyMulticellularSpecies>> MulticellularMutations;

    public SelectionPressure(float strength, List<IMutationStrategy<MicrobeSpecies>> microbeMutations, List<IMutationStrategy<EarlyMulticellularSpecies>> multicellularMutations)
    {
        Strength = strength;
        MicrobeMutations = microbeMutations;
        MulticellularMutations = multicellularMutations;
    }

    public abstract float Score(Species species, SimulationCache cache);
}
