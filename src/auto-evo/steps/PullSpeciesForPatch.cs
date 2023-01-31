﻿using System;
using System.Collections.Generic;
using System.Linq;
using AutoEvo;
using Godot;

class PullSpeciesForPatch : IRunStep
{
    public Patch Patch;
    public SimulationCache Cache;
    public bool PlayerPatch;

    public int TotalSteps => 1;

    public bool CanRunConcurrently => false;

    public PullSpeciesForPatch(Patch patch, SimulationCache cache, bool playerPatch)
    {
        Patch = patch;
        Cache = cache;
        PlayerPatch = playerPatch;
    }

    public bool RunStep(RunResults results)
    {
        var variants = CandiateSpecies().ToList();

        var newMiche = PopulateMiche(results, variants);

        PopulateForMiche(Patch, newMiche, variants, results, Cache);

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

        PopulateForMiche(Patch, results.MicheByPatch[Patch], candidates, results, Cache);

        // Second trophic level
        var speciesToEat = results.MicheByPatch[Patch].AllOccupants();
        var newMiches = SelectionPressure.PredationMiches(Patch, speciesToEat.ToHashSet(), Cache);

        results.MicheByPatch[Patch].AddChildren(newMiches);

        newMiches.ForEach(x => PopulateForMiche(Patch, x, candidates, results, Cache));

        // Third trophic level
        speciesToEat = newMiches.SelectMany(x => x.AllOccupants());
        newMiches = SelectionPressure.PredationMiches(Patch, speciesToEat.ToHashSet(), Cache);

        results.MicheByPatch[Patch].AddChildren(newMiches);

        return results.MicheByPatch[Patch];
    }

    /*
     * Finds or creates a species for every leaf node of the provided miche.
     */
    private static void PopulateForMiche(Patch patch, Miche miche, IEnumerable<Species> allSpecies, RunResults results, SimulationCache cache)
    {
        foreach (var curSpecies in allSpecies)
        {
            // Try to add existing species
            if (miche.Root().InsertSpecies(curSpecies))
            {

                results.MakeSureResultExistsForSpecies(curSpecies);

                // Mark the best pressures for hovering over in game
                foreach(var traversal in miche.TraversalsTerminatingInSpecies(curSpecies))
                {
                    results.results[curSpecies].AddBestPressuresResults(patch, traversal.Select(x => x.Pressure).ToList());
                }
            }
        }

        /*foreach (var traversal in miche.AllTraversals())
        {
            var pressures = traversal.Select(x => x.Pressure);
            var qualifiedSpeciesScores = new Dictionary<Species, double>();

            var variants = new List<Species>(allSpecies);

            // Take all possible variants of all possible species
            foreach (var species in allSpecies)
            {
                //TODO: put this in a fixed place
                var partList = new PartList(species);

                //TODO: Should I just add the best one?
                var variantsToAdd = ModifyExistingSpecies.ViableVariants(results, species, patch, partList, cache, traversal.Select(x => x.Pressure).ToList());

                foreach (var variant in variantsToAdd)
                {
                    results.AncestorDictionary.Add(variant, species);
                }

                variants.AddRange(variantsToAdd);
            }

            // Set scores for all starting species
            foreach (var species in variants)
            {
                qualifiedSpeciesScores[species] = 0;
            }

            // Travel down the list of pressures, adding up totals and elminiating any zeros
            foreach (var pressure in pressures)
            {
                var remainingQualifiedSpecies = new Dictionary<Species, double>(qualifiedSpeciesScores);

                foreach (var species in qualifiedSpeciesScores.Keys)
                {
                    var score = pressure.Score(species, cache);
                    if (score > 0)
                    {
                        remainingQualifiedSpecies[species] += score;
                    }
                    else
                    {
                        remainingQualifiedSpecies.Remove(species);
                        continue;
                    }
                }

                qualifiedSpeciesScores = remainingQualifiedSpecies;
            }

            // If anything is able to survive this path, put the best scoring species on the leaf node.
            if (qualifiedSpeciesScores.Count > 0)
            {
                var speciesToAdd = qualifiedSpeciesScores.OrderByDescending(x => x.Value).First().Key;

                
            }
        }*/
    }

    private IEnumerable<Species> CandiateSpecies()
    {
        var retval = Patch.Adjacent.SelectMany(x => x.SpeciesInPatch.Keys).ToList();

        retval.AddRange(Patch.SpeciesInPatch.Select(x => x.Key));

        return retval;
    }
}
