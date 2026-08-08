using System;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent.Animations;
using Terraria.GameContent.ItemDropRules;
using Terraria.ID;
using Terraria.ModLoader;
using KillYou3000.Buffs;
using KillYou3000.DamageClasses;
using KillYou3000.Items.Accessories;
using KillYou3000.Items.weapon;
using KillYou3000.Players;

namespace KillYou3000.GlobalNPCs
{
    public class LifePercentGlobalNpc : GlobalNPC
    {
        public override bool InstancePerEntity => true;

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

                    if (Main.netMode != NetmodeID.MultiplayerClient)
                    {
                        // ===== 单机/服务器：正式结算伤害 =====
                        // ApplyDamageToNPC 内部走 NPC.StrikeNPC，会自动把命中结果同步给所有客户端。
                        // 单机下它会弹出一个 vanilla 默认色（黄）的伤害数字，这里通过快照对比改为白色
                        // （多人下该数字不会绘制在客户端，由下方 else 分支自行显示白字）。
                        // 若希望隐藏它，把 `color = Color.White` 改为 `active = false`。
                        bool[] combatTextActiveBefore = new bool[Main.combatText.Length];
                        for (int i = 0; i < Main.combatText.Length; i++)
                            combatTextActiveBefore[i] = Main.combatText[i]?.active ?? false;

                        player.ApplyDamageToNPC(target, extraDamage, 0f, 0, false, ModContent.GetInstance<LifePercentDamage>());

                        if (Main.netMode != NetmodeID.Server)
                        {
                            for (int i = 0; i < Main.combatText.Length; i++)
                            {
                                if (!combatTextActiveBefore[i] && Main.combatText[i]?.active == true)
                                {
                                    Main.combatText[i].color = Color.White;
                                    break;
                                }
                            }
                        }
                    }
                    else
                    {
                        // ===== 多人客户端：不结算伤害（由服务器裁决并同步），只做本地视觉反馈 =====
                        // 服务器 ApplyDamageToNPC 的伤害数字不会绘制在客户端（fromNet 同步不显示文字），
                        // 因此由客户端本地显示白色伤害数字，保证多人下也有伤害提示。
                        CombatText.NewText(target.getRect(), Color.White, $"{extraDamage}", true);
                    }

                    // 命中粒子特效：单机与多人客户端播放（服务器无 UI，播放无效）
                    if (Main.netMode != NetmodeID.Server)
                    {
                        ShowHitEffect(target);
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
                    // SpiritHeal：让客户端播放治疗特效（绿色治疗数字）
                    NetMessage.SendData(MessageID.SpiritHeal, -1, -1, null, player.whoAmI, healAmount);
                    // PlayerLifeMana：同步玩家实际生命值（SpiritHeal 只包含特效、不含 HP 数值，
                    // 否则客户端血量条不会更新；tML 1.4.5 中该消息名为 PlayerLifeMana，旧版 PlayerHp 已移除）
                    NetMessage.SendData(MessageID.PlayerLifeMana, -1, -1, null, player.whoAmI);
                }
            }
        }

        public override void OnKill(NPC npc)
        {
            // 击杀计数与生命加成由服务器/单机权威结算：
            // OnKill 在客户端模拟 NPC 死亡时也会触发，若客户端也累加计数，会造成双端数据漂移；
            // 且客户端调用 SyncAccessoryData 会向服务器发送数据包，覆盖服务器的权威数据。
            if (Main.netMode == NetmodeID.MultiplayerClient) return;

            if (!npc.friendly && npc.lifeMax > 5 && !npc.SpawnedFromStatue)
            {
                // 多人下 lastInteraction 可能尚未初始化（默认 255），先做范围保护
                if (npc.lastInteraction < 0 || npc.lastInteraction >= Main.maxPlayers) return;
                Player player = Main.player[npc.lastInteraction];

                if (player != null && player.active && !player.dead)
                {
                    // 处理 LifePercentAccessory 击杀计数
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

                            // 检查是否达到阈值
                            if (item.killCounter >= item.maxKillCount)
                            {
                                // 增加生命值加成
                                var i = (int)Math.Max(1, player.statLifeMax2 * 0.01);
                                item.lifeBonus += i;
                                item.killCounter = 0;

                                // 显示效果（单机直接显示；多人由服务器广播给客户端）
                                ShowLifeBonusEffect(player, i);

                                // 同步饰品数据
                                SyncAccessoryData(player, item);
                            }
                        }
                    }

                    // 处理 BlinkBlade 武器击杀计数
                    // 检查玩家手持物品是否是 BlinkBlade
                    if (player.HeldItem?.ModItem is BlinkBlade heldBlade)
                    {
                        heldBlade.AddKill();
                    }
                    // 也检查玩家的召唤物是否是 BlinkBlade（通过 buff 判断）
                    else if (player.HasBuff(ModContent.BuffType<BlinkBladeBuff>()))
                    {
                        // 找到玩家的 BlinkBlade 武器
                        for (int i = 0; i < player.inventory.Length; i++)
                        {
                            if (player.inventory[i]?.ModItem is BlinkBlade blade)
                            {
                                blade.AddKill();
                                break;
                            }
                        }
                    }
                }
            }
        }

        // 同步饰品数据（仅服务器发送；客户端不发送，避免客户端数据覆盖服务器权威数据）
        private void SyncAccessoryData(Player player, LifePercentAccessory accessory)
        {
            if (Main.netMode != NetmodeID.Server) return;

            // 找到该饰品在玩家装备栏中的槽位（接收端需要按槽位定位饰品）
            int slot = -1;
            for (int i = 0; i < player.armor.Length; i++)
            {
                if (player.armor[i]?.ModItem == accessory)
                {
                    slot = i;
                    break;
                }
            }
            if (slot < 0) return;

            // 创建网络包（与 KillYou3000.HandlePacket 的 case 1 读取格式严格对应）
            ModPacket packet = Mod.GetPacket();
            packet.Write((byte)1); // 消息类型：更新饰品数据
            packet.Write((byte)player.whoAmI);
            packet.Write((byte)slot);
            packet.Write(accessory.killCounter);
            packet.Write(accessory.lifeBonus);
            packet.Send();
        }

        // 显示生命值增加效果：单机直接显示；多人由服务器广播，客户端收到后显示
        private void ShowLifeBonusEffect(Player player, int num)
        {
            if (Main.netMode == NetmodeID.Server)
            {
                // 广播给所有客户端（消息类型 2：生命加成特效）
                ModPacket packet = Mod.GetPacket();
                packet.Write((byte)2);
                packet.Write((byte)player.whoAmI);
                packet.Write(num);
                packet.Send();
                return;
            }

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


        // 显示命中粒子特效（仅在单机/客户端调用；服务器无 UI，调用无效）
        // 伤害数字不在这里显示：单机由 vanilla 数字改白、多人客户端由 ApplyLifePercentDamage 本地显示，
        // 这里只负责粒子，避免与伤害数字重复。
        private void ShowHitEffect(NPC target)
        {
            // 以目标中心偏上为命中点
            Vector2 hitPosition = target.Center + new Vector2(0f, -target.height * 0.25f);
            bool isBoss = target.boss;

            // 根据目标类型使用不同粒子效果
            int dustType = isBoss ? DustID.Shadowflame : DustID.Blood;
            int dustAmount = isBoss ? 15 : 10;
            float dustScale = isBoss ? 2.0f : 1.8f;
            float dustVelocityY = isBoss ? -3f : -2f;

            // 创建粒子
            for (int i = 0; i < dustAmount; i++)
            {
                Dust dust = Dust.NewDustPerfect(
                    hitPosition,
                    dustType,
                    new Vector2(Main.rand.NextFloat(-3f, 3f), Main.rand.NextFloat(-5f, -1f)),
                    0,
                    default,
                    dustScale
                );
                dust.noGravity = true;
                dust.velocity = new Vector2(0f, dustVelocityY);
            }

            // 添加Boss特殊效果：紫色冲击波尘埃 + 光效
            if (isBoss)
            {
                for (int i = 0; i < 5; i++)
                {
                    Dust dust = Dust.NewDustPerfect(
                        hitPosition,
                        DustID.PurpleTorch,
                        new Vector2(Main.rand.NextFloat(-5f, 5f), Main.rand.NextFloat(-3f, -1f)),
                        0,
                        new Color(180, 50, 230),
                        2f
                    );
                    dust.noGravity = true;
                }

                Lighting.AddLight(hitPosition, new Vector3(0.7f, 0.2f, 0.8f));
            }
        }
    }
}