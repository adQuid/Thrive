using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AutoEvo;
using Godot;

public class ModifyExistingSpecies : IRunStep
{
    public Species Species;
    public Patch Patch;
    public SimulationCache Cache;

    public ModifyExistingSpecies(Species species, Patch patch, SimulationCache cache)
    {
        Species = species;
        Patch = patch;
        Cache = cache;
    }

    public int TotalSteps => 1;

    public bool CanRunConcurrently => true;

    public bool RunStep(RunResults results)
    {
        var viableVariants = ViableVariants(Species, Patch, Cache);

        // TODO: pass this in
        var random = new Random();

        results.AddMutationResultForSpecies(Species, viableVariants.First());
        return true;
    }

    public static List<MicrobeSpecies> ViableVariants(Species species, Patch patch, SimulationCache cache)
    {
        var modifiedSpecies = (MicrobeSpecies)species.Clone();

        var selectionPressures = SelectionPressure.PressuresFromPatch(species, patch, cache, null);

        // find the initial scores
        var pressureScores = new Dictionary<SelectionPressure, float>();
        foreach (var curPressure in selectionPressures)
        {
            pressureScores[curPressure] = curPressure.Score(modifiedSpecies, cache);
        }

        var viableVariants = new List<MicrobeSpecies> { modifiedSpecies };

        foreach (var curPressure in selectionPressures)
        {
            var previousPressures = selectionPressures.IndexOf(curPressure) > 0 ? selectionPressures.GetRange(0, selectionPressures.IndexOf(curPressure) - 1) : new List<SelectionPressure>();
            previousPressures.Reverse();

            // For each viable variant, get a new variants that at least improve score a little bit
            var potentialVariants = viableVariants.Select(startVariant =>
                curPressure.MicrobeMutations.Select(mutationStrategy => mutationStrategy.MutationsOf(startVariant))
                .SelectMany(x => x)
                .Where(x => curPressure.Score(x, cache) >= curPressure.Score(startVariant, cache))
                )
                .SelectMany(x => x).ToList();

            // Prune variants that don't hurt the previous scores too much
            foreach (var potentialVariant in potentialVariants)
            {
                var currentImprovement = curPressure.Score(potentialVariant, cache) / pressureScores[curPressure];

                var viable = true;
                foreach (var pastPressure in previousPressures)
                {
                    var pastImprovement = pastPressure.Score(potentialVariant, cache) / pressureScores[pastPressure];

                    // If, proportional to weights, the improvement to the current pressure doesn't outweigh the loss to the other pressures, this is still viable.
                    if (currentImprovement * curPressure.Strength + pastImprovement * pastPressure.Strength < curPressure.Strength + pastPressure.Strength)
                    {
                        viable = false;
                    }
                }

                if (viable)
                {
                    viableVariants.Add(potentialVariant);
                }
            }
        }

        GD.Print(viableVariants.Count() + " viable variants");

        foreach (var variant in viableVariants)
        {
            variant.Epithet += (new Random()).Next(1, 10);
        }

        return viableVariants.OrderByDescending(x => selectionPressures.Select(pressure => (pressure.Score(x, cache) / pressureScores[pressure]) * pressure.Strength).Sum()).ToList();
    }
}
