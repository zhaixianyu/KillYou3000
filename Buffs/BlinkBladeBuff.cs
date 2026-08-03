using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using KillYou3000.Projectiles;

namespace KillYou3000.Buffs;

public class BlinkBladeBuff : ModBuff
{
    public override void SetStaticDefaults()
    {
        Main.buffNoSave[Type] = true;
        Main.buffNoTimeDisplay[Type] = true;
    }

    public override void Update(Player player, ref int buffIndex)
    {
        // 只要还有存活的棱刃仆从就持续保持 buff
        if (player.ownedProjectileCounts[ModContent.ProjectileType<BlinkBladeMinion>()] > 0)
        {
            player.buffTime[buffIndex] = 18000;
        }
        else
        {
            player.DelBuff(buffIndex);
            buffIndex--;
        }
    }
}
