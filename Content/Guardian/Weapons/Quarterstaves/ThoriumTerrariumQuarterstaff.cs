using Microsoft.Xna.Framework;
using OrchidMod.Common.Attributes;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace OrchidMod.Content.Guardian.Weapons.Quarterstaves;

[CrossmodContent("ThoriumMod")]
public class ThoriumTerrariumQuarterstaff : OrchidModGuardianQuarterstaff
{
	
	// Code borrowed from ThoriumTerrariumWarhammerProjectile.cs
	// (itself borrowed from most Terrarium projectiles)
	public Color CurrentColor = new(255, 255, 255, 100);

	public int RColor = 255;
	public bool RColorAllow = false;
	public int GColor = 128;
	public bool GColorAllow = true;
	public int BColor = 0;
	public bool BColorAllow = true;

	public static readonly int[] PotentialDusts =
	[
		DustID.GemRuby,
		DustID.InfernoFork,
		DustID.GemTopaz,
		DustID.GemEmerald,
		DustID.Frost,
		DustID.GemSapphire,
		DustID.GemAmethyst
	];
	
	public override void SafeSetDefaults()
	{
		Item.width = 58;
		Item.height = 64;
		Item.value = Item.sellPrice(0, 13, 50, 0);
		Item.rare = OrchidMod.ThoriumMod != null ? OrchidMod.ThoriumMod.Find<ModRarity>("TerrariumRarity").Type : ItemRarityID.Expert;
		Item.damage = 310;
		Item.knockBack = 6f;
		Item.useTime = 35;
		CounterStyle = 3;
		GuardStacks = 1;
		SlamStacks = 2;
	}

	public override void OnHit(Player player, OrchidGuardian guardian, NPC target, Projectile projectile, NPC.HitInfo hit, bool jabAttack, bool counterAttack)
	{
		var thoriumMod = OrchidMod.ThoriumMod;
		if (thoriumMod != null)
		{
			target.AddBuff(thoriumMod.Find<ModBuff>("TerrariumBacklash").Type, 120);
		}
	}
	
	public override void AddRecipes()
	{
		var thoriumMod = OrchidMod.ThoriumMod;
		if (thoriumMod != null)
		{
			CreateRecipe()
				.AddTile(TileID.LunarCraftingStation)
				.AddIngredient(thoriumMod, "TerrariumCore", 9)
				.Register();
		}
	}
}