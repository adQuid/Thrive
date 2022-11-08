using System.Collections.Generic;
using AutoEvo;

class MetabolicStabilityPressure : SelectionPressure
{
    private Patch Patch;

    public MetabolicStabilityPressure(Patch patch, float weight) : base(false,
        weight,
        new List<IMutationStrategy<MicrobeSpecies>>
        {
            new AddOrganelleAnywhere(SimulationParameters.Instance.GetOrganelleType("metabolosome"))
        },
        new List<IMutationStrategy<EarlyMulticellularSpecies>>()
        )
    {
        Patch = patch;
    }

    public override string Name()
    {
        return "Metabolic Stability";
    }

    public override float Score(Species species, SimulationCache cache)
    {
        var microbeSpecies = (MicrobeSpecies)species;
        var energyBalance = MicrobeInternalCalculations.ComputeEnergyBalance(microbeSpecies.Organelles, Patch.Biome, microbeSpecies.MembraneType);

        if (energyBalance.FinalBalance > 0)
        {
            return 1.0f;
        } 
        else if (energyBalance.FinalBalanceStationary >= 0)
        {
            return 0.5f;
        }
        else
        {
            return 0.0f;
        }
    }
}
