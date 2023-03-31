using System.Collections.Generic;
using AutoEvo;
using Godot;
using System.Linq;

class MetabolicStabilityPressure : SelectionPressure
{
    private Patch Patch;

    private AutotrophEnergyEfficiencyPressure SunPressure;

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
        SunPressure = new AutotrophEnergyEfficiencyPressure(patch, Compound.ByName("sunlight"), 1.0f);
    }

    public override string Name()
    {
        return "Metabolic Stability";
    }

    public override float Score(Species species, SimulationCache cache)
    {
        if (species is MicrobeSpecies)
        {
            if (((MicrobeSpecies)species).BaseSpeed == 0
                && SunPressure.Score(species, cache) < 0.1f)
            {
                return 0.0f;
            }

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
