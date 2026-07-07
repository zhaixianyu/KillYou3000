using Terraria;
using Terraria.ModLoader;

namespace KillYou3000.DamageClasses
{
    public class LifePercentDamage : DamageClass
    {
        public override bool UseStandardCritCalcs => false;
        
        public override void SetDefaultStats(Player player)
        {
            player.GetCritChance<LifePercentDamage>() += 0;
            // player.GetDamage<LifePercentDamage>() = 1f;
        }
    }
}