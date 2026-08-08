using System;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;
using KillYou3000.Buffs;

namespace KillYou3000.Projectiles;

public class BlinkBladeMinion : ModProjectile
{
    // ============ 可调参数 ============
    private const float AttackRange = 1000f;      // 索敌范围：62.5 格（与泰拉棱镜一致）
    private const float AbandonRange = 10f;     // 目标离玩家超过此距离则放弃
    private const int AttackCooldown = 10;        // 攻击冷却：100 tick ≈ 1.67 秒
    // 爆炸范围半径由 ai[2] 从武器传入（BlinkBlade.Shoot 写入），默认 130f（约 8 格）
    private ref float ExplosionRadiusConfig => ref Projectile.ai[2];
    private float ExplosionRadius => ExplosionRadiusConfig > 0 ? ExplosionRadiusConfig : 130f;
    private const float HoverHeight = 90f;        // 悬浮高度（玩家头顶上方像素）
    private const float HoverSpread = 26f;        // 多把剑之间的水平间距
    // ===================================

    private ref float TargetIndex => ref Projectile.ai[1];    // 当前锁定目标的 whoAmI
    private ref float Cooldown => ref Projectile.localAI[0];  // 攻击冷却倒计时
    private ref float Initialized => ref Projectile.localAI[1];// 初始化标志（用于设置错峰冷却）

    public override void SetStaticDefaults()
    {
        Main.projFrames[Type] = 1;
        ProjectileID.Sets.MinionTargettingFeature[Type] = true; // 支持右键锁定
    }

    public override void SetDefaults()
    {
        Projectile.width = 32;
        Projectile.height = 32;
        Projectile.friendly = true;
        Projectile.minion = true;
        Projectile.minionSlots = 1f;
        Projectile.DamageType = DamageClass.Summon;
        Projectile.penetrate = -1;
        Projectile.tileCollide = false;      // 传送攻击无视方块
        Projectile.ignoreWater = true;
        Projectile.netImportant = true;
        Projectile.timeLeft = 18000;
        Projectile.usesLocalNPCImmunity = true;
        Projectile.localNPCHitCooldown = -1;
    }

    public override void AI()
    {
        Player player = Main.player[Projectile.owner];
        if (!player.active || player.dead)
        {
            player.ClearBuff(ModContent.BuffType<BlinkBladeBuff>());
            return;
        }
        if (player.HasBuff<BlinkBladeBuff>())
            Projectile.timeLeft = 2;

        // 统计同类仆从数量，确定本剑在剑阵中的序号（用于环阵排列与错峰攻击）
        int index = 0, count = 0;
        for (int i = 0; i < Main.maxProjectiles; i++)
        {
            Projectile p = Main.projectile[i];
            if (p.active && p.owner == Projectile.owner && p.type == Projectile.type)
            {
                if (i < Projectile.whoAmI)
                    index++;
                count++;
            }
        }

        // 首次运行：按序号设置错峰冷却（多把剑轮流出击，像泰拉棱镜）
        if (Initialized == 0f)
        {
            Initialized = 1f;
            Cooldown = index * (AttackCooldown / Math.Max(count, 1));
        }

        Cooldown -= 1f;

        // ===== 攻击冷却结束：检查目标，死亡则立即锁定新目标攻击 =====
        if (Cooldown <= 0f)
        {
            NPC target = (TargetIndex >= 0 && TargetIndex < Main.maxNPCs) ? Main.npc[(int)TargetIndex] : null;
            bool targetInvalid = target == null 
                || target.Distance(player.Center) > AbandonRange 
                || !target.active || target.life <= 0
                || !target.CanBeChasedBy(this);

            if (targetInvalid)
            {
                // 原目标死亡/失效 → 立即重新索敌并发动攻击
                target = FindTarget(player);
                if (target != null)
                {
                    TargetIndex = target.whoAmI;
                    TeleportAttack(target);
                    return;
                }
            }
            else
            {
                // 目标存活 → 继续攻击当前目标
                TeleportAttack(target);
                return;
            }
        }

        // ===== 待机：星辰细胞式悬浮头顶 =====
        IdleHoverAbovePlayer(player, index, count);
    }

    // 星辰细胞式待机：悬浮在玩家头顶上方，多把剑水平排开，轻微上下浮动
    private void IdleHoverAbovePlayer(Player player, int index, int count)
    {
        // 以玩家为中心，多把剑水平排开
        float spread = (index - (count - 1) * 0.5f) * HoverSpread;
        // 呼吸式悬浮：上下浮动 + 左右轻摆（正弦平滑，多把剑相位错开，灵动但不晃）
        float bob = (float)Math.Sin(Main.GlobalTimeWrappedHourly * 1.3f + index * 1.1f) * 8f;
        float sway = (float)Math.Sin(Main.GlobalTimeWrappedHourly * 0.9f + index * 2.3f) * 6f;
        Vector2 desiredCenter = player.Center + new Vector2(spread + sway, -HoverHeight + bob);

        // 连续比例速度追赶：距离越近速度越小，自然减速归位，无阈值跳变（消除震荡抖动）
        Vector2 toDesired = desiredCenter - Projectile.Center;
        float distance = toDesired.Length();
        float speed = Math.Min(distance * 0.2f, 24f);
        Projectile.velocity = toDesired.SafeNormalize(Vector2.Zero) * speed;
        if (distance < 1f)
            Projectile.velocity = Vector2.Zero;
        Projectile.Center += Projectile.velocity;

        // 悬浮时剑尖朝上竖直，水平移动时轻微倾斜（Lerp 平滑过渡，倾斜幅度减小）
        float targetRotation = Projectile.velocity.X * 0.004f;
        Projectile.rotation = MathHelper.Lerp(Projectile.rotation, targetRotation, 0.06f);

        // 彩虹尘埃拖尾 + 光照（棱镜主题）
        if (Main.rand.NextBool(3))
        {
            Color c = Main.hslToRgb((Main.GlobalTimeWrappedHourly * 0.15f + index * 0.1f) % 1f, 1f, 0.6f);
            Dust d = Dust.NewDustDirect(Projectile.position, Projectile.width, Projectile.height, DustID.RainbowRod);
            d.velocity = Projectile.velocity * 0.3f;
            d.noGravity = true;
            d.color = c;
        }
        Lighting.AddLight(Projectile.Center, new Vector3(0.6f, 0.4f, 0.8f));
    }

    // 索敌：优先玩家右键锁定的目标，否则取范围内最近敌人
    private NPC FindTarget(Player player)
    {
        if (player.HasMinionAttackTargetNPC)
        {
            NPC locked = Main.npc[player.MinionAttackTargetNPC];
            if (locked.active && locked.CanBeChasedBy(this) && locked.Distance(player.Center) <= AttackRange)
                return locked;
        }

        NPC result = null;
        float bestDistance = AttackRange;
        for (int i = 0; i < Main.maxNPCs; i++)
        {
            NPC npc = Main.npc[i];
            if (!npc.active || !npc.CanBeChasedBy(this))
                continue;
            float d = npc.Distance(player.Center);
            if (d <= bestDistance)
            {
                bestDistance = d;
                result = npc;
            }
        }
        return result;
    }

    // 传送攻击：原位消散 → 瞬移到目标位置 → 爆炸范围伤害 → 进入冷却
    private void TeleportAttack(NPC target)
    {
        // 原位消散特效（仅单机/客户端播放，服务器无 UI）
        if (Main.netMode != NetmodeID.Server)
            SpawnFadeDust(Projectile.Center);

        // 直接传送到目标位置
        Projectile.Center = target.Center;
        Projectile.velocity = Vector2.Zero;
        Projectile.netUpdate = true; // 同步位置到各客户端

        // 爆炸：特效 + 范围伤害
        Explode(Projectile.Center);

        // 开始冷却（带错峰，多把剑轮流攻击）
        Cooldown = AttackCooldown;
    }

    // 水波扩散特效 + 正式范围伤害：特效所有端播放，伤害由爆炸子弹幕经 vanilla 碰撞管线结算
    private void Explode(Vector2 center)
    {
        // ===== 视觉特效：仅单机/客户端播放（服务器无 UI，播放无效）=====
        // 注意：多人下客户端与服务器各自独立模拟召唤物 AI，客户端会自行播放本特效，
        // 伤害则由服务器生成的爆炸弹幕裁决后再同步回客户端。
        if (Main.netMode != NetmodeID.Server)
        {
            // 水声（替代爆炸声）
            SoundEngine.PlaySound(SoundID.Splash, center);

            // 从配置半径计算缩放比例（基准 130f ≈ 8格）
            float radiusScale = ExplosionRadius / 130f;

            // ===== 水波涟漪：三层同心半透明水环，从内向外扩散 =====
            for (int ring = 0; ring < 3; ring++)
            {
                float startRadius = (16f + ring * 28f) * radiusScale;      // 每圈初始半径
                int ringCount = 22 + ring * 6;             // 每圈尘埃数（外圈更密）
                int ringAlpha = 60 + ring * 20;            // 半透明程度（外圈更透）
                float ringScale = (1.1f + ring * 0.15f) * radiusScale;
                for (int i = 0; i < ringCount; i++)
                {
                    float angle = MathHelper.TwoPi * i / ringCount;
                    Vector2 dir = new Vector2((float)Math.Cos(angle), (float)Math.Sin(angle));
                    Dust d = Dust.NewDustPerfect(
                        center + dir * startRadius,
                        DustID.Water,
                        dir * (2f + ring * 1.6f) * radiusScale,   // 径向向外扩散，形成水波扩散感
                        ringAlpha,                  // alpha 半透明
                        new Color(140, 210, 255, 128),
                        ringScale);
                    d.noGravity = true;
                }
            }

            // 中心水花：半透明水滴向四周溅开
            for (int i = 0; i < 12; i++)
            {
                Vector2 dir = Vector2.UnitX.RotatedByRandom(MathHelper.TwoPi);
                Dust d = Dust.NewDustPerfect(
                    center + dir * Main.rand.NextFloat(0f, 18f * radiusScale),
                    DustID.Water,
                    dir * Main.rand.NextFloat(2f, 6f) * radiusScale,
                    100,
                    new Color(180, 230, 255, 140),
                    1.3f * radiusScale);
                d.noGravity = true;
            }
        }

        // ===== 范围伤害：生成爆炸弹幕，手动遍历 NPC 施加伤害 =====
        // BlinkBladeExplosion 在 AI 中手动遍历范围内 NPC 并调用 StrikeNPC，
        // 不依赖 vanilla 爆炸/碰撞管线，确保不伤害玩家。
        // 单机/服务器生成（NewProjectile 自动同步到客户端），客户端不生成，避免多人重复结算。
        if (Main.netMode != NetmodeID.MultiplayerClient)
        {
            Projectile.NewProjectileDirect(
                Projectile.GetSource_FromThis(),
                center,
                Vector2.Zero,
                ModContent.ProjectileType<BlinkBladeExplosion>(),
                Projectile.damage,
                Projectile.knockBack,
                Projectile.owner,
                ai0: ExplosionRadius);
        }
    }

    // 传送前的消散尘埃
    private void SpawnFadeDust(Vector2 pos)
    {
        for (int i = 0; i < 12; i++)
        {
            Vector2 dir = Vector2.UnitX.RotatedByRandom(MathHelper.TwoPi);
            Dust d = Dust.NewDustPerfect(pos, DustID.MagicMirror, dir * Main.rand.NextFloat(1f, 4f), 0, default, 1.2f);
            d.noGravity = true;
        }
    }
}
