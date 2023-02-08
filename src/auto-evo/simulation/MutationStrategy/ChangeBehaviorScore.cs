using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

class ChangeBehaviorScore : IMutationStrategy<MicrobeSpecies>
{
    public enum BehaviorAttribute
    {
        ACTIVITY, 
        AGGRESSION, 
        OPPORTUNISM, 
        FOCUS,
        FEAR, 
    }

    private BehaviorAttribute Attribute;
    private float MaxChange;

    public ChangeBehaviorScore(BehaviorAttribute attribute, float maxChange)
    {
        Attribute = attribute;
        MaxChange = maxChange;
    }

    public List<MicrobeSpecies> MutationsOf(MicrobeSpecies baseSpecies, MutationLibrary partList)
    {
        var newSpecies = (MicrobeSpecies)baseSpecies.Clone();

        var change = (float)new Random().NextDouble() * MaxChange;

        if (Math.Abs(change) < 1)
        {
            return new List<MicrobeSpecies>();
        }

        switch (Attribute)
        {
            case BehaviorAttribute.ACTIVITY:
                newSpecies.Behaviour.Activity = Math.Max(Math.Min(newSpecies.Behaviour.Activity + change, Constants.MAX_SPECIES_ACTIVITY), 0);
                break;
            case BehaviorAttribute.AGGRESSION:
                newSpecies.Behaviour.Aggression = Math.Max(Math.Min(newSpecies.Behaviour.Aggression + change, Constants.MAX_SPECIES_AGGRESSION), 0);
                break;
            case BehaviorAttribute.OPPORTUNISM:
                newSpecies.Behaviour.Opportunism = Math.Max(Math.Min(newSpecies.Behaviour.Opportunism + change, Constants.MAX_SPECIES_OPPORTUNISM), 0);
                break;
            case BehaviorAttribute.FEAR:
                newSpecies.Behaviour.Fear = Math.Max(Math.Min(newSpecies.Behaviour.Fear + change, Constants.MAX_SPECIES_FEAR), 0);
                break;
            case BehaviorAttribute.FOCUS:
                newSpecies.Behaviour.Focus = Math.Max(Math.Min(newSpecies.Behaviour.Focus + change, Constants.MAX_SPECIES_FOCUS), 0);
                break;
            default:
                break;
        }

        return new List<MicrobeSpecies> { newSpecies };
    }
}