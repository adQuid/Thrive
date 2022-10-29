using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AutoEvo;
using Godot;

class ComputeBestPressures : IRunStep
{
    public int TotalSteps => 1;

    public bool CanRunConcurrently => true;

    public Patch Patch;
    public SimulationCache Cache;

    public ComputeBestPressures(Patch patch, SimulationCache cache)
    {
        Patch = patch;
        Cache = cache;
    }

    public bool RunStep(RunResults results)
    {
        var bestPressures = CommonSelectionFunctions.GetBestPressures(results, Patch, Cache);

        foreach (var species in results.results.Keys)
        {
            results.results[species].BestPressures[Patch] = bestPressures.Where(x => x.Value == species).Select(x => x.Key).ToList();
        }

        AutoEvoGlobals.RunResults = results;
        return true;
    }
}