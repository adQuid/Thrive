using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AutoEvo;

public class AutotrophEnergyEfficiencyPressure : SelectionPressure
{
    public Patch Patch;

    public AutotrophEnergyEfficiencyPressure(Patch patch, float weight): base(true, 
        weight, 
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
        return CommonSelectionFunctions.EnergyGenerationScore((MicrobeSpecies)species, Compound.ByName("hydrogensulfide"), Patch, cache) / 
            CommonSelectionFunctions.SpeciesOsmoregulationCost((MicrobeSpecies)species);
    }

    public override string Name()
    {
        return "Autotroph Energy Efficiency";
    }
}
