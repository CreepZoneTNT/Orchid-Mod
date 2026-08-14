using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Audio;
using Terraria.ID;

namespace OrchidMod.Content.Guardian.Weapons.FencingBlades;

public class DeerclopsFencingBlade : OrchidModGuardianFencingBlade
{
	public override void SafeSetDefaults()
	{
		Item.useTime = 24;
		Item.damage = 60;
		Item.rare = ItemRarityID.Green;
		Item.width = 60;
		Item.height = 60;
		Item.knockBack = 3f;
		Item.shootSpeed = 14f;
		Item.value = Item.sellPrice(0, 1, 50);

		DrawSheath = false;
		SwingSound = SoundID.DD2_MonkStaffSwing;
		SwingsPerAttack = 10;
		ReinforcedSwingSpeed = 0.6f;
	}

	public override Color GetColor(Player player, OrchidGuardian guardian, Projectile anchor) => new (54, 58, 39);

	public override void FencingBladeModifyHitNPC(Player player, OrchidGuardian guardian, NPC target, Projectile projectile, ref NPC.HitModifiers modifiers, bool firstHit)
	{
		modifiers.ArmorPenetration += 15;
	}
}