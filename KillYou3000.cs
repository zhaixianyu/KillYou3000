using System.Collections.Generic;
using System.IO;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using KillYou3000.Items.Accessories;

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
                case 1: // 更新饰品数据
                    byte playerId = reader.ReadByte();
                    int slot = reader.ReadInt32();
                    int killCounter = reader.ReadInt32();
                    int lifeBonus = reader.ReadInt32();

                    if (playerId < Main.maxPlayers)
                    {
                        Player player = Main.player[playerId];
                        if (slot >= 0 && slot < player.armor.Length)
                        {
                            Item item = player.armor[slot];
                            if (item.ModItem is LifePercentAccessory accessory)
                            {
                                accessory.killCounter = killCounter;
                                accessory.lifeBonus = lifeBonus;
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
