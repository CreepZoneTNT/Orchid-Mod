using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Audio;
using Terraria.ID;

namespace OrchidMod.Content.Guardian.Weapons.FencingBlades;

public class DeerclopsFencingBlade : OrchidModGuardianFencingBlade
{
	public override FencingBladeAttackProfile ChargedProfile(Projectile anchor) => FencingBladeAttackID.MultiSwing with
	{
		AnimationSpeed = 0.8f, BendAmount = 0.005f, Damage = 0.4f, Quantity = 5, Scale = 1.1f
	};

	public override FencingBladeAttackProfile ReinforcedProfile(Projectile anchor) => FencingBladeAttackID.MultiSwing with
	{
		AnimationSpeed = 0.6f, Velocity = 1.5f, BendAmount = 0.01f, Damage = 0.25f, Quantity = 10, Scale = 1.5f
	};

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
		
	}

	public override Color GetColor(Player player, OrchidGuardian guardian, Projectile anchor) => new (54, 58, 39);

	public override void FencingBladeModifyHitNPC(Player player, OrchidGuardian guardian, NPC target, Projectile projectile, ref NPC.HitModifiers modifiers, bool firstHit)
	{
		modifiers.ArmorPenetration += 15;
	}
}