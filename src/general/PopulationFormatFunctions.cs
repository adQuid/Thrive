public class PopulationFormatFunctions
{
    public static string FormatPopulationForPlayer(long population)
    {
        if (population == 1)
        {
            return "1";
        }
        if (population < 100)
        {
            return "Critically Endangered";
        }
        if (population < 1000)
        {
            return "Endangered";
        }
        return "Thriving";
    }
}
