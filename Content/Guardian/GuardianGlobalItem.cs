using System;
using OrchidMod.Content.General.Prefixes;
using OrchidMod.Utilities;
using System.Collections.Generic;
using System.Linq;
using Terraria;
using Terraria.Localization;
using Terraria.ModLoader;

namespace OrchidMod.Content.Guardian
{
	internal class GuardianGlobalItem : GlobalItem
	{
		public override void UpdateEquip(Item item, Player player)
		{
			if (OrchidMod.ThoriumMod != null)
			{
				OrchidGuardian modPlayer = player.GetModPlayer<OrchidGuardian>();

				if (item.type == OrchidMod.ThoriumMod.Find<ModItem>("DepthDiverHelmet").Type)
				{
					modPlayer.GuardianGuardRecharge += 0.8f;
				}

				if (item.type == OrchidMod.ThoriumMod.Find<ModItem>("DepthDiverChestplate").Type)
				{
					player.aggro += 250;
					modPlayer.GuardianGuardMax += 2;
				}

				if (item.type == OrchidMod.ThoriumMod.Find<ModItem>("DepthDiverGreaves").Type)
				{
					modPlayer.GuardianGuardMax += 2;
				}
			}
		}

		public override void ModifyTooltips(Item item, List<TooltipLine> tooltips)
		{
			OrchidGuardian modPlayer = Main.LocalPlayer.GetModPlayer<OrchidGuardian>();
			
			// <item><description>"Tooltip#" - A tooltip line of the item. # will be 0 for the first line, 1 for the second, etc.</description></item>
			if (OrchidMod.ThoriumMod != null)
			{
				if (item.type == OrchidMod.ThoriumMod.Find<ModItem>("DepthDiverHelmet").Type)
				{
					int index = tooltips.FindIndex(ttip => ttip.Mod.Equals("Terraria") && ttip.Name.Equals("Defense")); // Tooltip#0 doesn't work
					tooltips.Insert(index + 2, new TooltipLine(Mod, "Tooltip", Language.GetTextValue(ModContent.GetInstance<OrchidMod>().GetLocalizationKey("Items.DepthDiverHelmet.Tooltip"))));
				}

				if (item.type == OrchidMod.ThoriumMod.Find<ModItem>("DepthDiverChestplate").Type)
				{
					int index = tooltips.FindIndex(ttip => ttip.Mod.Equals("Terraria") && ttip.Name.Equals("Defense"));
					tooltips.Insert(index + 2, new TooltipLine(Mod, "Tooltip", Language.GetTextValue(ModContent.GetInstance<OrchidMod>().GetLocalizationKey("Items.DepthDiverChestplate.Tooltip"))));
				}

				if (item.type == OrchidMod.ThoriumMod.Find<ModItem>("DepthDiverGreaves").Type)
				{
					int index = tooltips.FindIndex(ttip => ttip.Mod.Equals("Terraria") && ttip.Name.Equals("Defense"));
					tooltips.Insert(index + 2, new TooltipLine(Mod, "Tooltip", Language.GetTextValue(ModContent.GetInstance<OrchidMod>().GetLocalizationKey("Items.DepthDiverGreaves.Tooltip"))));
				}
			}
			
			if (item.ModItem is OrchidModGuardianItem guardianItem && modPlayer.modifyTooltipsDelegate != null)
				foreach (Delegate del in modPlayer.modifyTooltipsDelegate.GetInvocationList())
				{
					try
					{
						if (del is OrchidGuardian.GuardianModifyTooltipsDelegate modify)
							modify(Main.LocalPlayer, modPlayer, guardianItem, tooltips);
					}
					catch
					{
						Mod.Logger.Error("ModifyTooltips delegate failed!");
					}
				}
		}
	}
}
