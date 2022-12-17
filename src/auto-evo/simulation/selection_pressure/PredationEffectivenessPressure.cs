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
            new AddOrganelleAnywhere(organelle => organelle.MPCost < 30),
            AddOrganelleAnywhere.ThatCreateCompound(Compound.ByName("oxytoxy")),
            new ChangeBehaviorScore(ChangeBehaviorScore.BehaviorAttribute.OPPORTUNISM, 150.0f),
            new ChangeBehaviorScore(ChangeBehaviorScore.BehaviorAttribute.FEAR, -150.0f),
            new RemoveAnyOrganelle(),
            new LowerRigidity(),
            new ChangeMembraneType(SimulationParameters.Instance.GetMembrane("single")),
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
        return EngulfmentScore(predator, prey) + ToxinScore(predator, prey);
    }

    private float EngulfmentScore(MicrobeSpecies predator, MicrobeSpecies prey)
    {
        if (predator.BaseHexSize / prey.BaseHexSize < Constants.ENGULF_SIZE_RATIO_REQ)
        {
            return 0.0f;
        }

        // TEMPORARY logic for auto-evo testing
        var SizeScore = predator.Organelles.Select(x => x.Definition == SimulationParameters.Instance.GetOrganelleType("cytoplasm")).Count() / Mathf.Sqrt(predator.BaseHexSize);

        if (predator.BaseSpeed < 0.1f || predator.MembraneType.CellWall) 
        {
            return 0.0f;
        }
        else if (predator.BaseSpeed > prey.BaseSpeed)
        {
            return SizeScore;
        }
        else
        {
            return SizeScore * Constants.AUTO_EVO_ENGULF_LUCKY_CATCH_PROBABILITY;
        }
    }

    private float ToxinScore(MicrobeSpecies predator, MicrobeSpecies prey)
    {
        if (!(MicrobeAIFunctions.WouldTryToToxinHuntBiggerPrey(predator.Behaviour.Opportunism) || MicrobeAIFunctions.CouldEngulf(predator.BaseHexSize, prey.BaseHexSize)))
        {
            return 0.0f;
        }

        var toxinProductionScore = 0.0f;
        foreach (var organelle in predator.Organelles)
        {
            foreach (var process in organelle.Definition.RunnableProcesses)
            {
                if (process.Process.Outputs.TryGetValue(Compound.ByName("oxytoxy"), out var oxytoxyAmount))
                {
                    toxinProductionScore += oxytoxyAmount * Constants.AUTO_EVO_TOXIN_PREDATION_SCORE;
                }
            }
        }

        return toxinProductionScore / prey.MembraneType.ToxinResistance;
    }
}