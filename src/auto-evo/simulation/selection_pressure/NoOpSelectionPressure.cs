using System.Collections.Generic;
using AutoEvo;

public class NoOpSelectionPressure : SelectionPressure
{
    public NoOpSelectionPressure() : base(
        false,
        1.0f,
        new List<IMutationStrategy<MicrobeSpecies>>(),
        new List<IMutationStrategy<EarlyMulticellularSpecies>>()
        )
    {

    }

    public override string Name()
    {
        return "No-op";
    }

    public override float Score(Species species, SimulationCache cache)
    {
        return 1.0f;
    }
}