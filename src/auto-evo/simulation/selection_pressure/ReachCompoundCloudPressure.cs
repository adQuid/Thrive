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
        EnergyProvided = 2000;
    }

    public override string Name()
    {
        return "Reach Compound Cloud";
    }

    public override float Score(Species species, SimulationCache cache)
    {
        // TODO: Add logic for multicel species (do I really need to?)
        if (!(species is MicrobeSpecies))
        {
            return 0.0f;
        }

        var microbeSpecies = (MicrobeSpecies)species;

        return microbeSpecies.BaseSpeed;
    }
}