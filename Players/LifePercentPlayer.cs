using System.Collections.Generic;
using System.Linq;
using Terraria;
using Terraria.DataStructures;
using Terraria.ModLoader;
using KillYou3000.MyInterface.MyItem;

namespace KillYou3000.Players
{
    public class LifePercentPlayer : ModPlayer
    {
        public bool hasAccessory;
        public HashSet<Item> accessoryList = new HashSet<Item>();
        public double recoveryHP = 0;
        public int godTime = -1;

        public override void ResetEffects() {
            hasAccessory = false;
            accessoryList.Clear();
            if (godTime > 0) {
                godTime--;
            }
        }

        public override void Load() {
            On_Player.KillMe += my_killMe;
        }

        public override void Unload() {
            On_Player.KillMe -= my_killMe;
        }

        private static void my_killMe(On_Player.orig_KillMe orig, Player player, PlayerDeathReason damageSource,
            double dmg, int hitDirection, bool pvp) {
            var lifePercentPlayer = player.GetModPlayer<LifePercentPlayer>();
            if (lifePercentPlayer.hasAccessory && player.statLife > 1000) {
                // 阻止死亡处理
                player.statLife -= 1000;
                player.dead = false;
                // 添加无敌
                lifePercentPlayer.godTime = 180;
            }
            else orig(player, damageSource, dmg, hitDirection, pvp);
        }

        public override bool HoverSlot(Item[] inventory, int context, int slot) {
            Item item = inventory[slot];
            bool result = false;
            foreach (GlobalItem globalItem in from i in GlobalList<GlobalItem>.Globals
                     where i is IItemOverrideHover
                     select i) {
                result |= ((IItemOverrideHover)globalItem).OverrideHover(inventory, context, slot);
            }

            IItemOverrideHover overrideHover = item.ModItem as IItemOverrideHover;
            if (overrideHover != null && overrideHover.OverrideHover(inventory, context, slot)) {
                result |= overrideHover.OverrideHover(inventory, context, slot);
            }

            return result;
        }

        public override bool ImmuneTo(PlayerDeathReason damageSource, int cooldownCounter, bool dodgeable) {
            if (godTime > 0) return true;
            return base.ImmuneTo(damageSource, cooldownCounter, dodgeable);
        }

        public override void ModifyHurt(ref Player.HurtModifiers modifiers) {
            if (accessoryList.Count > 0) {
                modifiers.SetMaxDamage(1000);
            }
        }
        
        public override bool? CanConsumeBait(Item bait) {
            Main.NewText(bait.Name);
            return false;
        }
    }
}