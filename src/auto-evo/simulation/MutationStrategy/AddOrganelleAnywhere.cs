using System;
using System.Collections.Generic;
using System.Linq;
using Godot;

public class AddOrganelleAnywhere : IMutationStrategy<MicrobeSpecies>
{
    public Func<OrganelleDefinition, bool> Criteria;

    public AddOrganelleAnywhere(Func<OrganelleDefinition, bool> criteria)
    {
        Criteria = criteria;
    }

    public static AddOrganelleAnywhere ThatUseCompound(Compound compound)
    {
        return new AddOrganelleAnywhere(organelle => organelle.RunnableProcesses.Where(proc => proc.Process.Inputs.ContainsKey(compound)).Count() > 0);
    }

    public static AddOrganelleAnywhere ThatCreateCompound(Compound compound)
    {
        var matches = SimulationParameters.Instance.GetAllOrganelles().Where(organelle => organelle.RunnableProcesses.Where(proc => proc.Process.Outputs.ContainsKey(compound)).Count() > 0).Count();

        return new AddOrganelleAnywhere(organelle => organelle.RunnableProcesses.Where(proc => proc.Process.Outputs.ContainsKey(compound)).Count() > 0);
    }

    public List<MicrobeSpecies> MutationsOf(MicrobeSpecies baseSpecies, MutationLibrary partList)
    {
        var viableOrganelles = partList.GetAllOrganelles().Where(x => Criteria(x));

        var retval = new List<MicrobeSpecies>();

        foreach (var organelle in viableOrganelles)
        {
            var newSpecies = (MicrobeSpecies)baseSpecies.Clone();

            var position = CommonMutationFunctions.GetRealisticPosition(organelle, newSpecies.Organelles, new Random());

            newSpecies.Organelles.Add(position);

            retval.Add(newSpecies);
        }

        return retval;
    }
}
