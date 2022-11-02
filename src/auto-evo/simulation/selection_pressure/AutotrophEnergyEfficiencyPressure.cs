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
    }

    public override float Score(Species species, SimulationCache cache)
    {
        return CommonSelectionFunctions.EnergyGenerationScore((MicrobeSpecies)species, Compound, Patch, cache) /
            CommonSelectionFunctions.SpeciesOsmoregulationCost((MicrobeSpecies)species);
    }

    public override string Name()
    {
        return "Autotroph Energy Efficiency";
    }

    private static List<IMutationStrategy<MicrobeSpecies>> FromCompound(Compound compound)
    {
        List<IMutationStrategy<MicrobeSpecies>> retval = AddOrganelleAnywhere.ForOrganellesMatching(organelle => organelle.RunnableProcesses.Where(proc => proc.Process.Inputs.ContainsKey(compound)).Count() > 0);
        retval.Add(new ChangeMembraneType(SimulationParameters.Instance.GetMembrane("cellulose")));
        return retval;
    }
}
