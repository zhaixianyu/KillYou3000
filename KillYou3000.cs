using System.Collections.Generic;
using System.IO;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;
using KillYou3000.Items.Accessories;
using KillYou3000.Items.weapon;

namespace KillYou3000
{
    // Please read https://github.com/tModLoader/tModLoader/wiki/Basic-tModLoader-Modding-Guide#mod-skeleton-contents for more information about the various files in a mod.
    public class KillYou3000 : Mod
    {
        public const string AssetPath = $"{nameof(KillYou3000)}/Assets/";
        public override void HandlePacket(BinaryReader reader, int whoAmI)
        {
            byte messageType = reader.ReadByte();

            switch (messageType)
            {
                case 1: // 更新饰品数据（服务器 → 客户端）
                    byte playerId = reader.ReadByte();
                    byte slot = reader.ReadByte();
                    int killCounter = reader.ReadInt32();
                    int lifeBonus = reader.ReadInt32();

                    if (playerId < Main.maxPlayers)
                    {
                        Player player = Main.player[playerId];
                        if (slot < player.armor.Length && player.armor[slot]?.ModItem is LifePercentAccessory accessory)
                        {
                            accessory.killCounter = killCounter;
                            accessory.lifeBonus = lifeBonus;
                        }
                    }
                    break;
                case 2: // 生命加成特效（服务器 → 客户端）
                    byte pId = reader.ReadByte();
                    int bonusNum = reader.ReadInt32();

                    if (pId < Main.maxPlayers)
                    {
                        Player p = Main.player[pId];
                        if (Main.netMode == NetmodeID.MultiplayerClient && p != null && p.active)
                        {
                            CombatText.NewText(p.getRect(), new Color(0, 200, 255), "饰品生命值 +" + bonusNum, true);
                            SoundEngine.PlaySound(SoundID.Item29 with { Pitch = 0.5f }, p.Center);
                        }
                    }
                    break;
                case 3: // 同步配置（客户端 → 服务器）
                    byte cfgPlayer = reader.ReadByte();
                    byte cfgSlot = reader.ReadByte();
                    byte itemType = reader.ReadByte(); // 0=LifePercentAccessory, 1=BlinkBlade

                    if (cfgPlayer < Main.maxPlayers)
                    {
                        Player cfgP = Main.player[cfgPlayer];
                        
                        if (itemType == 0) // LifePercentAccessory
                        {
                            double cfgDamage = reader.ReadDouble();
                            double cfgRevert = reader.ReadDouble();
                            int cfgMaxKill = reader.ReadInt32();

                            if (cfgSlot < cfgP.armor.Length && cfgP.armor[cfgSlot]?.ModItem is LifePercentAccessory acc)
                            {
                                acc.damagePercentage = cfgDamage;
                                acc.revertPercentage = cfgRevert;
                                acc.maxKillCount = cfgMaxKill;
                            }
                        }
                        else if (itemType == 1) // BlinkBlade
                        {
                            double cfgGrowthPct = reader.ReadDouble();
                            int cfgMaxKillCount = reader.ReadInt32();
                            int cfgKillCounter = reader.ReadInt32();
                            int cfgItemDamage = reader.ReadInt32();
                            float cfgExplosionRadius = reader.ReadSingle();

                            if (cfgSlot < cfgP.inventory.Length && cfgP.inventory[cfgSlot]?.ModItem is BlinkBlade blade)
                            {
                                blade.growthPercentage = cfgGrowthPct;
                                blade.maxKillCount = cfgMaxKillCount;
                                blade.killCounter = cfgKillCounter;
                                blade.Item.damage = cfgItemDamage;
                                blade.explosionRadius = cfgExplosionRadius;
                            }
                        }
                    }
                    break;
            }
        }
        public override void Load()
        {
            // 加载时执行的初始化代码
            Logger.Info("要你命3000 Mod已加载！");
        }

        public override void Unload()
        {
            // 卸载时执行的清理代码
            Logger.Info("要你命3000 Mod已卸载！");
        }

        public override void PostSetupContent()
        {
            // 内容加载完成后执行的代码
            Logger.Info("Mod内容加载完成！");
        }
        
        // ===================== 系统配置 =====================
        public class WuhuSystem : ModSystem
        {
            public override void Load()
            {
                // 注册自定义伤害类型
                // DamageClassLoader.AddDamageClass(ModContent.GetInstance<LifePercentDamage>());
            }

            public override void Unload()
            {
                // 清理自定义伤害类型
            }

            public override void PostAddRecipes()
            {
                // 配方添加完成后执行的代码
            }

            public override void ModifyGameTipVisibility(IReadOnlyList<GameTipData> gameTips)
            {
                // 修改游戏提示（可选）
            }
            // public override void AddRecipes()
            // {
            //     Recipe recipe = Recipe.Create(ModContent.ItemType<LifePercentAccessory>());
            //     recipe.AddIngredient(ItemID.LifeCrystal, 5); // 5个生命水晶
            //     recipe.Register();
            // }
        }

        // ===================== 内容配置 =====================
        // public class WuhuContent : ModContent
        // {
        //     public override void Load()
        //     {
        //         // 加载内容资源（如图片、音效）
        //         // 自动处理，通常不需要额外代码
        //     }
        // }

        // ===================== 全局初始化 =====================
        public class GlobalInitializer : ILoadable
        {
            public void Load(Mod mod)
            {
                // 注册全局NPC处理器
                // GlobalNPC.RegisterGlobalNPC<LifePercentGlobalNPC>();

                // 注册玩家效果处理器
                // ModContent.GetInstance<LifePercentPlayer>();
            }

            public void Unload()
            {
                // 卸载时的清理
            }
        }

    }
}
