using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AutoEvo;

public class OsmoregulationEfficiencyPressure : SelectionPressure
{
    public Patch Patch;

    public OsmoregulationEfficiencyPressure(Patch patch) : base(true,
        1.0f,
        new List<IMutationStrategy<MicrobeSpecies>> {
            new AddOrganelleAnywhere(SimulationParameters.Instance.GetOrganelleType("chemoSynthesizingProteins"))
        },
        new List<IMutationStrategy<EarlyMulticellularSpecies>>()
        )
    {
        Patch = patch;
    }

    public override float Score(Species species, SimulationCache cache)
    {
        return -cache.GetEnergyBalanceForSpecies((MicrobeSpecies)species, Patch.Biome).TotalConsumptionStationary;
    }
}
