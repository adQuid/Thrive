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
    public PartList PartList;

    public ModifyExistingSpecies(Species species, Patch patch, SimulationCache cache)
    {
        Species = species;
        Patch = patch;
        Cache = cache;
        PartList = new PartList(species);
    }

    public int TotalSteps => 1;

    public bool CanRunConcurrently => true;

    public bool RunStep(RunResults results)
    {
        /*var viableVariants = ViableVariants(results, Species, Patch, PartList, Cache, null);

        // TODO: pass this in
        var random = new Random();

        results.AddMutationResultForSpecies(Species, viableVariants.First());*/
        return true;
    }

    public static List<MicrobeSpecies> ViableVariants(RunResults results, 
        Species species, 
        Patch patch, 
        PartList partList, 
        SimulationCache cache, 
        List<SelectionPressure>? niche)
    {
        var modifiedSpecies = (MicrobeSpecies)results.LastestVersionForSpecies(species);

        var selectionPressures = SelectionPressure.PressuresFromPatch(species, patch, partList, cache, niche);

        // find the initial scores
        var pressureScores = new Dictionary<SelectionPressure, float>();
        foreach (var curPressure in selectionPressures)
        {
            pressureScores[curPressure] = curPressure.Score(modifiedSpecies, cache);
        }

        var viableVariants = new List<MicrobeSpecies> { modifiedSpecies };

        foreach (var curPressure in selectionPressures)
        {
            // For each viable variant, get a new variants that at least improve score a little bit
            var potentialVariants = viableVariants.Select(startVariant =>
                curPressure.MicrobeMutations.Select(mutationStrategy => mutationStrategy.MutationsOf(startVariant, partList))
                .SelectMany(x => x)
                .Where(x => curPressure.Score(x, cache) >= curPressure.Score(startVariant, cache))
                )
                .SelectMany(x => x).ToList();

            // Prune variants that hurt the previous scores too much
            viableVariants = PruneInviableSpecies(potentialVariants, curPressure, selectionPressures, pressureScores, cache);
        }

        foreach (var variant in viableVariants)
        {
            MutationLogicFunctions.NameNewMicrobeSpecies(variant, modifiedSpecies);
        }

        return viableVariants.OrderByDescending(x => selectionPressures.Select(pressure => (pressure.Score(x, cache) / pressureScores[pressure]) * pressure.Strength).Sum()).ToList();
    }

    public static List<MicrobeSpecies> PruneInviableSpecies(List<MicrobeSpecies> potentialVariants, 
        SelectionPressure curPressure, 
        List<SelectionPressure>? selectionPressures, 
        Dictionary<SelectionPressure, float> pressureScores, 
        SimulationCache cache)
    {
        var previousPressures = selectionPressures.IndexOf(curPressure) > 0 ? selectionPressures.GetRange(0, selectionPressures.IndexOf(curPressure) - 1) : new List<SelectionPressure>();
        previousPressures.Reverse();

        var viableVariants = new List<MicrobeSpecies>();
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
                // TODO: Move this somewhere better
                ((MicrobeSpecies)potentialVariant).Colour = new Color((float)new Random().NextDouble(), 0.5f, 0.5f);

                viableVariants.Add(potentialVariant);
            }
        }

        return viableVariants;
    }
}
