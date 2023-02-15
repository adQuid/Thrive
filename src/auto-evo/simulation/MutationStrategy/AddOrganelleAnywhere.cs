using System;
using System.Collections.Generic;
using System.Linq;
using Godot;

public class AddOrganelleAnywhere : IMutationStrategy<MicrobeSpecies>
{
    public enum Direction { FRONT, NEUTRAL, REAR}

    public Func<OrganelleDefinition, bool> Criteria;
    private Direction direction;

    public AddOrganelleAnywhere(Func<OrganelleDefinition, bool> criteria, Direction direction = Direction.NEUTRAL)
    {
        Criteria = criteria;
        this.direction = direction;
    }

    public static AddOrganelleAnywhere ThatUseCompound(Compound compound, Direction direction = Direction.NEUTRAL)
    {
        return new AddOrganelleAnywhere(organelle => organelle.RunnableProcesses.Where(proc => proc.Process.Inputs.ContainsKey(compound)).Count() > 0, direction);
    }

    public static AddOrganelleAnywhere ThatCreateCompound(Compound compound, Direction direction = Direction.NEUTRAL)
    {
        var matches = SimulationParameters.Instance.GetAllOrganelles().Where(organelle => organelle.RunnableProcesses.Where(proc => proc.Process.Outputs.ContainsKey(compound)).Count() > 0).Count();

        return new AddOrganelleAnywhere(organelle => organelle.RunnableProcesses.Where(proc => proc.Process.Outputs.ContainsKey(compound)).Count() > 0, direction);
    }

    public List<MicrobeSpecies> MutationsOf(MicrobeSpecies baseSpecies, MutationLibrary partList)
    {
        var viableOrganelles = partList.GetAllOrganelles().Where(x => Criteria(x));

        var retval = new List<MicrobeSpecies>();

        foreach (var organelle in viableOrganelles)
        {
            var newSpecies = (MicrobeSpecies)baseSpecies.Clone();

            OrganelleTemplate position = null;

            if (direction == Direction.NEUTRAL)
            {
                position = CommonMutationFunctions.GetRealisticPosition(organelle, newSpecies.Organelles, new Random());
            }
            else if (direction == Direction.FRONT)
            {
                // TODO: Just get a random of the top
                position = CommonMutationFunctions.ValidOrganellePositions(organelle, newSpecies.Organelles, new Random()).OrderBy(x => x.Position.R).First();
            }
            else
            {
                // TODO: Just get a random of the bottom
                position = CommonMutationFunctions.ValidOrganellePositions(organelle, newSpecies.Organelles, new Random()).OrderBy(x => x.Position.R).Last();
            }

            newSpecies.Organelles.Add(position);

            retval.Add(newSpecies);
        }

        return retval;
    }
}
