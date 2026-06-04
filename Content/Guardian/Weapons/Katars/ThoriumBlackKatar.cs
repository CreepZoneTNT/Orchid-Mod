using Microsoft.Xna.Framework;
using OrchidMod.Content.Guardian.Projectiles.Katars;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace OrchidMod.Content.Guardian.Weapons.Katars;

public class ThoriumBlackKatar : OrchidModGuardianKatar
{
	public override void SafeSetDefaults()
	{
		Item.width = 36;
		Item.height = 36;
		Item.knockBack = 6f;
		Item.damage = 240;
		Item.value = Item.sellPrice(0, 5);
		Item.rare = ItemRarityID.Yellow;
		Item.useTime = 30;
		JabVelocity = 24f;
		ParryDashSpeed = 24f;
		ParryDuration = 15;
		ParryDashMomentum = 0.6f;
	}

	public override Color GetColor()
	{
		return new Color(61, 48, 48);
	}

	public override void OnDashKatar(Player player, OrchidGuardian guardian, Projectile anchor)
	{
		Projectile.NewProjectileDirect(Item.GetSource_FromThis(), player.Center, Vector2.Zero, ModContent.ProjectileType<ThoriumBlackKatarProjectile>(), guardian.GetGuardianDamage(Item.damage * ParryDamage), 6f);
	}
}