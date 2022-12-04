using System.Collections.Generic;
using AutoEvo;
using Godot;

class MetabolicStabilityPressure : SelectionPressure
{
    private Patch Patch;

    public MetabolicStabilityPressure(Patch patch, PartList partList, float weight) : base(true,
        weight,
        new List<IMutationStrategy<MicrobeSpecies>>
        {
            new AddOrganelleAnywhere(organelle => organelle.Name.Equals("metabolosome"))
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
            return 1.0f / energyBalance.TotalConsumptionStationary;
        } 
        else if (energyBalance.FinalBalanceStationary >= 0)
        {
            return 0.5f / energyBalance.TotalConsumptionStationary;
        }
        else
        {
            return 0.0f;
        }
    }
}
