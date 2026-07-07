using System;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent.Animations;
using Terraria.GameContent.ItemDropRules;
using Terraria.ID;
using Terraria.ModLoader;
using KillYou3000.DamageClasses;
using KillYou3000.Items.Accessories;
using KillYou3000.Players;

namespace KillYou3000.GlobalNPCs
{
    public class LifePercentGlobalNpc : GlobalNPC
    {
        public override bool InstancePerEntity => true;

        // 存储最后一次命中的信息（用于显示特效）
        private Vector2 lastHitPosition;
        private bool lastHitWasBoss;

        public override void ModifyHitByProjectile(NPC npc, Projectile projectile, ref NPC.HitModifiers modifiers)
        {
            ApplyLifePercentDamage(npc, projectile.owner);
        }

        private void ApplyLifePercentDamage(NPC target, int playerIndex)
        {
            if (playerIndex < 0 || playerIndex >= Main.maxPlayers) return;

            Player player = Main.player[playerIndex];
            LifePercentPlayer modPlayer = player.GetModPlayer<LifePercentPlayer>();

            foreach (var item1 in player.GetModPlayer<LifePercentPlayer>().accessoryList)
            {
                if (item1.ModItem is not LifePercentAccessory item) continue;

                if (target.lifeMax > 0 && target.active && !target.friendly)
                {
                    if (Main.netMode != NetmodeID.MultiplayerClient)
                    {
                        ApplyHealingEffect(player, target, modPlayer, item);
                    }

                    if (item.damagePercentage == 0) continue;
                    double damageMultiplier = item.damagePercentage / 100.0D;
                    // 计算1%最大生命值的伤害
                    var extraDamage = (int)(target.lifeMax * (target.boss ? damageMultiplier / 10 : damageMultiplier));

                    // 确保有最小伤害值
                    if (extraDamage < 1) extraDamage = 1;

                    // 应用额外伤害
                    NPC.HitInfo hitInfo = new NPC.HitInfo
                    {
                        Damage = extraDamage,
                        Knockback = 0f,
                        HitDirection = 0,
                        Crit = false,
                        DamageType = ModContent.GetInstance<LifePercentDamage>(),
                        HideCombatText = true,
                    };
                    target.StrikeNPC(hitInfo, true);
                    // 显示伤害特效
                    CombatText.NewText(target.getRect(), new Color(255, 255, 255),
                        $"{extraDamage}", true);
                    if (Main.netMode == NetmodeID.Server)
                    {
                        // 显示伤害特效（客户端）
                        ShowHitEffect(target, extraDamage);
                    }
                }
            }
        }

        // 应用恢复效果
        private void ApplyHealingEffect(Player player, NPC npc, LifePercentPlayer modPlayer, LifePercentAccessory item)
        {
            // 计算已损失生命值
            int missingHealth = player.statLifeMax2 - player.statLife;

            // 如果有损失生命值
            if (missingHealth > 0)
            {
                // 计算恢复量（1%的已损失生命值）
                modPlayer.recoveryHP += missingHealth * (item.revertPercentage / 100);
                if (modPlayer.recoveryHP < 1) return;
                int healAmount = (int)modPlayer.recoveryHP;
                modPlayer.recoveryHP -= healAmount;
                // 应用恢复
                player.statLife += healAmount;

                // 确保不超过最大生命值
                if (player.statLife > player.statLifeMax2)
                    player.statLife = player.statLifeMax2;

                // 添加恢复特效
                if (Main.netMode != NetmodeID.Server)
                {
                    player.HealEffect(healAmount);

                    // 自定义恢复特效
                    for (int i = 0; i < 10; i++)
                    {
                        Dust dust = Dust.NewDustDirect(
                            player.position,
                            player.width,
                            player.height,
                            DustID.GreenTorch,
                            Main.rand.NextFloat(-2f, 2f),
                            Main.rand.NextFloat(-3f, -1f)
                        );
                        dust.noGravity = true;
                        dust.scale = 1.2f;
                    }
                }


                // 网络同步生命值（多人游戏）
                if (Main.netMode == NetmodeID.Server)
                {
                    NetMessage.SendData(MessageID.SpiritHeal, -1, -1, null, player.whoAmI, healAmount);
                }
            }
        }

        public override void OnKill(NPC npc)
        {
            if (!npc.friendly && npc.lifeMax > 5 && !npc.SpawnedFromStatue)
            {
                Player player = Main.player[npc.lastInteraction];

                if (player.active && !player.dead)
                {
                    foreach (var item1 in player.GetModPlayer<LifePercentPlayer>().accessoryList)
                    {
                        if (item1.ModItem is LifePercentAccessory item)
                        {
                            if (!item.isStateActive) continue;
                            // 计算击杀权重
                            int killValue = 1;
                            if (npc.boss) killValue = 5;
                            else if (IsEventEnemy(npc)) killValue = 2;

                            // 增加击杀计数
                            item.killCounter += killValue;

                            // 检查是否达到100击杀
                            if (item.killCounter >= item.maxKillCount)
                            {
                                // 增加生命值加成
                                var i = (int)Math.Max(1, player.statLifeMax2 * 0.01);
                                item.lifeBonus += i;
                                item.killCounter = 0;

                                // 应用新加成
                                // player.statLifeMax2 += 1;
                                // player.statLife += 1;
                                // 显示效果
                                ShowLifeBonusEffect(player, npc, item, i);

                                // 同步饰品数据
                                SyncAccessoryData(player, item);
                            }
                        }
                    }
                }
            }
        }

        // 同步饰品数据
        private void SyncAccessoryData(Player player, LifePercentAccessory accessory)
        {
            if (Main.netMode == NetmodeID.SinglePlayer) return;

            // 创建网络包
            ModPacket packet = Mod.GetPacket();
            packet.Write((byte)1); // 消息类型：更新饰品数据
            packet.Write((byte)player.whoAmI);
            packet.Write(accessory.killCounter);
            packet.Write(accessory.lifeBonus);
            packet.Send();

            // 通知客户端更新饰品
            NetMessage.SendData(MessageID.SyncEquipment, -1, -1, null, player.whoAmI);
        }

        // 显示生命值增加效果
        private void ShowLifeBonusEffect(Player player, NPC target, LifePercentAccessory accessory, int num)
        {
            if (Main.netMode == NetmodeID.Server) return;

            CombatText.NewText(
                player.getRect(),
                new Color(0, 200, 255),
                "饰品生命值 +"+num.ToString(),
                true
            );
            //
            // for (int i = 0; i < 15; i++)
            // {
            //     Dust dust = Dust.NewDustDirect(
            //         player.position, // 位置 (Vector2)
            //         player.width, // 宽度 (int)
            //         player.height, // 高度 (int)
            //         DustID.BlueFairy, // 粒子类型 (int)
            //         Main.rand.NextFloat(-2f, 2f), // X方向速度 (float)
            //         Main.rand.NextFloat(-4f, -2f), // Y方向速度 (float)
            //         0, // Alpha值 (int)
            //         default, // 颜色 (Color)
            //         1.5f // 缩放 (float)
            //     );
            //     dust.noGravity = true;
            //     dust.scale = 1.5f;
            // }
            SoundEngine.PlaySound(SoundID.Item29 with { Pitch = 0.5f }, player.Center);
        }

        // 判断是否为事件敌人
        private bool IsEventEnemy(NPC npc)
        {
            // 示例：海盗入侵、火星暴乱等事件敌人
            return npc.type == NPCID.PirateDeckhand ||
                   npc.type == NPCID.PirateCorsair ||
                   npc.type == NPCID.MartianWalker ||
                   npc.type == NPCID.MartianEngineer ||
                   npc.type == NPCID.DD2DarkMageT1;
        }


        // 显示命中特效
        private void ShowHitEffect(NPC target, int damage)
        {
            // 根据目标类型使用不同粒子效果
            int dustType = lastHitWasBoss ? DustID.Shadowflame : DustID.Blood;
            int dustAmount = lastHitWasBoss ? 15 : 10;
            float dustScale = lastHitWasBoss ? 2.0f : 1.8f;
            Vector2 dustVelocity = new Vector2(0, lastHitWasBoss ? -3f : -2f);

            // 创建粒子
            for (int i = 0; i < dustAmount; i++)
            {
                Dust dust = Dust.NewDustPerfect(
                    lastHitPosition,
                    dustType,
                    new Vector2(Main.rand.NextFloat(-3f, 3f), Main.rand.NextFloat(-5f, -1f)),
                    0,
                    default,
                    dustScale
                );
                dust.noGravity = true;
                dust.velocity = dustVelocity;
            }

            // 显示伤害数字
            Vector2 textPos = lastHitPosition + new Vector2(
                Main.rand.Next(-target.width / 2, target.width / 2),
                Main.rand.Next(-target.height, -target.height / 2)
            );

            // 根据目标类型使用不同颜色
            Color textColor = lastHitWasBoss ? new Color(180, 50, 230) : new Color(255, 50, 50);

            CombatText.NewText(
                new Rectangle((int)textPos.X, (int)textPos.Y, 0, 0),
                textColor,
                $"-{damage}",
                true
            );

            // 添加Boss特殊效果
            if (lastHitWasBoss)
            {
                // 创建冲击波效果
                for (int i = 0; i < 5; i++)
                {
                    Dust dust = Dust.NewDustPerfect(
                        lastHitPosition, // 位置
                        DustID.PurpleTorch, // 粒子类型
                        new Vector2(Main.rand.NextFloat(-5f, 5f), // 速度X
                            Main.rand.NextFloat(-3f, -1f)), // 速度Y
                        0, // Alpha值
                        new Color(180, 50, 230), // 颜色
                        2f // 缩放
                    );
                    dust.noGravity = true;
                }

                // 添加光效
                Lighting.AddLight(lastHitPosition, new Vector3(0.7f, 0.2f, 0.8f));
            }
        }
    }
}