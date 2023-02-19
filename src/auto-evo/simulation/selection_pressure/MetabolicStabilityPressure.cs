using System.Collections.Generic;
using AutoEvo;
using Godot;
using System.Linq;

class MetabolicStabilityPressure : SelectionPressure
{
    private Patch Patch;

    public MetabolicStabilityPressure(Patch patch, float weight) : base(true,
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
        if (species is MicrobeSpecies)
        {
            return ScoreByCell((MicrobeSpecies)species, cache);
        }
        else
        {
            return ((EarlyMulticellularSpecies)species).Cells.Sum(cell => ScoreByCell(cell, cache));
        }
    }

    private float ScoreByCell(ICellProperties cell, SimulationCache cache)
    {
        var energyBalance = MicrobeInternalCalculations.ComputeEnergyBalance(cell.Organelles, Patch.Biome, cell.MembraneType);

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
