using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AutoEvo;
using Godot;

public class PredationEffectivenessPressure : SelectionPressure
{
    Species prey;

    public PredationEffectivenessPressure(Species prey, float weight): base(true,
        weight,
        new List<IMutationStrategy<MicrobeSpecies>> {
            new AddOrganelleAnywhere(SimulationParameters.Instance.GetOrganelleType("cytoplasm")),
            new RemoveAnyOrganelle(),
            new LowerRigidity(),
        },
        new List<IMutationStrategy<EarlyMulticellularSpecies>>()
        )
    {
        this.prey = prey;
    }

    public override string Name()
    {
        return "Predation Effectiveness against " + prey;
    }

    public override float Score(Species species, SimulationCache cache)
    {
        if (!(species is MicrobeSpecies || prey is MicrobeSpecies))
        {
            throw new NotImplementedException();
        }

        // Very lazy way of preventing canibalism
        if (species.Epithet == prey.Epithet)
        {
            return 0.0f;
        }

        var microbeSpecies = (MicrobeSpecies)species;
        var microbePrey = (MicrobeSpecies)prey;

        var predatorScore = PredationScore(microbeSpecies, microbePrey);
        var reversePredatorScore = PredationScore(microbePrey, microbeSpecies);

        // Explicitly prohibit circular predation relationships
        if (reversePredatorScore > predatorScore)
        {
            return 0.0f;
        }

        return predatorScore;
    }

    private float PredationScore(MicrobeSpecies predator, MicrobeSpecies prey)
    {
        return EngulfmentScore(predator, prey);
    }

    private float EngulfmentScore(MicrobeSpecies predator, MicrobeSpecies prey)
    {
        if (predator.BaseHexSize / prey.BaseHexSize < Constants.ENGULF_SIZE_RATIO_REQ)
        {
            return 0.0f;
        }

        // TEMPORARY logic for auto-evo testing
        var SizeScore = predator.Organelles.Select(x => x.Definition == SimulationParameters.Instance.GetOrganelleType("cytoplasm")).Count() / Mathf.Sqrt(predator.BaseHexSize);

        if (predator.BaseSpeed > prey.BaseSpeed)
        {
            return SizeScore;
        }
        else
        {
            return SizeScore * Constants.AUTO_EVO_ENGULF_LUCKY_CATCH_PROBABILITY;
        }
    }
}