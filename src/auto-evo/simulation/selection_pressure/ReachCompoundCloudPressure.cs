using System.Collections.Generic;
using AutoEvo;

public class ReachCompoundCloudPressure : SelectionPressure
{
    public ReachCompoundCloudPressure(float weight) : base(false,
        weight,
        new List<IMutationStrategy<MicrobeSpecies>> {
            new LowerRigidity(),
            new ChangeMembraneType(SimulationParameters.Instance.GetMembrane("single")),
        },
        new List<IMutationStrategy<EarlyMulticellularSpecies>>()
        )
    {

    }

    public override string Name()
    {
        return "Reach Compound Cloud";
    }

    public override float Score(Species species, SimulationCache cache)
    {
        var microbeSpecies = (MicrobeSpecies)species;

        return microbeSpecies.BaseSpeed;
    }
}