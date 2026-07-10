using Microsoft.Xna.Framework;
using OrchidMod.Common.Attributes;
using OrchidMod.Common.ModSystems;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace OrchidMod.Content.Guardian.Accessories
{
	[CrossmodContent("ThoriumMod")]
	public class ThoriumLifeQuartzCenser : OrchidModGuardianEquipable
	{
		public override void SafeSetDefaults()
		{
			Item.width = 26;
			Item.height = 30;
			Item.value = Item.sellPrice(0, 0, 15, 0);
			Item.rare = ItemRarityID.Blue;
			Item.accessory = true;
		}

		public override void UpdateAccessory(Player player, bool hideVisual)
		{
			OrchidGuardian modPlayer = player.GetModPlayer<OrchidGuardian>();
			modPlayer.GuardianThoriumCenser = true;
			modPlayer.onUseSlamDelegate += HealTeammates;
		}
		
		public void HealTeammates(Player player, OrchidGuardian guardian)
		{
			if (OrchidMod.ThoriumMod != null)
			{
				Player lowestHealthPlayer = player;
				foreach (Player other in Main.player)
				{
					// targets the lowest heath nearby player
					if (other.DistanceSQ(player.Center) < 800f && other.active && !other.dead && other.statLife < lowestHealthPlayer.statLife)
					{
						// 16 * 50 = 800f for a 50 tiles range
						lowestHealthPlayer = other;
					}
				}

				/* Ended up being unused, but this is how I would check for the players shield health, potentially to target unshielded players.
				foreach (ModPlayer thoriumPlayer in Player.ModPlayers)
				{
					if (thoriumPlayer.Name == "ThoriumPlayer" && thoriumPlayer.Mod == OrchidMod.ThoriumMod)
					{
						FieldInfo field = thoriumPlayer.GetType().GetField("shieldHealth", BindingFlags.Public | BindingFlags.Instance);
						int shieldHealth = (int)field.GetValue(thoriumPlayer);
						break;
					}
				}
				*/

				// This is how the War Forger applies its shield, where 5f is the shield amount, and 10f is the maximum shield amount that can be applied
				int projectileType = OrchidMod.ThoriumMod.Find<ModProjectile>("HealerShield").Type;
				Projectile.NewProjectile(player.GetSource_FromThis(), lowestHealthPlayer.Center, Vector2.Zero, projectileType, 0, 0.0f, player.whoAmI, 5f, 10f);
			}
		}

		public override void AddRecipes()
		{
			if (OrchidMod.ThoriumMod != null)
			{
				var recipe = CreateRecipe();
				recipe.AddTile(TileID.Anvils);
				recipe.AddRecipeGroup(OrchidRecipes.AnySilverBarGroup, 6);
				recipe.AddIngredient(OrchidMod.ThoriumMod, "LifeQuartz", 4);
				recipe.AddIngredient(OrchidMod.ThoriumMod, "SmoothCoal", 4);
				recipe.Register();
			}
		}
	}
}