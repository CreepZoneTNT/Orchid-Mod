using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using OrchidMod.Utilities;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;

namespace OrchidMod.Content.Guardian.Accessories;

public class GuardianDelegateTest : OrchidModGuardianItem
{
	public override string Texture => "OrchidMod/Content/Guardian/Accessories/GuardianTest";
	
	public override void SafeSetDefaults()
	{
		Item.width = 24;
		Item.height = 28;
		Item.value = Item.sellPrice(0, 0, 30, 0);
		Item.rare = ItemRarityID.Quest;
		Item.accessory = true;
	}

	public override void UpdateAccessory(Player player, bool hideVisual)
	{
		OrchidGuardian guardian = player.Guardian();
		guardian.onUseSlamDelegate += OnUseSlam;
		guardian.addGuardDelegate += AddGuard;
		guardian.onBlockFirstDelegate += OnBlockFirst;
		guardian.useSlamDelegate += UseSlam;
		guardian.useGuardDelegate += UseGuard;
		guardian.onHitNPCDelegate += OnHitNPCDelegate;
		guardian.modifyTooltipsDelegate += ModifyTooltipsDelegate;
	}

	public void AddGuard(Player player, OrchidGuardian guardian, int nb)
	{
		if (nb > 1)
			CombatText.NewText(player.getRect(), Color.White, "Bazinga");
	}

	public void OnUseSlam(Player player, OrchidGuardian guardian)
	{
		Projectile proj = Projectile.NewProjectileDirect(player.GetSource_FromAI(), player.Center, Main.rand.NextVector2Unit(MathHelper.Pi, MathHelper.Pi) * 6f, ProjectileID.NailFriendly, guardian.GetGuardianDamage(80), 10f);
		proj.DamageType = ModContent.GetInstance<GuardianDamageClass>();
	}
	
	public void OnBlockFirst(Player player, OrchidGuardian guardian, Projectile anchor, Entity aggressor, ref int toAdd, bool parry)
	{
		if (aggressor is NPC)
		{
			((NPC)aggressor).AddBuff(BuffID.Daybreak, 60);
			
			if (parry && guardian.IsPerfectParry(15, ref toAdd))
			{
				CombatText.NewText(player.getRect(), Color.White, "Bazinga");
				SoundEngine.PlaySound(SoundID.Item4, player.Center);
			}
		}
	}

	public bool UseSlam(Player player, OrchidGuardian guardian, int nb)
	{
		if (!player.immune && Main.rand.NextBool(Math.Max((int)(10f * player.statLife / player.statLifeMax2), 1)))
		{
			CombatText.NewText(player.getRect(), new (225, 200, 255), "Freebie!");
			guardian.GuardianSlam += nb;
		}
		return true;
	}
	
	public bool UseGuard(Player player, OrchidGuardian guardian, int nb)
	{
		if (!player.immune && Main.rand.NextBool(Math.Max((int)(10f * player.statLife / player.statLifeMax2), 1)))
		{
			CombatText.NewText(player.getRect(), new (225, 200, 255), "Freebie!");
			guardian.GuardianGuard += nb;
		}
		return true;
	}

	public void OnHitNPCDelegate(Player player, OrchidGuardian guardian, OrchidModGuardianProjectile proj, NPC target, NPC.HitInfo hit, int damageDone)
	{

		int origGuardStacks = 0;
		int origSlamStacks = 0;

		Projectile projectile = proj.Projectile;
		
		if (proj is GuardianHammerAnchor or GuardianQuarterstaffAnchor && proj.FirstHit)
		{
			if (proj is GuardianHammerAnchor hammer)
			{
				OrchidModGuardianHammer hammerItem = hammer.HammerItem;
				origGuardStacks = hammerItem.GuardStacks;
				origSlamStacks = hammerItem.SlamStacks;
			}
			else if (proj is GuardianQuarterstaffAnchor staff)
			{
				OrchidModGuardianQuarterstaff staffItem = (OrchidModGuardianQuarterstaff)staff.QuarterstaffItem.ModItem;
				origGuardStacks = staffItem.GuardStacks;
				origSlamStacks = staffItem.SlamStacks;
			}
			
			if ((proj is GuardianHammerAnchor && projectile.timeLeft == 600 && projectile.ai[1] < 0 || (projectile.ai[1] > 0 && projectile.ai[0] == 1)) || (proj is GuardianQuarterstaffAnchor && projectile.ai[2] == 0 && !(projectile.ai[0] > 1)))
			{
				if (origGuardStacks > 0) guardian.AddGuard(origGuardStacks);
				if (origSlamStacks > 0) guardian.AddSlam(origSlamStacks);
			}
				
			if (origGuardStacks > 0)
			{
				float halfGuardStacks = origGuardStacks / 2f;
				guardian.GuardianGuard -= (int)halfGuardStacks;
				if (halfGuardStacks % 1 != 0)
					guardian.GuardianGuardRecharging -= halfGuardStacks % 1;
			}
			if (origSlamStacks > 0)
			{
				float halfSlamStacks = origSlamStacks / 2f;
				guardian.GuardianSlam -= (int)halfSlamStacks;
				if (halfSlamStacks % 1 != 0)
					guardian.GuardianSlamRecharging -= halfSlamStacks % 1;
			}
			
			foreach (CombatText combatText in Main.combatText)
			{
				if (combatText.active && combatText.alpha == 1 && !combatText.crit && combatText.dot)
				{
					if (combatText.color == Color.LightSkyBlue && combatText.text == "+" + Language.GetTextValue("Mods.OrchidMod.UI.GuardianItem.Guard", origGuardStacks))
						combatText.text = "+" + Language.GetTextValue("Mods.OrchidMod.UI.GuardianItem.Guard", origGuardStacks / 2f);
					else if (combatText.color == Color.LightCyan && combatText.text == "+" + Language.GetTextValue("Mods.OrchidMod.UI.GuardianItem.Slam", origSlamStacks))
						combatText.text = "+" + Language.GetTextValue("Mods.OrchidMod.UI.GuardianItem.Slam", origSlamStacks / 2f);
				}
			}
		}
	}
	public void ModifyTooltipsDelegate(Player player, OrchidGuardian guardian, OrchidModGuardianItem item, List<TooltipLine> tooltips)
	{
		int origGuardStacks = 0;
		int origSlamStacks = 0;
		
		if (item is OrchidModGuardianHammer hammerItem)
		{
			origGuardStacks = hammerItem.GuardStacks;
			origSlamStacks = hammerItem.SlamStacks;
		}
		else if (item is OrchidModGuardianQuarterstaff staffItem)
		{
			origGuardStacks = staffItem.GuardStacks;
			origSlamStacks = staffItem.SlamStacks;
		}
		
		if (origGuardStacks > 0 || origSlamStacks > 0)
		{
			string TooltipToGet = Mod.GetLocalizationKey("Misc.GuardianGrants");
			if (origGuardStacks > 0) TooltipToGet += "Guard";
			if (origSlamStacks > 0) TooltipToGet += "Slam";
			if (origGuardStacks == origSlamStacks) TooltipToGet += "Same";

			int index = tooltips.FindIndex(tt => tt.Mod == "OrchidMod" && tt.Name == "GuardianGrants");
			TooltipLine grantsLine = tooltips[index];
			if (grantsLine != null)
				grantsLine.Text = Language.GetText(TooltipToGet).Format(origGuardStacks / 2f, origSlamStacks / 2f);
		}
	}
}