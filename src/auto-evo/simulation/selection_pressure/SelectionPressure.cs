using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AutoEvo;

abstract class SelectionPressure
{
    public float Strength;
    public List<IMutationStrategy<MicrobeSpecies>> MicrobeMutations;
    public List<IMutationStrategy<EarlyMulticellularSpecies>> MulticellularMutations;

    public SelectionPressure(float Strength, List<IMutationStrategy<MicrobeSpecies>> MicrobeMutations, List<IMutationStrategy<EarlyMulticellularSpecies>> MulticellularMutations)
    {
        this.Strength = Strength;
        this.MicrobeMutations = MicrobeMutations;
        this.MulticellularMutations = MulticellularMutations;
    }

    public abstract float Score(Species species, SimulationCache cache);
}
