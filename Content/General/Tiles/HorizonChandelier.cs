using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using OrchidMod.Content.Guardian.Misc;
using ReLogic.Content;
using Terraria;
using Terraria.DataStructures;
using Terraria.Enums;
using Terraria.GameContent;
using Terraria.GameContent.Drawing;
using Terraria.GameContent.UI;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using Terraria.ObjectData;

namespace OrchidMod.Content.General.Tiles;

public class HorizonChandelier : ModTile
{
	public Asset<Texture2D> GlowMask;
	
	public override void SetStaticDefaults()
	{
		Main.tileLighted[Type] = true;
		Main.tileFrameImportant[Type] = true;
		Main.tileLavaDeath[Type] = true;
		TileID.Sets.DisableSmartCursor[Type] = true;
		TileObjectData.newTile.CopyFrom(TileObjectData.Style1x2Top);
		TileObjectData.newTile.Origin = new Point16(1, 0);
		TileObjectData.newTile.AnchorTop = new AnchorData(AnchorType.SolidTile | AnchorType.SolidSide, 1, 1);
		TileObjectData.newTile.AnchorBottom = AnchorData.Empty;
		TileObjectData.newTile.Height = TileObjectData.newTile.Width = 3;
		TileObjectData.newTile.CoordinateHeights = [16, 16, 16];
		TileObjectData.newTile.StyleLineSkip = 2;
		TileObjectData.addTile(Type);
		AddMapEntry(new Color(253, 221, 3), Language.GetText("MapObject.Chandelier"));
		DustType = -1;
		AdjTiles = [4];
		AddToArray(ref TileID.Sets.RoomNeeds.CountsAsTorch);
		
		GlowMask = ModContent.Request<Texture2D>(Texture + "_Color");
	}

	public override bool PreDraw(int i, int j, SpriteBatch spriteBatch)
	{
		Tile tile = Main.tile[i, j];
		Vector2 zero = Main.drawToScreen ? Vector2.Zero : new Vector2(Main.offScreenRange);
		Vector2 drawPos = new Vector2(i * 16f, j * 16f) - Main.screenPosition + zero;
		Rectangle frame = new Rectangle(tile.TileFrameX, tile.TileFrameY, 16, 16);
		
		Texture2D texture = TextureAssets.Tile[Type].Value;
		spriteBatch.Draw(
			texture,
			drawPos,
			frame,
			Lighting.GetColor(i, j),
			0f, Vector2.Zero, 1f, SpriteEffects.None, 0f
		);
		
		spriteBatch.Draw(
			GlowMask.Value,
			drawPos,
			frame,
			Lighting.GetColor(i, j, HorizonBrick.HorizonBrickColor),
			0f, Vector2.Zero, 1f, SpriteEffects.None, 0f
		);

		if (TileDrawing.IsVisible(tile) && tile.TileFrameX < 36)
		{
			spriteBatch.Draw(
				ModContent.Request<Texture2D>(Texture + "_Glow").Value,
				drawPos,
				frame,
				Color.White,
				0f, Vector2.Zero, 1f, SpriteEffects.None, 0f
			);
			
		}

		return false;
	}

	public override void HitWire(int i, int j)
	{
		Tile tile = Main.tile[i, j];
		int num1 = 2;
		int num2 = 2;
		int num3 = i - tile.TileFrameX / 18 % num1;
		int num4 = j - tile.TileFrameY / 18 % num2;
		for (int index1 = num3; index1 < num3 + num1; ++index1)
		{
			for (int index2 = num4; index2 < num4 + num2; ++index2)
			{
				Tile tileSafely = Framing.GetTileSafely(index1, index2);
				if (tileSafely.HasTile && tileSafely.TileType == Type)
				{
					if (tileSafely.TileFrameX != 72)
						tileSafely.TileFrameX += 36;
					if (tileSafely.TileFrameX >= 72)
						tileSafely.TileFrameX -= 72;
				}
				if (Wiring.running)
					Wiring.SkipWire(index1, index2);
			}
		}
	}
}