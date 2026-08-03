// Decompiled with JetBrains decompiler
// Type: MoreEmpressBlade.Weapons.EmpressBladeLV1
// Assembly: MoreEmpressBlade, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null
// MVID: 183EAAC6-7C02-4A6E-BE96-FC0B12F69996
// Assembly location: C:\Users\11419\Documents\My Games\Terraria\tModLoader\ModReader\MoreEmpressBlade\MoreEmpressBlade.dll

#nullable disable
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Audio;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;

namespace KillYou3000.Items.weapon;

public class Empress : ModItem
{
    public override void SetStaticDefaults()
    {
        ItemID.Sets.GamepadWholeScreenUseRange[this.Item.type] = true;
        ItemID.Sets.LockOnIgnoresCollision[this.Item.type] = true;
        ItemID.Sets.StaffMinionSlotsRequired[this.Type] = 1f;
    }

    public override void SetDefaults()
    {
        this.Item.damage = 8;
        this.Item.knockBack = 0.1f;
        this.Item.mana = 4;
        ((Entity) this.Item).width = 32 /*0x20*/;
        ((Entity) this.Item).height = 32 /*0x20*/;
        this.Item.useTime = 36;
        this.Item.useAnimation = 36;
        this.Item.useStyle = 1;
        this.Item.value = Item.sellPrice(0, 1, 0, 0);
        this.Item.rare = 9;
        this.Item.UseSound = new SoundStyle?(SoundID.Item44);
        this.Item.noMelee = true;
        this.Item.DamageType = DamageClass.Summon;
        this.Item.buffType = 322;
        this.Item.shoot = 946;
    }

    public override bool Shoot(
        Player player,
        EntitySource_ItemUse_WithAmmo source,
        Vector2 position,
        Vector2 velocity,
        int type,
        int damage,
        float knockback)
    {
        player.AddBuff(this.Item.buffType, 2, true, false);
        Projectile projectile = Projectile.NewProjectileDirect((IEntitySource) source, position, velocity, type, damage, knockback, Main.myPlayer, 0.0f, 0.0f, 0.0f);
        projectile.scale = 0.5f;
        projectile.localNPCHitCooldown = -1;
        projectile.extraUpdates = 3;
        
        return false;
    }

    public override void AddRecipes()
    {
        this.CreateRecipe(1).AddIngredient(75, 5).Register();
    }
}