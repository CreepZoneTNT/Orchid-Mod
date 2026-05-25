using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using OrchidMod.Content.Guardian.Projectiles.Misc;
using OrchidMod.Content.Guardian.Tiles;
using OrchidMod.Utilities;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using SpriteBatchSnapshot = OrchidMod.Utilities.SpriteBatchSnapshot;

namespace OrchidMod.Content.Guardian.Misc
{
	public class HorizonChandelier : ModItem
	{
		public Texture2D GlowMask;
		

		public override void SetStaticDefaults()
		{
			Item.ResearchUnlockCount = 100;
		}

		public override void SetDefaults() {
			Item.DefaultToPlaceableTile(ModContent.TileType<HorizonBrickTile>());
			Item.width = 26;
			Item.height = 26;
			
			GlowMask ??= ModContent.Request<Texture2D>(Texture + "_Glow").Value;
		}

		public override bool PreDrawInInventory(SpriteBatch spriteBatch, Vector2 position, Rectangle frame, Color drawColor, Color itemColor, Vector2 origin, float scale)
		{
			spriteBatch.Draw(GlowMask, position, null, HorizonBrick.HorizonBrickColor, 0f, GlowMask.Size() * 0.5f, scale, SpriteEffects.None, 0f);

			return true;
		}

		public override bool PreDrawInWorld(SpriteBatch spriteBatch, Color lightColor, Color alphaColor, ref float rotation, ref float scale, int whoAmI)
		{
			Main.GetItemDrawFrame(Item.type, out _, out Rectangle itemFrame);
			Vector2 drawOrigin = itemFrame.Size() * 0.5f;
			Vector2 drawPosition = Item.Bottom - Main.screenPosition - new Vector2(0, drawOrigin.Y);
			
			spriteBatch.Draw(GlowMask, drawPosition, itemFrame, Lighting.GetColor((int)(Item.position.X / 16f), (int)(Item.position.Y / 16f), HorizonBrick.HorizonBrickColor), rotation, GlowMask.Size() * 0.5f, scale, SpriteEffects.None, 0f);
			
			return true;
		}

		public override void AddRecipes()
		{
			CreateRecipe()
				.AddIngredient<HorizonBrick>(4)
				.AddIngredient(ItemID.Torch, 4)
				.AddIngredient(ItemID.Chain)
				.AddTile(TileID.LunarCraftingStation)
				.Register();
		}
	}
}
