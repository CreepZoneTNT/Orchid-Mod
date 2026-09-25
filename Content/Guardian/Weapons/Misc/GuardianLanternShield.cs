using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using OrchidMod.Common;
using OrchidMod.Common.Global.Items;
using OrchidMod.Content.General.Prefixes;
using OrchidMod.Utilities;
using ReLogic.Content;
using System.Collections.Generic;
using System.Linq;
using OrchidMod.Content.Guardian.Projectiles.Misc;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using static Terraria.NPC;

namespace OrchidMod.Content.Guardian.Weapons.Misc
{
	public class GuardianLanternShield : OrchidModGuardianParryItem
	{
		public float StrikeVelocity; // Initial speed of the punches
		/// <summary> Jab and slam animation speed multiplier. Also affected by melee speed, but not by usetime. </summary>
		public float JabDamage;

		public int ParryDuration;
		public float BlockDurationMult;

		public int TorchIndex = -1;

		public override int AnchorType => ModContent.ProjectileType<GuardianLanternShieldAnchor>();

		public sealed override void SetDefaults()
		{
			Item.DamageType = ModContent.GetInstance<GuardianDamageClass>();
			Item.noMelee = true;
			Item.autoReuse = true;
			Item.maxStack = 1;
			Item.noUseGraphic = true;
			Item.UseSound = SoundID.Item7;
			Item.useStyle = ItemUseStyleID.Thrust;

			Item.width = 48;
			Item.height = 48;
			Item.value = Item.sellPrice(0, 5);
			Item.rare = ItemRarityID.LightRed;
			Item.useTime = Item.useAnimation = 45;
			// Item.shootSpeed = 30f;
			Item.knockBack = 13f;
			Item.damage = 447;
			ParryDuration = 30;
			BlockDurationMult = 4.5f;
			
			JabDamage = 0.25f;
			StrikeVelocity = 15;

			OrchidGlobalItemPerEntity orchidItem = Item.GetGlobalItem<OrchidGlobalItemPerEntity>();
			orchidItem.guardianWeapon = true;
		}

		public override bool AltFunctionUse(Player player) => true;

		public override void OnParry(Player player, OrchidGuardian guardian, Entity aggressor, Projectile anchor)
		{
			
			bool swap = ModContent.GetInstance<OrchidClientConfig>().GuardianSwapGauntletInputs;
			bool punchHold = swap ? Main.mouseRight : Main.mouseLeft;
			bool guardHold = swap ? Main.mouseLeft : Main.mouseRight;
			
			anchor.ai[0] = 0;
			anchor.ai[1] = guardHold ? 1f : punchHold ? -1f : 0f;
		}
		
		public override bool WeaponPrefix() => true;
		
		public override bool CanUseItem(Player player)
		{
			if (player.whoAmI == Main.myPlayer && !player.cursed)
			{
				if (player.ownedProjectileCounts[AnchorType] > 0)
				{
					OrchidGuardian guardian = player.Guardian();
					Projectile proj = Main.projectile.FirstOrDefault(i => i.active && i.owner == player.whoAmI && i.type == AnchorType);
					if (proj?.ModProjectile is GuardianLanternShieldAnchor anchor)
					{
						bool swap = ModContent.GetInstance<OrchidClientConfig>().GuardianSwapGauntletInputs;
						bool punchHold = swap ? Main.mouseRight : Main.mouseLeft;
						bool guardHold = swap ? Main.mouseLeft : Main.mouseRight;

						if (guardian.GuardianItemCharge == 0f && proj.ai[0] == 0f)
						{
							if (guardHold)
							{
								proj.ai[0] = 0f;
								proj.ai[1] = 1f;
								proj.ai[2] = 0f;
								anchor.NeedNetUpdate = true;
								guardian.GuardianItemCharge++;
								
							}
							else if (punchHold)
							{
								proj.ai[0] = 0f;
								proj.ai[1] = -1f;
								proj.ai[2] = 0f;
								anchor.NeedNetUpdate = true;
								guardian.GuardianItemCharge++;
								
							}
						}
					}
				}
			}
			return false;
		}

		public override void HoldItem(Player player)
		{
			int projectileType = ModContent.ProjectileType<GuardianLanternShieldAnchor>();
			player.Guardian().GuardianDisplayUI = 300;

			if (player.ownedProjectileCounts[projectileType] != 1)
			{
				foreach (Projectile projectile in Main.projectile)
				{
					if (projectile.active && projectile.owner == player.whoAmI && projectile.type == projectileType)
						projectile.Kill();
				}

				int index = Projectile.NewProjectile(Item.GetSource_FromThis(), player.Center.X, player.Center.Y, 0f, 0f, projectileType, 0, 0f, player.whoAmI);

				Projectile proj = Main.projectile[index];
				if (proj.ModProjectile is not GuardianLanternShieldAnchor shield)
					proj.Kill();
				else
					shield.OnChangeSelectedItem(player);
				
			}
			else
			{
				Projectile proj = Main.projectile.FirstOrDefault(i => i.active && i.owner == player.whoAmI && i.type == projectileType);
				if (proj?.ModProjectile is GuardianLanternShieldAnchor shield)
				{
					if (shield.SelectedItem != player.selectedItem)
						shield.OnChangeSelectedItem(player);
				}
			}
			
			Item torchItem = TorchIndex >= 0 ? player.inventory[TorchIndex] : null;
			if (TorchIndex == -1 || torchItem == null || !ItemID.Sets.Torches[torchItem.type] || !TileID.Sets.Torch[torchItem.createTile])
			{
				for (int i = 0; i < 50; i++)
				{
					Item torch = player.inventory[i];
					if (ItemID.Sets.Torches[torch.type] && TileID.Sets.Torch[torch.createTile])
					{
						TorchIndex = i;
						return;
					}
				}
			}

			TorchIndex = -1;
		}
		
		public override void ModifyTooltips(List<TooltipLine> tooltips)
		{
			var guardian = Main.LocalPlayer.GetModPlayer<OrchidGuardian>();
			TooltipLine tt = tooltips.FirstOrDefault(x => x.Name == "Damage" && x.Mod == "Terraria");
			if (tt != null)
			{
				string[] splitText = tt.Text.Split(' ');
				string damageValue = splitText.First();
				tt.Text = damageValue + " " + Language.GetTextValue(ModContent.GetInstance<OrchidMod>().GetLocalizationKey("DamageClasses.GuardianDamageClass.DisplayName"));
			}

			int index = tooltips.FindIndex(ttip => ttip.Mod.Equals("Terraria") && ttip.Name.Equals("Knockback"));
			tooltips.Insert(index + 1, new TooltipLine(Mod, "ParryDuration", Language.GetTextValue("Mods.OrchidMod.UI.GuardianItem.ParryAndBlockDuration", OrchidUtils.FramesToSeconds((int)guardian.GetParryDuration(Item, ParryDuration)), OrchidUtils.FramesToSeconds((int)guardian.GetBlockDuration(Item, (int)(ParryDuration * BlockDurationMult))))));

			string click = OrchidUtils.GetClickInfoTooltip(OrchidMod.OrchidClientConfig.GuardianSwapPaviseInputs, false);
			tooltips.Insert(index + 2, new TooltipLine(Mod, "ClickInfo", Language.GetTextValue("Mods.OrchidMod.UI.GuardianItem.ChargeToPunchLanternShield", click))
			{
				OverrideColor = new Color(175, 255, 175)
			});
			click = OrchidUtils.GetClickInfoTooltip(OrchidMod.OrchidClientConfig.GuardianSwapPaviseInputs);
			tooltips.Insert(index + 3, new TooltipLine(Mod, "ClickInfo2", Language.GetTextValue("Mods.OrchidMod.UI.GuardianItem.ChargeToParryLanternShield", click))
			{
				OverrideColor = new Color(175, 255, 175)
			});
		}

		public int TorchTypeDebuff(int itemID)
		{
			int debuff = -1;

			if (ModLoader.TryGetMod("CalamityMod", out Mod calamityMod))
			{
				if (calamityMod.TryFind("VoidTorch", out ModItem voidTorch) && calamityMod.TryFind("CrushDepth", out ModBuff crushDepth) && itemID == voidTorch.Type)
					debuff = crushDepth.Type;
				else if (calamityMod.TryFind("AstralTorch", out ModItem astralTorch) && calamityMod.TryFind("AstralInfectionDebuff", out ModBuff astralInfection) && itemID == astralTorch.Type)
					debuff = astralInfection.Type;
				else if (calamityMod.TryFind("CausticTorch", out ModItem causticTorch) && calamityMod.TryFind("SulphurousTorch", out ModItem sulphurousTorch) && calamityMod.TryFind("Irradiated", out ModBuff irradiated) && (itemID == causticTorch.Type || itemID == sulphurousTorch.Type))
					debuff = irradiated.Type;
				else if (calamityMod.TryFind("ThermalTorch", out ModItem thermalTorch) && calamityMod.TryFind("BrimstoneFlames", out ModBuff brimstoneFlames) && itemID == thermalTorch.Type)
					debuff = brimstoneFlames.Type;
			}
			else if (itemID is ItemID.DemonTorch or ItemID.BoneTorch)
				debuff = BuffID.ShadowFlame;
			else if (itemID is ItemID.CursedTorch)
				debuff = BuffID.CursedInferno;
			else if (itemID is ItemID.IchorTorch)
				debuff = BuffID.Ichor;
			else if (itemID is ItemID.IceTorch)
				debuff = BuffID.Frostburn2;
			else if (itemID is ItemID.JungleTorch)
				debuff = BuffID.Venom;
			else debuff = BuffID.OnFire3;

			return debuff;
		}
	}
}
