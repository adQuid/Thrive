using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AutoEvo;

public class PredationEffectivenessPressure : SelectionPressure
{
    Species prey;

    public PredationEffectivenessPressure(Species prey, float weight): base(true,
        weight,
        new List<IMutationStrategy<MicrobeSpecies>> {
            new AddOrganelleAnywhere(SimulationParameters.Instance.GetOrganelleType("cytoplasm"))
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

        if (species.Epithet == prey.Epithet)
        {
            return 0.0f;
        }

        var microbeSpecies = (MicrobeSpecies)species;
        var microbePrey = (MicrobeSpecies)prey;

        return microbeSpecies.Organelles.Select(x => x.Definition == SimulationParameters.Instance.GetOrganelleType("cytoplasm")).Count() / microbeSpecies.BaseHexSize; /// MicrobeInternalCalculations.OsmoregulationCost(microbeSpecies.Organelles.Select(x => x.Definition), microbeSpecies.MembraneType);
    }
}