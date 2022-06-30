using Newtonsoft.Json;

public class EditorGlobals
{
    [JsonProperty]
    public static int MaxMutationPoints = Constants.BASE_MUTATION_POINTS;

    [JsonProperty]
    public static bool InPityEditor;
}