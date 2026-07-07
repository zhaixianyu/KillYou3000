using KillYou3000.Common.ModSystems;

namespace KillYou3000.MyInterface.MyItem;

public interface IMiddleClickToInteractWithItems : IItemOverrideHover
{
    private static bool canClick = true;

    private bool CanClick()
    {
        if (KeyBindSystem.ItemInteractions.JustPressed)
        {
            if (canClick)
            {
                canClick = false;
                return true;
            }

        }else
        {
            canClick = true;
        }

        return false;
    }

    void Interaction()
    {
    }
    bool IItemOverrideHover.OverrideHover(Terraria.Item[] inventory, int context, int slot)
    {
        if (inventory[slot].ModItem is IMiddleClickToInteractWithItems)
        {
            if (CanClick()) Interaction();
        }
        return false;
    }
}