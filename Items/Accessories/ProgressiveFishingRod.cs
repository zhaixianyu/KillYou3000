using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;
using KillYou3000.Config;

namespace KillYou3000.Items.Accessories
{
    public class ProgressiveFishingRod : ModItem
    {
        private static bool loadAutoFishing;
        public static ClientConfig ClientConfig;
        public static ServerConfig ServerConfig;
        private int AutocastDelay;
        private int PullTimer;
        private Vector2? recordedPosition = null;
        Point CastPosition;
        bool modUpData;
        bool AutoThrowTheRod;
        int bobberCount;
        int bonus;

        public override void SetStaticDefaults() {
            ItemID.Sets.CanFishInLava[Item.type] = true;
        }

        public override void SetDefaults() {
            Item.CloneDefaults(ItemID.GoldenFishingRod); // 继承金钓竿属性
            Item.fishingPole = 35; // 基础渔力（后续动态覆盖）
            Item.shootSpeed = 20f; // 抛竿速度
            Item.shoot = ProjectileID.BobberGolden; // 浮标类型
            Item.rare = ItemRarityID.Purple;
        }
        
        public override void ModifyTooltips(List<TooltipLine> tooltips) {
            foreach (TooltipLine line in tooltips) {
                if (line.Text.Contains("{0}")) line.Text = line.Text.Replace("{0}", bonus.ToString());
                if (line.Text.Contains("{1}")) line.Text = line.Text.Replace("{1}", bobberCount.ToString());
            }
        }
        
        public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position,
            Vector2 velocity, int type, int damage, float knockback) {
            for (int i = 0; i < bobberCount; i++) {
                Projectile.NewProjectile(source, position, velocity.RotatedByRandom(MathHelper.ToRadians(18f)), type, 0,
                    0f, player.whoAmI, ai2: Main.rand.Next(2));
            }

            return base.Shoot(player, source, position, velocity, type, damage, knockback);
        }

        public override void HoldItem(Player player) {
            if (loadAutoFishing || player.whoAmI != Main.myPlayer || !AutoThrowTheRod) return;
            if (modUpData) {
                modUpData = false;
                return;
            }


            // 收杆
            if (PullTimer > 0) {
                PullTimer--;
                if (PullTimer == 0) {
                    UseItem2(player);
                }
            }

            // 抛竿       
            AutocastDelay--;
            if (AutocastDelay > 0 || CheckBobbersActive(player.whoAmI)) {
                return;
            }

            var mouseX = Main.mouseX;
            var mouseY = Main.mouseY;
            if (CastPosition != default) {
                Main.mouseX = CastPosition.X - (int)Main.screenPosition.X;
                Main.mouseY = CastPosition.Y - (int)Main.screenPosition.Y;
            }

            UseItem2(player);
            AutocastDelay = 10;

            if (CastPosition != default) {
                Main.mouseX = mouseX;
                Main.mouseY = mouseY;
            }
        }

        public static bool CheckBobbersActive(int whoAmI) {
            foreach (var _ in from p in Main.projectile where p.active && p.owner == whoAmI && p.bobber select p) {
                return true;
            }

            return false;
        }

        public override void Load() {
            loadAutoFishing = ModLoader.TryGetMod("Autofish", out _);
            On_Player.ItemCheck_CheckCanUse += my_ItemCheck_CheckCanUse;
            On_Player.ItemCheck_CheckFishingBobber_PickAndConsumeBait += my_ItemCheck_CheckFishingBobber_PickAndConsumeBait;
            if (loadAutoFishing) return;
            On_Projectile.FishingCheck += my_Projectile_FishingCheck;
        }

        public override void Unload() {
            loadAutoFishing = ModLoader.TryGetMod("Autofish", out _);
            On_Player.ItemCheck_CheckCanUse -= my_ItemCheck_CheckCanUse;
            if (loadAutoFishing) return;
            On_Projectile.FishingCheck -= my_Projectile_FishingCheck;
        }

        public static void my_ItemCheck_CheckFishingBobber_PickAndConsumeBait(On_Player.orig_ItemCheck_CheckFishingBobber_PickAndConsumeBait orig, Player self, Projectile bobber, out bool pullTheBobber, out int baitTypeUsed) {
            if (GetMyItem(out _)) {
                var player = Main.LocalPlayer;
                pullTheBobber = false;
                baitTypeUsed = 0;
                int num = -1;
                for (int i = 54; i < 58; i++) {
                    if (player.inventory[i].stack > 0 && player.inventory[i].bait > 0) {
                        num = i;
                        break;
                    }
                }

                if (num == -1) {
                    for (int j = 0; j < 50; j++) {
                        if (player.inventory[j].stack > 0 && player.inventory[j].bait > 0) {
                            num = j;
                            break;
                        }
                    }
                }
                if (num <= -1)
                    return;
                
                Item item = player.inventory[num];
                baitTypeUsed = item.type;
                pullTheBobber = true;
                return;
                //原有的方法会在后面计算诱饵消耗
            }
            orig(self, bobber, out pullTheBobber, out baitTypeUsed);
        }

        private static void my_Projectile_FishingCheck(On_Projectile.orig_FishingCheck orig, Projectile self) {
            
            if (GetMyItem(out var myItem)) {
                myItem.PullTimer = (int)(ClientConfig.PullingDelay * 60 + 1);
            }
            orig(self);
        }

        // 左键点击切换状态
        private static bool my_ItemCheck_CheckCanUse(On_Player.orig_ItemCheck_CheckCanUse origItemCheckCheckCanUse,
            Player self, Item item) {
            if (item.ModItem is ProgressiveFishingRod progressiveFishingRod && Main.mouseLeft &&
                Main.mouseLeftRelease) {
                progressiveFishingRod.CalculateProgressBonus();
                progressiveFishingRod.Item.fishingPole = 35 + progressiveFishingRod.bonus;
                progressiveFishingRod.AutoThrowTheRod = !progressiveFishingRod.AutoThrowTheRod;
                if (!loadAutoFishing && progressiveFishingRod.AutoThrowTheRod && ClientConfig.PullingDelay <= -0.09f) {
                    progressiveFishingRod.CastPosition = Main.MouseWorld.ToPoint();
                }
            }

            return origItemCheckCheckCanUse(self, item);
        }

        public static bool GetMyItem(out ProgressiveFishingRod myItem) {
            Player player = Main.LocalPlayer;
            Item item = player.inventory[player.selectedItem];
            if (item.ModItem is ProgressiveFishingRod progressiveFishingRod) {
                myItem = progressiveFishingRod;
                return true;
            }
            myItem = null;
            return false;
        }

        private void UseItem2(Player player) {
            player.controlUseItem = true;
            player.releaseUseItem = true;
            modUpData = true;
            player.ItemCheck();
        }

        private void CalculateProgressBonus() {
            // // 阶段1：肉山前
            // if (NPC.downedBoss1) bonus += 10; // 克苏鲁之眼
            // if (NPC.downedBoss2) bonus += 15; // 世界吞噬怪/克苏鲁之脑
            // if (NPC.downedQueenBee) bonus += 10; // 蜂后
            //
            // // 阶段2：机械三王
            // if (NPC.downedMechBoss1) bonus += 20; // 双子魔眼
            // if (NPC.downedMechBoss2) bonus += 20; // 毁灭者
            // if (NPC.downedMechBoss3) bonus += 20; // 机械骷髅王
            //
            // // 阶段3：世纪之花后
            // if (NPC.downedPlantBoss) bonus += 30; // 世纪之花
            //
            // // 阶段4：月亮领主
            // if (NPC.downedMoonlord) bonus += 50;
            var totalBossUnlockPercentage = GetTotalBossUnlockPercentage();
            bonus = (int)(totalBossUnlockPercentage * ServerConfig.MaximumFishingCapacity);
            bobberCount = (int)(totalBossUnlockPercentage * ServerConfig.MaximumNumberOfFishingLines);
        }

        public override void AddRecipes() {
            CreateRecipe().Register();
        }


        public static float GetTotalBossUnlockPercentage() {
            int unlockedBosses = 0;
            int totalBosses = 0;

            for (int npcType = 1; npcType < NPCLoader.NPCCount; npcType++) {
                try {
                    NPC npc = new NPC();
                    npc.SetDefaults(npcType);

                    if (npc.boss && !npc.friendly) {
                        totalBosses++;
                        // 检查玩家是否解锁了该Boss的图鉴
                        if (Main.BestiaryTracker.Sights.GetWasNearbyBefore(npc) ||
                            Main.BestiaryTracker.Kills.GetKillCount(npc) > 0) {
                            unlockedBosses++;
                        }
                    }
                }
                catch {
                    // 忽略无效NPC类型
                }
            }

            return totalBosses == 0 ? 0f : (float)unlockedBosses / totalBosses;
        }
    }
}