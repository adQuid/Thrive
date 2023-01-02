using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AutoEvo;

class ExcludeSpecies : IRunStep
{
    public int TotalSteps => 1;

    public bool CanRunConcurrently => false;

    Patch Patch;
    public ExcludeSpecies(Patch patch)
    {
        Patch = patch;
    }

    public bool RunStep(RunResults results)
    {
        foreach (var species in Patch.SpeciesInPatch)
        {
            if (!results.Miches[Patch].AllOccupants().Contains(species.Key))
            {
                results.KillSpeciesInPatch(species.Key, Patch);
            }
        }

        return true;
    }
}
