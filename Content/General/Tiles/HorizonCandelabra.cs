using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using OrchidMod.Content.Guardian.Misc;
using ReLogic.Content;
using Terraria;
using Terraria.DataStructures;
using Terraria.GameContent;
using Terraria.GameContent.Drawing;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.ObjectData;

namespace OrchidMod.Content.General.Tiles;

public class HorizonCandelabra : ModTile
{
	public Asset<Texture2D> GlowMask;
	
	public override void SetStaticDefaults()
	{
		Main.tileLighted[Type] = true;
		Main.tileFrameImportant[Type] = true;
		Main.tileNoAttach[Type] = true;
		Main.tileLavaDeath[Type] = true;
		TileID.Sets.DisableSmartCursor[Type] = true;
		TileObjectData.newTile.CopyFrom(TileObjectData.Style2x2);
		TileObjectData.newTile.Origin = new Point16(1, 1);
		TileObjectData.newTile.CoordinateHeights = [16, 16];
		TileObjectData.newTile.StyleLineSkip = 2;
		TileObjectData.newTile.DrawYOffset = 2;
		TileObjectData.addTile(Type);
		AddMapEntry(new Color(253, 221, 3), Lang.GetItemName(ItemID.Candelabra));
		DustType = -1;
		AdjTiles = [4];
		AddToArray(ref TileID.Sets.RoomNeeds.CountsAsTorch);
		
		GlowMask = ModContent.Request<Texture2D>(Texture + "_Color");
	}

	public override void PostDraw(int i, int j, SpriteBatch spriteBatch)
	{
		Tile tile = Main.tile[i, j];
		Vector2 zero = Main.drawToScreen ? Vector2.Zero : new Vector2(Main.offScreenRange);
		Vector2 drawPos = new Vector2(i * 16f, j * 16f) - Main.screenPosition + zero;
		Rectangle frame = new (tile.TileFrameX, tile.TileFrameY, 16, 16);
		
		spriteBatch.Draw(
			GlowMask.Value,
			drawPos,
			frame,
			Lighting.GetColor(i, j, HorizonBrick.HorizonBrickColor),
			0f, Vector2.Zero, 1f, SpriteEffects.None, 0f
		);

		if (!TileDrawing.IsVisible(tile) || tile.TileFrameX >= 36)
			return;
		
		spriteBatch.Draw(
			ModContent.Request<Texture2D>(Texture + "_Glow").Value,
			drawPos,
			frame,
			Color.White,
			0f, Vector2.Zero, 1f, SpriteEffects.None, 0f
		);
	}

	public override void PostDrawPlacementPreview(int i, int j, SpriteBatch spriteBatch, Rectangle frame, Vector2 position, Color color, bool validPlacement, SpriteEffects spriteEffects)
	{Tile tile = Main.tile[i, j];
		Vector2 zero = Main.drawToScreen ? Vector2.Zero : new Vector2(Main.offScreenRange);
		frame = new Rectangle(tile.TileFrameX, tile.TileFrameY, 16, 16);
     	
		spriteBatch.Draw(
			GlowMask.Value,
			position,
			frame,
			Lighting.GetColor(i, j, HorizonBrick.HorizonBrickColor),
			0f, Vector2.Zero, 1f, SpriteEffects.None, 0f
		);
	}

	public override void HitWire(int i, int j)
	{
		Tile tile = Main.tile[i, j];
		int num3 = i - tile.TileFrameX / 18 % 2;
		int num4 = j - tile.TileFrameY / 18 % 2;
		for (int index1 = num3; index1 < num3 + 2; ++index1)
		{
			for (int index2 = num4; index2 < num4 + 2; ++index2)
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