using Microsoft.Xna.Framework;
using System.Linq;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using KillYou3000.Items.Accessories;
using KillYou3000.Players;

namespace KillYou3000.Content.Hook
{
    /// <summary>
    /// 让仆从本身持续发光，方便玩家识别仆从的位置和状态
    /// </summary>
    public class MinionGlowSystem : GlobalProjectile
    {
        public override bool InstancePerEntity => true;

        public override void AI(Projectile projectile) {
            Player localPlayer = Main.LocalPlayer;
            LifePercentPlayer modPlayer = localPlayer.GetModPlayer<LifePercentPlayer>();

            if (!modPlayer.accessoryList.Any(item => item.ModItem is LifePercentAccessory accessory && accessory.lighting)) {
                return;
            }

            if (IsMinionProjectile(projectile)) {
                Lighting.AddLight(projectile.Center, new Vector3(1f, 1f, 1f));
            }
        }

        private bool IsMinionProjectile(Projectile projectile) {
            return projectile.minion || projectile.sentry || projectile.minionPos >= 0;
        }
    }
}