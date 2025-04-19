using Microsoft.Xna.Framework;
using StardewValley;

namespace PreventEnergyLoss;

internal static class ToolMethods
{
    public static List<Vector2> GetTilesAffected(Vector2 tileLocation, int power, Farmer who)
    {
        power++;
        List<Vector2> list = new List<Vector2>();
        list.Add(tileLocation);
        Vector2 vector = Vector2.Zero;
        switch (who.FacingDirection)
        {
            case 0:
                if (power >= 6)
                {
                    vector = new Vector2(tileLocation.X, tileLocation.Y - 2f);
                    break;
                }

                if (power >= 2)
                {
                    list.Add(tileLocation + new Vector2(0f, -1f));
                    list.Add(tileLocation + new Vector2(0f, -2f));
                }

                if (power >= 3)
                {
                    list.Add(tileLocation + new Vector2(0f, -3f));
                    list.Add(tileLocation + new Vector2(0f, -4f));
                }

                if (power >= 4)
                {
                    list.RemoveAt(list.Count - 1);
                    list.RemoveAt(list.Count - 1);
                    list.Add(tileLocation + new Vector2(1f, -2f));
                    list.Add(tileLocation + new Vector2(1f, -1f));
                    list.Add(tileLocation + new Vector2(1f, 0f));
                    list.Add(tileLocation + new Vector2(-1f, -2f));
                    list.Add(tileLocation + new Vector2(-1f, -1f));
                    list.Add(tileLocation + new Vector2(-1f, 0f));
                }

                if (power >= 5)
                {
                    for (int num3 = list.Count - 1; num3 >= 0; num3--)
                    {
                        list.Add(list[num3] + new Vector2(0f, -3f));
                    }
                }

                break;
            case 1:
                if (power >= 6)
                {
                    vector = new Vector2(tileLocation.X + 2f, tileLocation.Y);
                    break;
                }

                if (power >= 2)
                {
                    list.Add(tileLocation + new Vector2(1f, 0f));
                    list.Add(tileLocation + new Vector2(2f, 0f));
                }

                if (power >= 3)
                {
                    list.Add(tileLocation + new Vector2(3f, 0f));
                    list.Add(tileLocation + new Vector2(4f, 0f));
                }

                if (power >= 4)
                {
                    list.RemoveAt(list.Count - 1);
                    list.RemoveAt(list.Count - 1);
                    list.Add(tileLocation + new Vector2(0f, -1f));
                    list.Add(tileLocation + new Vector2(1f, -1f));
                    list.Add(tileLocation + new Vector2(2f, -1f));
                    list.Add(tileLocation + new Vector2(0f, 1f));
                    list.Add(tileLocation + new Vector2(1f, 1f));
                    list.Add(tileLocation + new Vector2(2f, 1f));
                }

                if (power >= 5)
                {
                    for (int num2 = list.Count - 1; num2 >= 0; num2--)
                    {
                        list.Add(list[num2] + new Vector2(3f, 0f));
                    }
                }

                break;
            case 2:
                if (power >= 6)
                {
                    vector = new Vector2(tileLocation.X, tileLocation.Y + 2f);
                    break;
                }

                if (power >= 2)
                {
                    list.Add(tileLocation + new Vector2(0f, 1f));
                    list.Add(tileLocation + new Vector2(0f, 2f));
                }

                if (power >= 3)
                {
                    list.Add(tileLocation + new Vector2(0f, 3f));
                    list.Add(tileLocation + new Vector2(0f, 4f));
                }

                if (power >= 4)
                {
                    list.RemoveAt(list.Count - 1);
                    list.RemoveAt(list.Count - 1);
                    list.Add(tileLocation + new Vector2(1f, 2f));
                    list.Add(tileLocation + new Vector2(1f, 1f));
                    list.Add(tileLocation + new Vector2(1f, 0f));
                    list.Add(tileLocation + new Vector2(-1f, 2f));
                    list.Add(tileLocation + new Vector2(-1f, 1f));
                    list.Add(tileLocation + new Vector2(-1f, 0f));
                }

                if (power >= 5)
                {
                    for (int num4 = list.Count - 1; num4 >= 0; num4--)
                    {
                        list.Add(list[num4] + new Vector2(0f, 3f));
                    }
                }

                break;
            case 3:
                if (power >= 6)
                {
                    vector = new Vector2(tileLocation.X - 2f, tileLocation.Y);
                    break;
                }

                if (power >= 2)
                {
                    list.Add(tileLocation + new Vector2(-1f, 0f));
                    list.Add(tileLocation + new Vector2(-2f, 0f));
                }

                if (power >= 3)
                {
                    list.Add(tileLocation + new Vector2(-3f, 0f));
                    list.Add(tileLocation + new Vector2(-4f, 0f));
                }

                if (power >= 4)
                {
                    list.RemoveAt(list.Count - 1);
                    list.RemoveAt(list.Count - 1);
                    list.Add(tileLocation + new Vector2(0f, -1f));
                    list.Add(tileLocation + new Vector2(-1f, -1f));
                    list.Add(tileLocation + new Vector2(-2f, -1f));
                    list.Add(tileLocation + new Vector2(0f, 1f));
                    list.Add(tileLocation + new Vector2(-1f, 1f));
                    list.Add(tileLocation + new Vector2(-2f, 1f));
                }

                if (power >= 5)
                {
                    for (int num = list.Count - 1; num >= 0; num--)
                    {
                        list.Add(list[num] + new Vector2(-3f, 0f));
                    }
                }

                break;
        }

        if (power >= 6)
        {
            list.Clear();
            for (int i = (int)vector.X - 2; (float)i <= vector.X + 2f; i++)
            {
                for (int j = (int)vector.Y - 2; (float)j <= vector.Y + 2f; j++)
                {
                    list.Add(new Vector2(i, j));
                }
            }
        }

        return list;
    }
}
