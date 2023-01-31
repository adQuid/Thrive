using System;
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

        PopulateForMiche(Patch, results.MicheByPatch[Patch], variants, results, Cache);

        return true;
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

        // if there are any empty miches, try out other species to fill the gap
        foreach (var emptyMiche in miche.TraversalsTerminatingInSpecies(null))
        {
            GD.Print("Searching to fill empty path: " + String.Join(",",emptyMiche.Select(x => x.Name)));
            foreach (var curSpecies in allSpecies)
            {
                var variants = ModifyExistingSpecies.ViableVariants(results, curSpecies, patch, new PartList(curSpecies), cache, emptyMiche.Select(x => x.Pressure).ToList());

                if (variants.Count() > 0)
                {
                    // This may do nothing or be overwritten, and that's ok
                    emptyMiche.Last().InsertSpecies(variants.First());
                    results.AncestorDictionary.Add(variants.First(), curSpecies);
                }
            }

            var finalSpecies = emptyMiche.Last().Occupant;

            if (finalSpecies != null)
            {
                GD.Print("  and I found something: "+finalSpecies.FormattedName);                
                results.MakeSureResultExistsForSpecies(finalSpecies);

                // Mark the best pressures for hovering over in game
                foreach (var traversal in miche.TraversalsTerminatingInSpecies(finalSpecies))
                {
                    results.results[finalSpecies].AddBestPressuresResults(patch, traversal.Select(x => x.Pressure).ToList());
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
