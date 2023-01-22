using AutoEvo;
using System.Collections.Generic;

public class BePlayerSelectionPressure : SelectionPressure
{
    public BePlayerSelectionPressure(float weight) : base(true,
        weight,
        new List<IMutationStrategy<MicrobeSpecies>>(),
        new List<IMutationStrategy<EarlyMulticellularSpecies>>()
        )
    {
        EnergyProvided = 1000;
    }

    public override string Name()
    {
        return "Be the player";
    }

    public override float Score(Species species, SimulationCache cache)
    {
        return species.PlayerSpecies ? 1.0f : 0.0f;
    }
}