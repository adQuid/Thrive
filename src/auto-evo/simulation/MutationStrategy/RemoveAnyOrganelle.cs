using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

class RemoveAnyOrganelle : IMutationStrategy<MicrobeSpecies>
{
    public List<MicrobeSpecies> MutationsOf(MicrobeSpecies baseSpecies)
    {
        //TODO: Make this something passed in
        var random = new Random();

        var newSpecies = (MicrobeSpecies)baseSpecies.Clone();

        if (newSpecies.Organelles.Count() > 1)
        {
            newSpecies.Organelles.RemoveHexAt(
                newSpecies.Organelles.ToList().ElementAt(random.Next(0, newSpecies.Organelles.Count())).Position
            );
        }

        var islandHexes = newSpecies.Organelles.GetIslandHexes();

        // Attach islands
        while (islandHexes.Count > 0)
        {
            var mainHexes = newSpecies.Organelles.ComputeHexCache().Except(islandHexes);

            // Compute shortest hex distance
            Hex minSubHex = default;
            int minDistance = int.MaxValue;
            foreach (var mainHex in mainHexes)
            {
                foreach (var islandHex in islandHexes)
                {
                    var sub = islandHex - mainHex;
                    int distance = (Math.Abs(sub.Q) + Math.Abs(sub.Q + sub.R) + Math.Abs(sub.R)) / 2;
                    if (distance < minDistance)
                    {
                        minDistance = distance;
                        minSubHex = sub;

                        // early exit if minDistance == 2 (distance 1 == direct neighbour => not an island)
                        if (minDistance == 2)
                            break;
                    }
                }

                // early exit if minDistance == 2 (distance 1 == direct neighbour => not an island)
                if (minDistance == 2)
                    break;
            }

            // Calculate the path to move island organelles.
            // If statement is there because otherwise the path could be (0, 0).
            if (minSubHex.Q != minSubHex.R)
                minSubHex.Q = (int)(minSubHex.Q * (minDistance - 1.0) / minDistance);

            minSubHex.R = (int)(minSubHex.R * (minDistance - 1.0) / minDistance);

            // Move all island organelles by minSubHex
            foreach (var organelle in newSpecies.Organelles.Where(
                         o => islandHexes.Any(h =>
                             o.Definition.GetRotatedHexes(o.Orientation).Contains(h - o.Position))))
            {
                organelle.Position -= minSubHex;
            }

            islandHexes = newSpecies.Organelles.GetIslandHexes();
        }

        return new List<MicrobeSpecies>
        {
            newSpecies
        };
    }
}
