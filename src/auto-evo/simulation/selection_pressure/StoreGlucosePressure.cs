using System;
using System.Collections.Generic;
using AutoEvo;
using System.Linq;

public class StoreGlucosePressure : SelectionPressure
{
    public StoreGlucosePressure(float weight): base(false, weight, 
        new List<IMutationStrategy<MicrobeSpecies>> {
            new AddOrganelleAnywhere(organelle => organelle.Storage() > 0.5f)
        },
        new List<IMutationStrategy<EarlyMulticellularSpecies>>()
    )
    {
    }

    public override string Name()
    {
        return "Store Glucose Pressure";
    }

    public override float Score(Species species, SimulationCache cache)
    {
        if (species is MicrobeSpecies)
        {
            return ((MicrobeSpecies)species).Organelles.Sum(x => x.Definition.Storage());
        }
        else
        {
            var multicellSpecies = (EarlyMulticellularSpecies)species;

            return multicellSpecies.Cells.Select(cell => cell.CellType).Sum(cell => cell.Organelles.Sum(x => x.Definition.Storage()));
        }

    }
}
