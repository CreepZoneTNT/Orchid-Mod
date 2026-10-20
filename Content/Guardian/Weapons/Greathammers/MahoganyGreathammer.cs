using OrchidMod.Content.Guardian.Weapons.Shields;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace OrchidMod.Content.Guardian.Weapons.Greathammers
{
	public class MahoganyGreathammer : OrchidModGuardianGreathammer
	{
		public override void SetStaticDefaults()
		{
			ItemID.Sets.ShimmerTransformToItem[Type] = ModContent.ItemType<MahoganyPavise>();
			ItemID.Sets.ShimmerTransformToItem[ModContent.ItemType<MahoganyPavise>()] = Type;
		}

		public override void SafeSetDefaults()
		{
			Item.width = 46;
			Item.height = 44;
			Item.value = Item.sellPrice(0, 0, 0, 20);
			Item.rare = ItemRarityID.White;
			Item.knockBack = 4.5f;
			Item.shootSpeed = 12f;
			Item.useTime = 30;
			Item.damage = 52;
			GuardStacks = 1;
			SlamSpeed = 0.85f;
			Range = 20;
			AuraScale = 0.8f;
			AuraPrimary = new(115, 65, 68, 60);
			AuraSecondary = new(82, 47, 49, 60);
		}

		public override void AddRecipes()
		{
			var recipe = CreateRecipe();
			recipe.AddTile(TileID.WorkBenches);
			recipe.AddIngredient(ItemID.RichMahogany, 10);
			recipe.Register();
		}
	}
}
