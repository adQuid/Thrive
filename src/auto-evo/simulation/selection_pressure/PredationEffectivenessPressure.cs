using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AutoEvo;
using Godot;

public class PredationEffectivenessPressure : SelectionPressure
{
    public Species Prey;

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
        this.Prey = prey;
        EnergyProvided = 1000;
    }

    public override string Name()
    {
        return "Predation Effectiveness against " + Prey;
    }

    public override float Score(Species species, SimulationCache cache)
    {
        if (!(species is MicrobeSpecies || Prey is MicrobeSpecies))
        {
            throw new NotImplementedException();
        }

        // Very lazy way of preventing canibalism
        if (species.Epithet == Prey.Epithet)
        {
            return 0.0f;
        }

        var microbeSpecies = (MicrobeSpecies)species;
        var microbePrey = (MicrobeSpecies)Prey;

        var predatorScore = PredationScore(microbeSpecies, microbePrey);
        var reversePredatorScore = PredationScore(microbePrey, microbeSpecies);

        // Explicitly prohibit circular predation relationships
        if (reversePredatorScore > predatorScore)
        {
            return 0.0f;
        }

        return predatorScore;
    }

    public static float PredationScore(MicrobeSpecies predator, MicrobeSpecies prey)
    {
        return Math.Max(EngulfmentScore(predator, prey), ToxinScore(predator, prey) + PilusScore(predator, prey));
    }

    private static float EngulfmentScore(MicrobeSpecies predator, MicrobeSpecies prey)
    {
        if (predator.BaseHexSize / prey.BaseHexSize < Constants.ENGULF_SIZE_RATIO_REQ)
        {
            return 0.0f;
        }

        // TEMPORARY logic for auto-evo testing
        var SizeScore = predator.Organelles.Count();

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

    private static float PilusScore(MicrobeSpecies predator, MicrobeSpecies prey)
    {
        if (prey.BaseSpeed > 0)
        {
            return 0.0f;
        }

        foreach (var organelle in predator.Organelles)
        {
            if (organelle.Definition.HasComponentFactory<PilusComponentFactory>())
            {
                return prey.BaseHexSize;
            }
        }

        return 0.0f;
    }

    private static float ToxinScore(MicrobeSpecies predator, MicrobeSpecies prey)
    {
        // If the species wouldn't even attack this prey, no need to check further
        if (!(MicrobeAIFunctions.WouldTryToToxinHuntBiggerPrey(predator.Behaviour.Opportunism) || MicrobeAIFunctions.CouldEngulf(predator.BaseHexSize, prey.BaseHexSize)))
        {
            return 0.0f;
        }

        var predatorToxinStorage = predator.Organelles.Sum(x => x.Definition.Storage());

        var toxinStorageScore = Math.Min(1.0, predatorToxinStorage  * Constants.OXYTOXY_DAMAGE 
            / (prey.MembraneType.ToxinResistance * prey.MaxHealth()));

        var toxinProductionScore = 0.0f;
        foreach (var organelle in predator.Organelles)
        {
            foreach (var process in organelle.Definition.RunnableProcesses)
            {
                if (process.Process.Outputs.TryGetValue(Compound.ByName("oxytoxy"), out var oxytoxyAmount))
                {
                    toxinProductionScore += oxytoxyAmount;
                }
            }
        }

        var speedScore = predator.BaseSpeed > prey.BaseSpeed || 
            prey.MaxHealth() <= (Math.Min(predatorToxinStorage, Constants.MAXIMUM_AGENT_EMISSION_AMOUNT) * Constants.OXYTOXY_DAMAGE / prey.MembraneType.ToxinResistance)
            ? 1.0f : 0.5f;

        return ((float)toxinStorageScore * 100) * toxinProductionScore * speedScore;
    }
}