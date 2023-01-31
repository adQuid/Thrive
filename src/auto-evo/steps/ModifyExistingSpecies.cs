﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AutoEvo;
using Godot;

public class ModifyExistingSpecies : IRunStep
{
    public Patch Patch;
    public SimulationCache Cache;
    public PartList PartList;

    public ModifyExistingSpecies(Patch patch, SimulationCache cache)
    {
        Patch = patch;
        Cache = cache;
        PartList = new PartList(species);
    }

    public int TotalSteps => 1;

    public bool CanRunConcurrently => true;

    public bool RunStep(RunResults results)
    {
        foreach(var species in Patch.Miche.AllOccupants())
        {
            foreach(var traversal in Patch.Miche.TraversalsTerminatingInSpecies(species))
            {
                var partlist = new PartList(species);

                var variants = ViableVariants(results, species, Patch, partlist, new SimulationCache(), traversal.Select(x => x.Pressure).ToList());

                // I can safely just insert to the bottom here, right?
                traversal.Last().InsertSpecies(variants.First());
            }
        }

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
            potentialVariants.AddRange(viableVariants);

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
                var pastScore = pastPressure.Score(potentialVariant, cache);
                var pastImprovement = pastScore / pressureScores[pastPressure];

                // If, proportional to weights, the improvement to the current pressure doesn't outweigh the loss to the other pressures, this is still viable.
                if (pastScore == 0)
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
