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
        results.MicheByPatch[Patch] = PopulateMiche(results);

        return true;
    }

    /// <summary>
    ///   Modifies the miche of the RunResults provided with all primary miches (those not resulting from the presence of a species, since species are not populated at this time)
    /// </summary>
    /// <param name="results">results to modify</param>
    /// <returns></returns>
    private Miche PopulateMiche(RunResults results)
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
