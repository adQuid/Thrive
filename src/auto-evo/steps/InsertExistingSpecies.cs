using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AutoEvo;
using Godot;

class InsertExistingSpecies : IRunStep
{
    public int TotalSteps => 1;

    public bool CanRunConcurrently => false;

    public Patch Patch;

    public InsertExistingSpecies(Patch patch)
    {
        Patch = patch;
    }

    public bool RunStep(RunResults results)
    {
        foreach (var species in Patch.SpeciesInPatch.Keys)
        {
            Patch.Miche.InsertSpecies(species);
        }

        return true;
    }
}