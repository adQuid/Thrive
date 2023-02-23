using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AutoEvo;

public class AutotrophEnergyEfficiencyPressure : SelectionPressure
{
    public Patch Patch;
    public Compound Compound;

    public AutotrophEnergyEfficiencyPressure(Patch patch, Compound compound, float weight): base(true, 
        weight,
        FromCompound(compound),
        new List<IMutationStrategy<EarlyMulticellularSpecies>>()
        )
    {
        Patch = patch;
        Compound = compound;
        EnergyProvided = 40000;
    }

    public override float Score(Species species, SimulationCache cache)
    {
        // it's important to keep this ordered since this is spawning order
        List <ICellProperties> cells = new();

        if (species is MicrobeSpecies)
        {
            cells.Add((MicrobeSpecies)species);
        }
        else
        {
            cells.AddRange(((EarlyMulticellularSpecies)species).CellTypes);
        }

        return cells.Sum(cell => CommonSelectionFunctions.EnergyGenerationScore(cell, Compound, Patch, cache)) /
            cells.Sum(cell => CommonSelectionFunctions.SpeciesOsmoregulationCost(cell));
    }

    public override string Name()
    {
        return "Autotroph Energy Efficiency from " + Compound.Name;
    }

    private static List<IMutationStrategy<MicrobeSpecies>> FromCompound(Compound compound)
    {
        List<IMutationStrategy<MicrobeSpecies>> retval = new List<IMutationStrategy<MicrobeSpecies>>();
        retval.Add(AddOrganelleAnywhere.ThatUseCompound(compound));
        retval.Add(new ChangeMembraneType(SimulationParameters.Instance.GetMembrane("cellulose")));
        return retval;
    }
}
