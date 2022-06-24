using System;
using System.Diagnostics.CodeAnalysis;
using Godot;

/// <summary>
///   Contains logic for generating PatchMap objects
/// </summary>
[SuppressMessage("ReSharper", "StringLiteralTypo", Justification = "Patch names aren't proper words")]
public static class PatchMapGenerator
{
    public static WorldGenerationSettings WorldSettings = new();

    public static PatchMap Generate(WorldGenerationSettings settings, Species defaultSpecies, Random? random = null)
    {
        WorldSettings = settings;

        var map = new PatchMap();

        random ??= new Random(WorldSettings.Seed);

        var nameGenerator = SimulationParameters.Instance.GetPatchMapNameGenerator();

        string areaName = string.Empty;
        switch (WorldSettings.MapType)
        {
            case WorldGenerationSettings.PatchMapType.Classic:
                areaName = "Pangonian";
                break;
            case WorldGenerationSettings.PatchMapType.Procedural:
                areaName = nameGenerator.Next(random);
                break;
        }

        // Predefined patches
        var vents = new Patch(GetPatchLocalizedName(areaName, "VOLCANIC_VENT"), 0,
            GetBiomeTemplate("aavolcanic_vent"))
        {
            Depth =
            {
                [0] = 2500,
                [1] = 3000,
            },
            ScreenCoordinates = new Vector2(200, 300),
        };
        map.AddPatch(vents);

        var epipelagic = new Patch(GetPatchLocalizedName(areaName, "EPIPELAGIC"), 2,
            GetBiomeTemplate("default"))
        {
            Depth =
            {
                [0] = 0,
                [1] = 200,
            },
            ScreenCoordinates = new Vector2(200, 100),
        };
        map.AddPatch(epipelagic);

        var tidepool = new Patch(GetPatchLocalizedName(areaName, "TIDEPOOL"), 3,
            GetBiomeTemplate("tidepool"))
        {
            Depth =
            {
                [0] = 0,
                [1] = 10,
            },
            ScreenCoordinates = new Vector2(300, 100),
        };
        map.AddPatch(tidepool);

        var coast = new Patch(GetPatchLocalizedName(areaName, "COASTAL"), 6,
            GetBiomeTemplate("coastal"))
        {
            Depth =
            {
                [0] = 0,
                [1] = 200,
            },
            ScreenCoordinates = new Vector2(100, 100),
        };
        map.AddPatch(coast);

        var estuary = new Patch(GetPatchLocalizedName(areaName, "ESTUARY"), 7,
            GetBiomeTemplate("estuary"))
        {
            Depth =
            {
                [0] = 0,
                [1] = 200,
            },
            ScreenCoordinates = new Vector2(70, 160),
        };
        map.AddPatch(estuary);

        var iceShelf = new Patch(GetPatchLocalizedName(areaName, "ICESHELF"), 9,
            GetBiomeTemplate("ice_shelf"))
        {
            Depth =
            {
                [0] = 0,
                [1] = 200,
            },
            ScreenCoordinates = new Vector2(200, 30),
        };
        map.AddPatch(iceShelf);

        var seafloor = new Patch(GetPatchLocalizedName(areaName, "SEA_FLOOR"), 10,
            GetBiomeTemplate("seafloor"))
        {
            Depth =
            {
                [0] = 4000,
                [1] = 6000,
            },
            ScreenCoordinates = new Vector2(300, 400),
        };
        map.AddPatch(seafloor);

        // Starting patch based on new game settings
        switch (WorldSettings.Origin)
        {
            case WorldGenerationSettings.LifeOrigin.Vent:
                vents.AddSpecies(defaultSpecies, 1);
                map.CurrentPatch = vents;
                break;
            case WorldGenerationSettings.LifeOrigin.Pond:
                tidepool.AddSpecies(defaultSpecies, 1);
                map.CurrentPatch = tidepool;
                break;
            case WorldGenerationSettings.LifeOrigin.Panspermia:
                var startingPatch = map.Patches.Random(random);
                startingPatch!.AddSpecies(defaultSpecies, 1);
                map.CurrentPatch = startingPatch;
                break;
        }

        LinkPatches(vents, epipelagic);
        LinkPatches(seafloor, vents);
        LinkPatches(epipelagic, tidepool);
        LinkPatches(epipelagic, iceShelf);
        LinkPatches(epipelagic, coast);
        LinkPatches(coast, estuary);

        return map;
    }

    private static Biome GetBiomeTemplate(string name)
    {
        return SimulationParameters.Instance.GetBiome(name);
    }

    private static void LinkPatches(Patch patch1, Patch patch2)
    {
        patch1.AddNeighbour(patch2);
        patch2.AddNeighbour(patch1);
    }

    private static LocalizedString GetPatchLocalizedName(string name, string biomeKey)
    {
        return new LocalizedString("PATCH_NAME", name, new LocalizedString(biomeKey));
    }
}
