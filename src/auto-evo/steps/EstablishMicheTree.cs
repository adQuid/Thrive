using System.Collections;
using System.Collections.Generic;
using System.Linq;
using AutoEvo;

class EstablishMicheTree : IRunStep
{
    public Patch Patch;
    public SimulationCache Cache;
    public bool PlayerPatch;

    public int TotalSteps => 1;

    public bool CanRunConcurrently => false;

    public EstablishMicheTree(Patch patch, SimulationCache cache, bool playerPatch)
    {
        Patch = patch;
        Cache = cache;
        PlayerPatch = playerPatch;
    }

    public bool RunStep(RunResults results)
    {
        results.MicheByPatch[Patch] = PopulateMiche(results, Patch.Miche.AllOccupants().ToList());

        return true;
    }

    /*
     * Creates a root level miche appropriate for the patch
     */
    private Miche PopulateMiche(RunResults results, List<Species> candidates)
    {
        results.MicheByPatch[Patch] = Miche.RootMiche();

        if (PlayerPatch)
        {
            results.MicheByPatch[Patch].AddChild(new Miche("Be the player", new BePlayerSelectionPressure(1.0f)));
        }

        results.MicheByPatch[Patch].AddChildren(SelectionPressure.AutotrophicMichesForPatch(Patch, Cache));

        return results.MicheByPatch[Patch];
    }
}
