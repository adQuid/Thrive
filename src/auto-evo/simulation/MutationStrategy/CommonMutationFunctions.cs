using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

class CommonMutationFunctions
{
    public static OrganelleTemplate GetRealisticPosition(OrganelleDefinition organelle,
        OrganelleLayout<OrganelleTemplate> existingOrganelles)
    {
        var result = new OrganelleTemplate(organelle, new Hex(0, 0), 0);

        // Loop through all the organelles and find an open spot to
        // place our new organelle attached to existing organelles
        // This almost always is over at the first iteration, so its
        // not a huge performance hog
        foreach (var otherOrganelle in existingOrganelles.OrderBy(_ => (new Random()).Next()))
        {
            // The otherOrganelle is the organelle we wish to be next to
            // Loop its hexes and check positions next to them
            foreach (var hex in otherOrganelle.RotatedHexes)
            {
                // Offset by hexes in organelle we are looking at
                var pos = otherOrganelle.Position + hex;

                for (int side = 1; side <= 6; ++side)
                {
                    for (int radius = 1; radius <= 3; ++radius)
                    {
                        // Offset by hex offset multiplied by a factor to check for greater range
                        var hexOffset = Hex.HexNeighbourOffset[(Hex.HexSide)side];
                        hexOffset *= radius;
                        result.Position = pos + hexOffset;

                        // Check every possible rotation value.
                        for (int rotation = 0; rotation <= 5; ++rotation)
                        {
                            result.Orientation = rotation;

                            if (existingOrganelles.CanPlace(result))
                            {
                                return result;
                            }
                        }
                    }
                }
            }
        }

        // We didnt find an open spot, this doesn't make much sense
        throw new ArgumentException("Mutation code could not find a good position " +
            "for a new organelle");
    }
}
