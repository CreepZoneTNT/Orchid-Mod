using Terraria;
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
					player.aggro += 250;
					modPlayer.GuardianGuardRecharge += 0.8f;
				}

				if (item.type == OrchidMod.ThoriumMod.Find<ModItem>("DepthDiverChestplate").Type)
				{
					modPlayer.GuardianGuardMax += 2;
				}

				if (item.type == OrchidMod.ThoriumMod.Find<ModItem>("DepthDiverGreaves").Type)
				{
					modPlayer.GuardianGuardMax += 2;
				}
			}
		}
	}
}
