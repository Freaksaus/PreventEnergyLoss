using HarmonyLib;
using Microsoft.Xna.Framework;
using System.Diagnostics;
using StardewModdingAPI;
using StardewValley;
using StardewValley.Buildings;
using StardewValley.Locations;
using StardewValley.Objects;
using StardewValley.TerrainFeatures;

namespace PreventEnergyLoss;

/// <summary>The mod entry point.</summary>
internal sealed class ModEntry : Mod
{
    private static Tool? _toolMarkedEfficient;
    private static IMonitor _monitor = null!;
    private static readonly List<Vector2> _waterspotTiles = new()
    {
        new Vector2(16f, 6f),
        new Vector2(16f, 7f),
        new Vector2(16f, 8f),
        new Vector2(16f, 9f)
    };

    public override void Entry(IModHelper helper)
    {
        _monitor = Monitor;
        Harmony harmony = new(Helper.ModRegistry.ModID);

        harmony.Patch(
               original: AccessTools.Method(typeof(Tool), nameof(Tool.DoFunction)),
               prefix: new HarmonyMethod(typeof(ModEntry), nameof(DoFunction_Prefix))
           );

        var endUsingToolMethod = AccessTools.Method(typeof(Farmer), "endUsingTool")
                                ?? AccessTools.Method(typeof(Farmer), "EndUsingTool");
        if (endUsingToolMethod is not null)
        {
            harmony.Patch(
                   original: endUsingToolMethod,
                   postfix: new HarmonyMethod(typeof(ModEntry), nameof(EndUsingTool_Postfix))
               );
        }
        else
        {
            _monitor.Log("Could not patch Farmer endUsingTool/EndUsingTool; efficiency cleanup may not run.", LogLevel.Warn);
        }
    }

    public static void DoFunction_Prefix(GameLocation location, int x, int y, int power, Farmer who)
    {
        if (who.CurrentTool is null)
        {
            return;
        }

        if (who.CurrentTool.IsEfficient)
        {
            DebugLog($"{who.CurrentTool.Name} is already efficient");
            //Tool already doesn't consume energy
            return;
        }

        var tileVector = who.GetToolLocation(false) / 64;
        tileVector = new Vector2((int)tileVector.X, (int)tileVector.Y);

        var shouldTakeEnergy = true;
        switch (who.CurrentTool)
        {
            case StardewValley.Tools.Axe:
                shouldTakeEnergy = ShouldAxeTakeEnergy(location, tileVector);
                break;
            case StardewValley.Tools.Pickaxe:
                shouldTakeEnergy = ShouldPickaxeTakeEnergy(location, tileVector);
                break;
            case StardewValley.Tools.Hoe:
                shouldTakeEnergy = ShouldHoeTakeEnergy(who, location, tileVector);
                break;
            case StardewValley.Tools.WateringCan:
                shouldTakeEnergy = ShouldWateringCanTakeEnergy(who, location, tileVector);
                break;
            default:
                return;
        }

        if (shouldTakeEnergy)
        {
            return;
        }

        DebugLog("Energy usage preserved");
        who.CurrentTool.IsEfficient = true;
        _toolMarkedEfficient = who.CurrentTool;
    }

    public static void EndUsingTool_Postfix(Farmer __instance)
    {
        if (_toolMarkedEfficient is null)
        {
            return;
        }

        _toolMarkedEfficient.IsEfficient = false;

        _toolMarkedEfficient = null;
        DebugLog("EndUsingTool_Postfix reset tool efficiency");
    }

    [Conditional("DEBUG")]
    private static void DebugLog(string message)
    {
        _monitor.Log(message, LogLevel.Debug);
    }

    private static bool ShouldAxeTakeEnergy(
        GameLocation location,
        Vector2 tile)
    {
        if (location.Objects.TryGetValue(tile, out StardewValley.Object tileObject))
        {
            if (tileObject is Furniture)
            {
                return false;
            }
            else if (tileObject is BreakableContainer)
            {
                DebugLog("Axe energy used because of a breakable container");
                return true;
            }
            else if (tileObject.Type == ObjectTypes.Crafting || tileObject.Name == ObjectNames.Twig || tileObject.Name == ObjectNames.Weeds)
            {
                DebugLog($"Axe energy used because of a {tileObject.Name}");
                return true;
            }
        }

        if (location.terrainFeatures.TryGetValue(tile, out TerrainFeature terrainFeature))
        {
            if (terrainFeature is Tree)
            {
                DebugLog("Axe energy used because of a tree");
                return true;
            }
            else if (terrainFeature is Flooring)
            {
                DebugLog("Axe energy used because of flooring");
                return true;
            }
            else if (terrainFeature is HoeDirt && location.isCropAtTile((int)tile.X, (int)tile.Y))
            {
                DebugLog("Axe energy used because of a crop");
                return true;
            }
        }

        var tileRectangle = new Rectangle((int)tile.X * 64, (int)tile.Y * 64, 64, 64);
        if (location.resourceClumps
            .Where(x => x.getBoundingBox().Intersects(tileRectangle))
            .Where(x => x.parentSheetIndex.Value == ResourceClump.stumpIndex ||
                        x.parentSheetIndex.Value == ResourceClump.hollowLogIndex)
            .Any())
        {
            DebugLog("Axe energy used because of a log or stump");
            return true;
        }

        return false;
    }

    private static bool ShouldPickaxeTakeEnergy(
            GameLocation location,
            Vector2 tile)
    {
        if (location.Objects.TryGetValue(tile, out StardewValley.Object tileObject))
        {
            if (tileObject is Furniture)
            {
                return false;
            }
            else if (tileObject is BreakableContainer)
            {
                DebugLog("Pickaxe energy used because of a breakable container");
                return true;
            }
            else if (tileObject.Type == ObjectTypes.Crafting || tileObject.Name == ObjectNames.Stone || tileObject.Name == ObjectNames.Weeds)
            {
                DebugLog($"Pickaxe energy used because of a {tileObject.Name}");
                return true;
            }
        }

        if (location.terrainFeatures.TryGetValue(tile, out TerrainFeature terrainFeature))
        {
            if (terrainFeature is Flooring)
            {
                DebugLog("Pickaxe energy used because of flooring");
                return true;
            }
            else if (terrainFeature is HoeDirt)
            {
                DebugLog("Pickaxe energy used because of hoed dirt");
                return true;
            }
            else if (terrainFeature is Tree && (terrainFeature as Tree)!.growthStage.Value == 1)
            {
                DebugLog("Pickaxe energy used because of a tree");
                return true;
            }
        }

        var tileRectangle = new Rectangle((int)tile.X * 64, (int)tile.Y * 64, 64, 64);
        if (location.resourceClumps
                .Where(x => x.getBoundingBox().Intersects(tileRectangle))
                .Where(x => x.parentSheetIndex.Value == ResourceClump.boulderIndex ||
                            x.parentSheetIndex.Value == ResourceClump.meteoriteIndex ||
                            x.parentSheetIndex.Value == ResourceClump.mineRock1Index ||
                            x.parentSheetIndex.Value == ResourceClump.mineRock2Index ||
                            x.parentSheetIndex.Value == ResourceClump.mineRock3Index ||
                            x.parentSheetIndex.Value == ResourceClump.mineRock4Index ||
                            x.parentSheetIndex.Value == ResourceClump.quarryBoulderIndex)
                .Any())
        {
            DebugLog("Pickaxe energy used because of a boulder or meteorite");
            return true;
        }

        return false;
    }

    private static bool ShouldHoeTakeEnergy(
        Farmer who,
        GameLocation location,
        Vector2 tile)
    {
        var affectedTiles = ToolMethods.GetTilesAffected(tile, Game1.player.toolPower.Value, who);
        foreach (var affectedTile in affectedTiles)
        {
            if (location.Objects.TryGetValue(affectedTile, out StardewValley.Object tileObject))
            {
                if (tileObject is BreakableContainer)
                {
                    DebugLog("Hoe energy used because of a breakable container");
                    return true;
                }
                else if (tileObject is not Furniture && (tileObject.Type == ObjectTypes.Crafting || tileObject.Name == ObjectNames.ArtifactSpot || tileObject.Name == ObjectNames.Weeds))
                {
                    DebugLog($"Hoe energy used because of a {tileObject.Name}");
                    return true;
                }
            }

            if (location.doesTileHaveProperty((int)affectedTile.X, (int)affectedTile.Y, "Diggable", "Back") is not null &&
                !location.IsTileOccupiedBy(new Vector2(affectedTile.X, affectedTile.Y)))
            {
                return true;
            }

            if (location.terrainFeatures.TryGetValue(affectedTile, out TerrainFeature terrainFeature))
            {
                if (terrainFeature is Tree && (terrainFeature as Tree)!.growthStage.Value == 1)
                {
                    DebugLog("Hoe energy used because of a tree");
                    return true;
                }
            }
        }

        return false;
    }

    private static bool ShouldWateringCanTakeEnergy(
            Farmer who,
            GameLocation location,
            Vector2 tile)
    {
        var affectedTiles = ToolMethods.GetTilesAffected(tile, Game1.player.toolPower.Value, who);
        foreach (var affectedTile in affectedTiles)
        {
            if (location.terrainFeatures.TryGetValue(affectedTile, out TerrainFeature terrainFeature))
            {
                if (terrainFeature is HoeDirt && (terrainFeature as HoeDirt)?.state.Value == 0)
                {
                    DebugLog("Watering can energy used because of unwatered hoed dirt");
                    return true;
                }
            }

            var tileRectangle = new Rectangle((int)affectedTile.X * 64, (int)affectedTile.Y * 64, 64, 64);
            if (location.buildings
                .OfType<PetBowl>()
                .Where(x => !x.watered.Value)
                .Where(x => x.GetBoundingBox().Intersects(tileRectangle))
                .Any())
            {
                DebugLog("Watering can energy used because of pet bowl");
                return true;
            }

            if (location is SlimeHutch)
            {
                var currentLocation = location as SlimeHutch;
                var waterspotIndex = _waterspotTiles.IndexOf(affectedTile);
                if (waterspotIndex >= 0 && waterspotIndex < currentLocation!.waterSpots.Count && !currentLocation!.waterSpots[waterspotIndex])
                {
                    DebugLog("Watering can energy used because of slime hutch watering spot");
                    return true;
                }
            }
            else if (location is VolcanoDungeon)
            {
                var currentLocation = location as VolcanoDungeon;
                if (currentLocation!.isTileOnMap(affectedTile) &&
                    currentLocation.waterTiles[(int)affectedTile.X, (int)affectedTile.Y] &&
                    !currentLocation.cooledLavaTiles.ContainsKey(affectedTile))
                {
                    DebugLog("Watering can energy used because of Volcano dungeon lava");
                    return true;
                }

                if (location.Objects.TryGetValue(affectedTile, out StardewValley.Object tileObject))
                {
                    if (tileObject is IndoorPot && (tileObject as IndoorPot)!.hoeDirt.Value.state.Value == 0)
                    {
                        DebugLog("Watering can energy used because of unwatered garden plant");
                        return true;
                    }
                }
            }
        }

        return false;
    }
}