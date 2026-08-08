using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using Terraria;
using Terraria.Audio;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;
using KillYou3000.Buffs;
using KillYou3000.Common.Config;
using KillYou3000.Items.Accessories;
using KillYou3000.MyInterface.MyItem;
using KillYou3000.Projectiles;

namespace KillYou3000.Items.weapon;

public class BlinkBlade : ModItem, IConfigurableItem, IMiddleClickToInteractWithItems
{
    // 成长属性
    public int killCounter;           // 当前击杀计数
    // bonusDamage 已移除：成长直接加到 Item.damage，所有下游（召唤物/爆炸/tooltip）自动继承
    public int maxKillCount = 100;     // 成长阈值（击杀数）
    public int baseDamage = 1;
    public double growthPercentage = 1.0; // 每次成长增加当前伤害的百分比
    public float explosionRadius = 130f;  // 爆炸范围半径（像素），默认约8格

    public override void SetStaticDefaults()
    {
        ItemID.Sets.GamepadWholeScreenUseRange[Type] = true;
        ItemID.Sets.LockOnIgnoresCollision[Type] = true;
        ItemID.Sets.StaffMinionSlotsRequired[Type] = 1f;
    }

    public override void SetDefaults()
    {
        Item.damage = baseDamage;
        Item.knockBack = 4f;
        Item.mana = 10;
        Item.width = 32;
        Item.height = 32;
        Item.useTime = 36;
        Item.useAnimation = 36;
        Item.useStyle = ItemUseStyleID.Swing;
        Item.value = Item.sellPrice(0, 20, 0, 0);
        Item.rare = ItemRarityID.Yellow;
        Item.UseSound = SoundID.Item44;
        Item.noMelee = true;
        Item.DamageType = DamageClass.Summon;
        Item.buffType = ModContent.BuffType<BlinkBladeBuff>();
        Item.shoot = ModContent.ProjectileType<BlinkBladeMinion>();
    }

    public override void SaveData(TagCompound tag)
    {
        tag["killCounter"] = killCounter;
        tag["itemDamage"] = Item.damage; // 直接保存含成长的伤害
        tag["maxKillCount"] = maxKillCount;
        tag["growthPercentage"] = growthPercentage;
        tag["explosionRadius"] = explosionRadius;
    }

    public override void LoadData(TagCompound tag)
    {
        killCounter = tag.GetInt("killCounter");
        maxKillCount = tag.GetInt("maxKillCount");
        growthPercentage = tag.GetDouble("growthPercentage");
        explosionRadius = tag.GetFloat("explosionRadius");
        if (explosionRadius <= 0) explosionRadius = 130f;
        Item.damage = Math.Max(baseDamage, tag.GetInt("itemDamage"));
    }

    public override void ModifyTooltips(List<TooltipLine> tooltips)
    {
        foreach (TooltipLine line in tooltips)
        {
            if (line.Text.Contains("{kill}"))
                line.Text = line.Text.Replace("{kill}", killCounter.ToString());
            if (line.Text.Contains("{maxKill}"))
                line.Text = line.Text.Replace("{maxKill}", maxKillCount.ToString());
            if (line.Text.Contains("{bonus}"))
                line.Text = line.Text.Replace("{bonus}", (Item.damage - baseDamage).ToString());
            if (line.Text.Contains("{rate}"))
                line.Text = line.Text.Replace("{rate}", growthPercentage.ToString("F1"));
            if (line.Text.Contains("{radius}"))
                line.Text = line.Text.Replace("{radius}", (explosionRadius / 16f).ToString("F1"));
        }
    }

    // 增加击杀计数（由 GlobalNPC.OnKill 调用）
    // 达到阈值时增加当前伤害的 growthPercentage%（至少为 1）
    // 成长直接加到 Item.damage，所有下游自动继承
    public void AddKill()
    {
        if (growthPercentage == 0) {
            return;
        }
        killCounter++;
        if (killCounter >= maxKillCount)
        {
            // 达到阈值，计算成长量（基于当前 Item.damage）
            int growthAmount = (int)(Item.damage * growthPercentage / 100.0);
            growthAmount = Math.Abs(growthAmount);
            int max = Math.Max(1,growthAmount);
            growthAmount = growthPercentage > 0 ? max : -max;
            Item.damage += growthAmount; // 直接增加基础伤害
            Item.damage = Math.Max(baseDamage, Item.damage);
            killCounter = 0;

            // 显示成长提示（单机/客户端）
            if (Main.netMode != NetmodeID.Server)
            {
                Player player = Main.player[Main.myPlayer];
                CombatText.NewText(player.getRect(), new Color(255, 200, 0), 
                    $"武器成长！伤害 +{growthAmount}", true, false);
                SoundEngine.PlaySound(SoundID.Item29 with { Pitch = 0.8f }, player.Center);
            }
        }
    }

    public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 velocity, int type, int damage, float knockback)
    {
        // Item.damage 已包含成长加成，直接使用
        player.AddBuff(Item.buffType, 2);
        int proj = Projectile.NewProjectile(source, position, velocity, type, Item.damage, knockback, player.whoAmI);
        // 将爆炸半径写入召唤物的 ai[2]，供 BlinkBladeMinion 读取
        if (proj >= 0 && proj < Main.maxProjectiles)
            Main.projectile[proj].ai[2] = explosionRadius;
        return false;
    }

    public override void AddRecipes()
    {
        CreateRecipe()
            .AddIngredient(ItemID.LifeCrystal, 5)
            .AddIngredient(ItemID.FallenStar, 5)
            .Register();
    }

    #region IConfigurableItem 实现

    public string ConfigTitle => "湮灭棱刃配置";

    public List<ConfigField> GetConfigFields()
    {
        return new List<ConfigField>
        {
            new ConfigField
            {
                Label = "成长百分比(%)",
                Key = "growthPercentage",
                FieldType = ConfigFieldType.Double,
                Value = growthPercentage,
                OnChange = v => growthPercentage = (double)v
            },
            new ConfigField
            {
                Label = "成长阈值(击杀数)",
                Key = "maxKillCount",
                FieldType = ConfigFieldType.Int,
                Value = maxKillCount,
                OnChange = v => maxKillCount = (int)v
            },
            new ConfigField
            {
                Label = "爆炸半径(像素)",
                Key = "explosionRadius",
                FieldType = ConfigFieldType.Double,
                Value = (double)explosionRadius,
                OnChange = v => explosionRadius = (float)(double)v
            }
        };
    }

    public void OnConfigChanged()
    {
        // 多人游戏：同步配置到服务器
        if (Main.netMode == NetmodeID.MultiplayerClient)
        {
            SyncConfigToServer();
        }
    }

    private void SyncConfigToServer()
    {
        // 找到该武器在玩家物品栏中的槽位
        Player player = Main.player[Main.myPlayer];
        int slot = -1;
        for (int i = 0; i < player.inventory.Length; i++)
        {
            if (player.inventory[i]?.ModItem == this)
            {
                slot = i;
                break;
            }
        }
        if (slot < 0) return;

        // 发送配置同步包（消息类型 3）
        ModPacket packet = Mod.GetPacket();
        packet.Write((byte)3); // 消息类型：同步配置
        packet.Write((byte)player.whoAmI);
        packet.Write((byte)slot);
        packet.Write((byte)1); // 物品类型标识：1=BlinkBlade
        packet.Write(growthPercentage);
        packet.Write(maxKillCount);
        packet.Write(killCounter);
        packet.Write(Item.damage); // 同步含成长的伤害
        packet.Write(explosionRadius);
        packet.Send();
    }

    #endregion

    #region IMiddleClickToInteractWithItems 实现

    public void Interaction()
    {
        if (Main.keyState.IsKeyDown(Keys.LeftControl))
        {
            LifePercentUISystem.ShowUI(this);
        }
    }

    #endregion
}
