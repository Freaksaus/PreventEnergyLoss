using HarmonyLib;
using Microsoft.Xna.Framework;
using StardewModdingAPI;
using StardewValley;
using StardewValley.Objects;
using StardewValley.TerrainFeatures;

namespace PreventEnergyLoss;

/// <summary>The mod entry point.</summary>
internal sealed class ModEntry : Mod
{
    private static bool _toolEfficientChanged = false;
    private static IMonitor _monitor;

    public override void Entry(IModHelper helper)
    {
        Harmony harmony = new(Helper.ModRegistry.ModID);

        harmony.Patch(
               original: AccessTools.Method(typeof(Tool), nameof(Tool.DoFunction)),
               prefix: new HarmonyMethod(typeof(ModEntry), nameof(DoFunction_Prefix))
           );

        harmony.Patch(
               original: AccessTools.Method(typeof(Farmer), nameof(Farmer.EndUsingTool)),
               postfix: new HarmonyMethod(typeof(ModEntry), nameof(EndUsingTool_Postfix))
           );

        _monitor = Monitor;
    }

    public static void DoFunction_Prefix(GameLocation location, int x, int y, int power, Farmer who)
    {
        if (Game1.player.CurrentTool.IsEfficient)
        {
            //Tool already doesn't consume energy
            return;
        }

        var tileVector = Game1.player.GetToolLocation(false) / 64;
        tileVector = new Vector2((int)tileVector.X, (int)tileVector.Y);

        var shouldTakeEnergy = true;
        switch (Game1.player.CurrentTool)
        {
            case StardewValley.Tools.Axe:
                shouldTakeEnergy = ShouldAxeTakeEnergy(Game1.currentLocation, tileVector);
                break;
            case StardewValley.Tools.Pickaxe:
                shouldTakeEnergy = ShouldPickaxeTakeEnergy(Game1.currentLocation, tileVector);
                break;
            case StardewValley.Tools.Hoe:
                shouldTakeEnergy = ShouldHoeTakeEnergy(Game1.player, Game1.currentLocation, tileVector);
                break;
            default:
                return;
        }

        if (shouldTakeEnergy)
        {
            return;
        }

        _monitor.Log("Energy usage preserved", LogLevel.Debug);
        Game1.player.CurrentTool.IsEfficient = true;
        _toolEfficientChanged = true;
    }

    public static void EndUsingTool_Postfix()
    {
        if (!_toolEfficientChanged)
        {
            return;
        }

        Game1.player.CurrentTool.IsEfficient = false;
        _toolEfficientChanged = false;
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
                _monitor.Log("Axe energy used because of a breakable container", LogLevel.Debug);
                return true;
            }
            else if (tileObject.Type == ObjectTypes.Crafting || tileObject.Name == ObjectNames.Twig || tileObject.Name == ObjectNames.Weeds)
            {
                _monitor.Log($"Axe energy used because of a {tileObject.Name}", LogLevel.Debug);
                return true;
            }
        }

        if (location.terrainFeatures.TryGetValue(tile, out TerrainFeature terrainFeature))
        {
            if (terrainFeature is Tree)
            {
                _monitor.Log("Axe energy used because of a tree", LogLevel.Debug);
                return true;
            }
            else if (terrainFeature is Flooring)
            {
                _monitor.Log("Axe energy used because of flooring", LogLevel.Debug);
                return true;
            }
            else if (terrainFeature is HoeDirt && location.isCropAtTile((int)tile.X, (int)tile.Y))
            {
                _monitor.Log("Axe energy used because of a crop", LogLevel.Debug);
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
            _monitor.Log("Axe energy used because of a log or stump", LogLevel.Debug);
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
                _monitor.Log("Pickaxe energy used because of a breakable container", LogLevel.Debug);
                return true;
            }
            else if (tileObject.Type == ObjectTypes.Crafting || tileObject.Name == ObjectNames.Stone || tileObject.Name == ObjectNames.Weeds)
            {
                _monitor.Log($"Pickaxe energy used because of a {tileObject.Name}", LogLevel.Debug);
                return true;
            }
        }

        if (location.terrainFeatures.TryGetValue(tile, out TerrainFeature terrainFeature))
        {
            if (terrainFeature is Flooring)
            {
                _monitor.Log("Pickaxe energy used because of flooring", LogLevel.Debug);
                return true;
            }
            else if (terrainFeature is HoeDirt)
            {
                _monitor.Log("Pickaxe energy used because of hoed dirt", LogLevel.Debug);
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
            _monitor.Log("Pickaxe energy used because of a boulder or meteorite", LogLevel.Debug);
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
                    _monitor.Log("Hoe energy used because of a breakable container", LogLevel.Debug);
                    return true;
                }
                else if (tileObject is not Furniture && (tileObject.Type == ObjectTypes.Crafting || tileObject.Name == ObjectNames.ArtifactSpot || tileObject.Name == ObjectNames.Weeds))
                {
                    _monitor.Log($"Hoe energy used because of a {tileObject.Name}", LogLevel.Debug);
                    return true;
                }
            }

            if (location.doesTileHaveProperty((int)affectedTile.X, (int)affectedTile.Y, "Diggable", "Back") is not null &&
                !location.IsTileOccupiedBy(new Vector2(affectedTile.X, affectedTile.Y)))
            {
                return true;
            }
        }

        return false;
    }
}
