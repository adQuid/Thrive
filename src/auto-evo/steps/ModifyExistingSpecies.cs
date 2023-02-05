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

    public ModifyExistingSpecies(Patch patch, SimulationCache cache)
    {
        Patch = patch;
        Cache = cache;
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

                if (variants.Count() > 0)
                {
                    var speciesToAdd = variants.First();

                    // TODO: Be less hacky with this
                    if (speciesToAdd.FormattedName != species.FormattedName)
                    {

                        results.AncestorDictionary.Add(speciesToAdd, species);
                        results.MicheByPatch[Patch].InsertSpecies(variants.First());

                        results.MakeSureResultExistsForSpecies(speciesToAdd);
                        // Mark the best pressures for hovering over in game
                        results.results[speciesToAdd].AddBestPressuresResults(Patch, traversal.Select(x => x.Pressure).ToList());
                    }
                }
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
        var baseSpecies = (MicrobeSpecies)results.LastestVersionForSpecies(species);

        var selectionPressures = SelectionPressure.PressuresFromPatch(species, patch, partList, cache, niche);

        // find the initial scores
        var pressureScores = new Dictionary<SelectionPressure, float>();
        foreach (var curPressure in selectionPressures)
        {
            pressureScores[curPressure] = curPressure.Score(baseSpecies, cache);
        }

        var viableVariants = new List<MicrobeSpecies> { baseSpecies };

        var pressuresSoFar = new List<SelectionPressure>();
        foreach (var curPressure in selectionPressures)
        {
            pressuresSoFar.Add(curPressure);

            // For each viable variant, get a new variants that at least improve score a little bit
            var potentialVariants = viableVariants.Select(startVariant =>
                curPressure.MicrobeMutations.Select(mutationStrategy => mutationStrategy.MutationsOf(startVariant, partList))
                .SelectMany(x => x)
                //.Where(x => curPressure.Score(x, cache) >= curPressure.Score(startVariant, cache))
                )
                .SelectMany(x => x).ToList();
            potentialVariants.AddRange(viableVariants);

            // Prune variants that hurt the previous scores too much
            viableVariants = PruneInviableSpecies(potentialVariants, pressuresSoFar, baseSpecies, cache);
        }

        foreach (var variant in viableVariants)
        {
            MutationLogicFunctions.NameNewMicrobeSpecies(variant, baseSpecies);
        }

        return viableVariants.OrderByDescending(x =>
        selectionPressures.Select(pressure => (pressure.Score(x, cache) / pressureScores[pressure]) * pressure.Strength).Sum() + (x == baseSpecies ? 0.01f : 0.0f))
            .ToList();
    }

    public static List<MicrobeSpecies> PruneInviableSpecies(List<MicrobeSpecies> potentialVariants,
        List<SelectionPressure>? selectionPressures,
        Species baseSpecies,
        SimulationCache cache)
    {
        var viableVariants = new List<MicrobeSpecies>();
        foreach (var potentialVariant in potentialVariants)
        {
            var combinedScores = 0.0;
            foreach (var pastPressure in selectionPressures)
            {
                combinedScores += pastPressure.WeightedComparedScores(potentialVariant, baseSpecies, cache);
            }

            if (combinedScores >= 0)
            {
                // TODO: Move this somewhere better
                ((MicrobeSpecies)potentialVariant).Colour = new Color((float)new Random().NextDouble(), 0.5f, 0.5f);

                viableVariants.Add(potentialVariant);
            }

        }

        return viableVariants;
    }
}
