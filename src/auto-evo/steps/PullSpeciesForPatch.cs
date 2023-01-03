using System;
using System.Collections.Generic;
using System.Linq;
using AutoEvo;
using Godot;

class PullSpeciesForPatch : IRunStep
{
    public Patch Patch;
    public SimulationCache Cache;

    public int TotalSteps => 1;

    public bool CanRunConcurrently => false;

    public PullSpeciesForPatch(Patch patch, SimulationCache cache)
    {
        Patch = patch;
        Cache = cache;
    }

    public bool RunStep(RunResults results)
    {
        GD.Print("Running 'pullspecies' for "+Patch.Name+". "+CandiateSpecies().Count()+" candidate species");
        results.Miches[Patch] = Miche.RootMiche();
        results.Miches[Patch].AddChildren(SelectionPressure.AutotrophicMichesForPatch(Patch, Cache));

        var variants = CandiateSpecies().ToList();
        /*foreach (var niche in results.Miches[Patch].SelectMany(x => x.AllTraversals()))
        {
            // TODO: Only check relivent species
            foreach (var species in CandiateSpecies())
            {
                //TODO: put this in a fixed place
                var partList = new PartList(species);

                variants.Add(ModifyExistingSpecies.ViableVariants(results, species, Patch, partList, Cache, niche.Select(x => x.Pressure).ToList()).First());
            }
        }*/

        PopulateForMiche(Patch, results.Miches[Patch], variants, results, Cache);

        // Second trophic level
        var speciesToEat = results.Miches[Patch].AllOccupants();
        GD.Print("Species to eat: "+string.Join(",", speciesToEat.Select(x => x.FormattedName)));
        var newMiches = SelectionPressure.PredationMiches(Patch, speciesToEat.ToHashSet(), Cache);

        results.Miches[Patch].AddChildren(newMiches);

        newMiches.ForEach(x => PopulateForMiche(Patch, x, variants, results, Cache));

        // Third trophic level
        speciesToEat = newMiches.SelectMany(x => x.AllOccupants());
        newMiches = SelectionPressure.PredationMiches(Patch, speciesToEat.ToHashSet(), Cache);

        results.Miches[Patch].AddChildren(newMiches);

        newMiches.ForEach(x => PopulateForMiche(Patch, x, variants, results, Cache));

        return true;
    }

    private static void PopulateForMiche(Patch patch, Miche miche, IEnumerable<Species> allSpecies, RunResults results, SimulationCache cache)
    {
        GD.Print(miche.AllTraversals().Count());

        foreach (var traversal in miche.AllTraversals())
        {
            var pressures = traversal.Select(x => x.Pressure);
            var qualifiedSpeciesScores = new Dictionary<Species, double>();

            var variants = new List<Species>(allSpecies);

            foreach (var species in allSpecies)
            {
                //TODO: put this in a fixed place
                var partList = new PartList(species);

                //TODO: Should I just add the best one?
                variants.AddRange(ModifyExistingSpecies.ViableVariants(results, species, patch, partList, cache, traversal.Select(x => x.Pressure).ToList()));
            }

            foreach (var species in variants)
            {
                qualifiedSpeciesScores[species] = 0;
            }

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

            // If anything is able to survive this path, put it on the leaf node.
            if (qualifiedSpeciesScores.Count > 0)
            {
                var speciesToAdd = qualifiedSpeciesScores.OrderByDescending(x => x.Value).First().Key;

                // TODO: It's probably very inefficient to do this here
                miche.InsertSpecies(speciesToAdd);

                GD.Print("Assigning Species " + speciesToAdd.FormattedName + " to niche " + String.Join(",", traversal.Select(x => x.Pressure.Name())));

                var population = new Dictionary<Patch, long>();
                population[patch] = 1000;

                if (!results.SpeciesHasResults(speciesToAdd))
                {
                    // TODO: Use the real parent, fool!
                    results.AddNewSpecies(speciesToAdd, population, RunResults.NewSpeciesType.FillNiche, speciesToAdd);
                }

                // Mark the best pressures for hovering over in game
                results.results[speciesToAdd].AddBestPressuresResults(patch, traversal.Select(x => x.Pressure).ToList());
            }
        }
    }

    private IEnumerable<Species> CandiateSpecies()
    {
        var retval = Patch.Adjacent.SelectMany(x => x.SpeciesInPatch.Keys).ToList();

        retval.AddRange(Patch.SpeciesInPatch.Select(x => x.Key));

        return retval;
    }
}
