using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AutoEvo;

class AvoidPredationSelectionPressure : SelectionPressure
{
    public Species Predator;

    public AvoidPredationSelectionPressure(Species predator, float weight): base(true, weight, 
        new List<IMutationStrategy<MicrobeSpecies>>
        {
            new AddOrganelleAnywhere(organelle => organelle.MPCost < 30),
            new LowerRigidity()
        },
        new List<IMutationStrategy<EarlyMulticellularSpecies>>()
        )
    {
        this.Predator = predator;
    }

    public override string Name()
    {
        return "Don't get eaten by "+Predator.FormattedName;
    }

    public override float Score(Species species, SimulationCache cache)
    {
        var predationScore = PredationEffectivenessPressure.PredationScore((MicrobeSpecies)Predator, (MicrobeSpecies)species);

        if(predationScore == 0)
        {
            return 1.0f;
        }

        return 1 / predationScore;
    }
}
