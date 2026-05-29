using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using OrchidMod.Content.Guardian.Projectiles.Misc;
using OrchidMod.Content.Guardian.Tiles;
using OrchidMod.Utilities;
using ReLogic.Content;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using SpriteBatchSnapshot = OrchidMod.Utilities.SpriteBatchSnapshot;

namespace OrchidMod.Content.Guardian.Misc
{
	public class HorizonBrick : ModItem
	{
		public Asset<Texture2D> GlowMask;
		
		public static Color HorizonBrickColor => GuardianHorizonLanceAnchor.GetHorizonGlowColor(Math.Sin(Main.timeForVisualEffects * 0.02f), 0.6f + (float)Math.Cos(Main.timeForVisualEffects * 0.04f) * 0.1f, 0.8f);


		public override void SetStaticDefaults()
		{
			Item.ResearchUnlockCount = 100;
		}

		public override void SetDefaults() {
			Item.DefaultToPlaceableTile(ModContent.TileType<HorizonBrickTile>());
			Item.width = 20;
			Item.height = 20;
			
			GlowMask = ModContent.Request<Texture2D>(Texture + "_Glow");
		}

		public override bool PreDrawInInventory(SpriteBatch spriteBatch, Vector2 position, Rectangle frame, Color drawColor, Color itemColor, Vector2 origin, float scale)
		{
			spriteBatch.Draw(GlowMask.Value, position, null, HorizonBrickColor, 0f, GlowMask.Size() * 0.5f, scale, SpriteEffects.None, 0f);

			return true;
		}

		public override bool PreDrawInWorld(SpriteBatch spriteBatch, Color lightColor, Color alphaColor, ref float rotation, ref float scale, int whoAmI)
		{
			Main.GetItemDrawFrame(Item.type, out _, out Rectangle itemFrame);
			Vector2 drawOrigin = itemFrame.Size() * 0.5f;
			Vector2 drawPosition = Item.Bottom - Main.screenPosition - new Vector2(0, drawOrigin.Y);
			
			spriteBatch.Draw(GlowMask.Value, drawPosition, itemFrame, Lighting.GetColor((int)(Item.position.X / 16f), (int)(Item.position.Y / 16f), HorizonBrickColor), rotation, GlowMask.Size() * 0.5f, scale, SpriteEffects.None, 0f);
			
			return true;
		}

		public override void AddRecipes()
		{
			CreateRecipe(10)
				.AddIngredient(ItemID.StoneBlock, 10)
				.AddIngredient<HorizonFragment>()
				.AddTile(TileID.LunarCraftingStation)
				.Register();
		}
	}
}
