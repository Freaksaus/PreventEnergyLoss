using HarmonyLib;
using StardewModdingAPI;
using StardewValley;
using StardewValley.Tools;

namespace PreventEnergyLoss;

/// <summary>The mod entry point.</summary>
internal sealed class ModEntry : Mod
{
    private static bool _toolEfficientChanged = false;

    public override void Entry(IModHelper helper)
    {
        Harmony harmony = new(Helper.ModRegistry.ModID);

        harmony.Patch(
               original: AccessTools.Method(typeof(Farmer), nameof(Farmer.BeginUsingTool)),
               postfix: new HarmonyMethod(typeof(ModEntry), nameof(BeginUsingTool_Postfix))
           );

        harmony.Patch(
               original: AccessTools.Method(typeof(Farmer), nameof(Farmer.EndUsingTool)),
               postfix: new HarmonyMethod(typeof(ModEntry), nameof(EndUsingTool_Postfix))
           );
    }

    public static void BeginUsingTool_Postfix()
    {
        if (Game1.player.CurrentTool.IsEfficient)
        {
            //Tool already doesn't consume energy
            return;
        }

        if (Game1.player.CurrentTool is not Axe)
        {
            return;
        }

        var tile = Game1.player.GetToolLocation(false) / 64;
        var tileObject = Game1.currentLocation.getObjectAtTile((int)tile.X, (int)tile.Y);
        if (tileObject is null)
        {
            return;
        }

        if (tileObject.Name.Equals("Stone"))
        {
            Game1.showGlobalMessage(Game1.player.stamina.ToString());
            Game1.player.CurrentTool.IsEfficient = true;
            _toolEfficientChanged = true;
        }
    }

    public static void EndUsingTool_Postfix()
    {
        if (!_toolEfficientChanged)
        {
            return;
        }

        Game1.player.CurrentTool.IsEfficient = false;
    }
}
