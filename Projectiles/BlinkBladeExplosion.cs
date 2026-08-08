using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace KillYou3000.Projectiles;

/// <summary>
/// 范围伤害弹幕：用大碰撞盒 + CanHitPlayer 实现，不使用 Explosive 管线。
/// </summary>
public class BlinkBladeExplosion : ModProjectile
{
    // Fully transparent, no draw
    public override string Texture => "Terraria/Images/Projectile_0";

    public override void SetStaticDefaults()
    {
        ProjectileID.Sets.MinionShot[Type] = true;
    }

    public override void SetDefaults()
    {
        // 从 ai[0] 读取爆炸半径，直接设置为碰撞盒大小
        float radius = Projectile.ai[0] > 0 ? Projectile.ai[0] : 130f;
        int size = (int)(radius * 2);

        Projectile.width = size;
        Projectile.height = size;
        Projectile.friendly = true; // vanilla 原生机制：friendly 弹幕不伤害玩家
        Projectile.penetrate = -1;
        Projectile.tileCollide = false;
        Projectile.ignoreWater = true;
        Projectile.timeLeft = 3;
        Projectile.netImportant = true;
        Projectile.alpha = 255;
        Projectile.DamageType = DamageClass.Summon;
        Projectile.ContinuouslyUpdateDamageStats = true;
        Projectile.usesLocalNPCImmunity = true;
        Projectile.localNPCHitCooldown = -1;
    }

    public override void AI()
    {
        // 不需要 PrepareBombToBlow，碰撞盒已在 SetDefaults 中设好
    }
}
