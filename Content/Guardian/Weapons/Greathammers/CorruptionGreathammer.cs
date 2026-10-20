using OrchidMod.Content.Guardian.Weapons.Warhammers;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace OrchidMod.Content.Guardian.Weapons.Greathammers
{
	public class CorruptionGreathammer : OrchidModGuardianGreathammer
	{
		public override void SetStaticDefaults()
		{
			ItemID.Sets.ShimmerTransformToItem[Type] = ModContent.ItemType<CorruptionWarhammer>();
			ItemID.Sets.ShimmerTransformToItem[ModContent.ItemType<CorruptionWarhammer>()] = Type;
		}

		public override void SafeSetDefaults()
		{
			Item.width = 38;
			Item.height = 36;
			Item.value = Item.sellPrice(0, 0, 27, 0);
			Item.rare = ItemRarityID.Blue;
			Item.knockBack = 5f;
			Item.shootSpeed = 14f;
			Item.useTime = 40;
			Item.damage = 108;
			GuardStacks = 1;
			SlamSpeed = 0.9f;
			Range = 25;
			AuraScale = 0.9f;
			AuraPrimary = new(50, 90, 0, 60);
			AuraSecondary = new(30, 30, 120, 60);
		}

		public override void AddRecipes()
		{
			var recipe = CreateRecipe();
			recipe.AddTile(TileID.Anvils);
			recipe.AddIngredient(ItemID.DemoniteBar, 12);
			recipe.Register();
		}
	}
}
