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

        CommonMutationFunctions.AttachIslandHexes(newSpecies.Organelles);

        return new List<MicrobeSpecies>
        {
            newSpecies
        };
    }
}
