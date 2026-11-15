using Microsoft.Xna.Framework;
using OrchidMod.Content.General.Prefixes;
using OrchidMod.Content.Guardian.Armors.OreHelms;
using OrchidMod.Utilities;
using System.Collections.Generic;
using System.Linq;
using OrchidMod.Common;
using OrchidMod.Common.Global.Items;
using Terraria;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;

namespace OrchidMod.Content.Guardian
{
	internal class GuardianGlobalItem : GlobalItem
	{
		private static int ThoriumDDHelmID = -1;
		private static int ThoriumDDChestID = -1;
		private static int ThoriumDDLegsID = -1;

		public override void SetStaticDefaults()
		{
			if (OrchidMod.ThoriumMod is { } thoriumMod)
			{
				ThoriumDDHelmID = thoriumMod.Find<ModItem>("DepthDiverHelmet").Type;
				ThoriumDDChestID = thoriumMod.Find<ModItem>("DepthDiverChestplate").Type;
				ThoriumDDLegsID = thoriumMod.Find<ModItem>("DepthDiverGreaves").Type;
			}

			if (ThoriumDDHelmID != -1 && ThoriumDDChestID != -1 && ThoriumDDLegsID != -1)
			{
				OrchidGlobalItem.tagsByItemTypeOverride.Add(ThoriumDDHelmID, ClassTags.Guardian);
				OrchidGlobalItem.tagsByItemTypeOverride.Add(ThoriumDDChestID, ClassTags.Guardian);
				OrchidGlobalItem.tagsByItemTypeOverride.Add(ThoriumDDLegsID, ClassTags.Guardian);
			}
		}

		public override void UpdateEquip(Item item, Player player)
		{
			if (ThoriumDDHelmID != -1 && ThoriumDDChestID != -1 && ThoriumDDLegsID != -1)
			{
				OrchidGuardian modPlayer = player.Guardian();

				if (item.type == ThoriumDDHelmID)
					modPlayer.GuardianGuardRecharge += 0.8f;

				if (item.type == ThoriumDDChestID)
				{
					player.aggro += 250;
					modPlayer.GuardianGuardMax += 2;
				}

				if (item.type == ThoriumDDLegsID)
					modPlayer.GuardianGuardMax += 2;
			}
		}

		public override void ModifyTooltips(Item item, List<TooltipLine> tooltips)
		{
			// <item><description>"Tooltip#" - A tooltip line of the item. # will be 0 for the first line, 1 for the second, etc.</description></item>
			if (OrchidMod.ThoriumMod != null)
			{
				if (item.type == OrchidMod.ThoriumMod.Find<ModItem>("DepthDiverHelmet").Type)
				{
					int index = tooltips.FindIndex(ttip => ttip.Mod.Equals("Terraria") && ttip.Name.Equals("Defense")); // Tooltip#0 doesn't work
					tooltips.Insert(index + 2, new TooltipLine(Mod, "Tooltip", Language.GetTextValue(Mod.GetLocalizationKey("Items.DepthDiverHelmet.Tooltip"))));
				}

				if (item.type == OrchidMod.ThoriumMod.Find<ModItem>("DepthDiverChestplate").Type)
				{
					int index = tooltips.FindIndex(ttip => ttip.Mod.Equals("Terraria") && ttip.Name.Equals("Defense"));
					tooltips.Insert(index + 2, new TooltipLine(Mod, "Tooltip", Language.GetTextValue(Mod.GetLocalizationKey("Items.DepthDiverChestplate.Tooltip"))));
				}

				if (item.type == OrchidMod.ThoriumMod.Find<ModItem>("DepthDiverGreaves").Type)
				{
					int index = tooltips.FindIndex(ttip => ttip.Mod.Equals("Terraria") && ttip.Name.Equals("Defense"));
					tooltips.Insert(index + 2, new TooltipLine(Mod, "Tooltip", Language.GetTextValue(Mod.GetLocalizationKey("Items.DepthDiverGreaves.Tooltip"))));
				}
			}
		}
	
		public override void DrawArmorColor(EquipType type, int slot, Player drawPlayer, float shadow, ref Color color, ref int glowMask, ref Color glowMaskColor)
		{
			if ((drawPlayer.armor[10].type == ModContent.ItemType<GuardianChlorophyteHead>() || (drawPlayer.armor[10].type == ItemID.None && drawPlayer.armor[0].type == ModContent.ItemType<GuardianChlorophyteHead>())) && drawPlayer.body == 51 && drawPlayer.legs == 47)
			{
				float magicnumber = (float)(((float)Main.mouseTextColor) / 200.0 - 0.30000001192092896);
				color.R = (byte)(color.R * magicnumber);
				color.G = (byte)(color.G * magicnumber);
				color.B = (byte)(color.B * magicnumber);
			}
		}
	}
}
