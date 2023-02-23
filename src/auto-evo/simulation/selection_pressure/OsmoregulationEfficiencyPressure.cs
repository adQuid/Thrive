using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AutoEvo;

public class OsmoregulationEfficiencyPressure : SelectionPressure
{
    public Patch Patch;

    public OsmoregulationEfficiencyPressure(Patch patch, float weight) : base(false,
        weight,
        new List<IMutationStrategy<MicrobeSpecies>> {
            new RemoveAnyOrganelle(),
            new RemoveAnyOrganelle(),
            new RemoveAnyOrganelle(),
        },
        new List<IMutationStrategy<EarlyMulticellularSpecies>>()
        )
    {
        Patch = patch;
    }

    public override float Score(Species species, SimulationCache cache)
    {
        return 1 / cache.GetEnergyBalanceForSpecies(species, Patch.Biome).TotalConsumptionStationary;
    }

    public override string Name()
    {
        return "Osmoregulation Efficiency";
    }
}
