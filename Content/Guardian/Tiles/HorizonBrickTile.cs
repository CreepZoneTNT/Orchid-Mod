using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using OrchidMod.Content.Guardian.Misc;
using OrchidMod.Content.Guardian.Projectiles.Misc;
using OrchidMod.Utilities;
using Terraria;
using Terraria.DataStructures;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;

namespace OrchidMod.Content.Guardian.Tiles;

public class HorizonBrickTile : ModTile
{
	public Texture2D GlowMask;

	
	public override void SetStaticDefaults()
	{
		Main.tileSolid[Type] = true;
		Main.tileBrick[Type] = true;
		Main.tileMergeDirt[Type] = true;
		Main.tileBlockLight[Type] = true;
		Main.tileLighted[Type] = true;
		DustType = DustID.SilverFlame;
		HitSound = SoundID.Tink;
		AddMapEntry(new Color(159, 122, 163));
		
		

		GlowMask ??= ModContent.Request<Texture2D>(Texture + "_Glow").Value;
	}
	
	public override void ModifyLight(int i, int j, ref float r, ref float g, ref float b)
	{
		Vector3 horizonColor = HorizonBrick.HorizonBrickColor.ToVector3() * 0.5f;
		r = horizonColor.X;
		g = horizonColor.Y;
		b = horizonColor.Z;
	}

	public override void NumDust(int i, int j, bool fail, ref int num) => num = fail ? 1 : 3;

	// public override bool PreDraw(int i, int j, SpriteBatch spriteBatch)
	// {
	// 	Tile tile = Main.tile[i, j];
	// 	Vector2 zero = Main.drawToScreen ? Vector2.Zero : new Vector2(Main.offScreenRange);
	// 	
	// 	Texture2D texture = TextureAssets.Tile[Type].Value;
	//
	// 	spriteBatch.Draw(texture, 
	// 		new Vector2(i, j) * 16f - Main.screenPosition + zero, 
	// 		new Rectangle(tile.TileFrameX, tile.TileFrameY, 16, 16), 
	// 		Lighting.GetColor(i, j)
	// 	);
	// 	
	// 	
	// 	return false;
	// }
	
	public override void DrawEffects(int i, int j, SpriteBatch spriteBatch, ref TileDrawInfo drawData)
	{
		drawData.drawTexture = GlowMask;
		drawData.glowColor = HorizonBrick.HorizonBrickColor;
	}
}