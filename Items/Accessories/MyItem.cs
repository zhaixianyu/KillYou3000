using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;
using KillYou3000.MyInterface.MyItem;

public class MyItem : GlobalItem , IItemOverrideHover
{
    public override bool InstancePerEntity => true;
    public bool isStateActive;
    public int operationalCooling;

    public override void SaveData(Item item, TagCompound tag)
    {
        tag["IsStateActive"] = isStateActive;
    }

    public override void LoadData(Item item, TagCompound tag)
    {
        isStateActive = tag.GetBool("IsStateActive");
    }
    public override void UpdateInventory(Item item, Player player)
    {
        if (operationalCooling > 0) operationalCooling--;
    }
    public bool OverrideHover(Item[] inventory, int context, int slot)
    {
        return false;
    }

    public void switchState()
    {
        if (operationalCooling == 0)
        {
            Main.NewText(isStateActive ? "已关闭" : "已开启");
            isStateActive = !isStateActive;
            operationalCooling = 1000;
            SoundEngine.PlaySound(SoundID.MenuTick);
        }

    }
}