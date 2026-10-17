using Terraria;
using Terraria.ID;

namespace OrchidMod.Content.Guardian.Weapons.Greathammers
{
	public class CorruptionGreathammer : OrchidModGuardianGreathammer
	{
		public override void SafeSetDefaults()
		{
			Item.width = 38;
			Item.height = 36;
			Item.value = Item.sellPrice(0, 0, 3, 50);
			Item.rare = ItemRarityID.White;
			Item.knockBack = 5f;
			Item.shootSpeed = 14f;
			Item.useTime = 32;
			Item.damage = 158;
			GuardStacks = 1;
			SlamSpeed = 0.8f;
			Range = 25;
			AuraScale = 0.9f;
			AuraPrimary = new(50, 90, 0, 60);
			AuraSecondary = new(30, 30, 120, 60);
		}
	}
}
