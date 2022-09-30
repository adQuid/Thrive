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

        var microbeSpecies = (MicrobeSpecies)species;
        var microbePrey = (MicrobeSpecies)prey;

        return microbeSpecies.BaseHexSize;
    }
}