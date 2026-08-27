using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent.Drawing;
using Terraria.ID;

namespace OrchidMod.Content.Guardian.Weapons.FencingBlades;

public class KingSlimeFencingBlade : OrchidModGuardianFencingBlade
{
	public override FencingBladeAttackProfile ChargedProfile(Projectile anchor) => FencingBladeAttackID.SingleSwing with {Scale = 1.25f};
	public override FencingBladeAttackProfile ReinforcedProfile(Projectile anchor) => FencingBladeAttackID.SingleSwing with
	{
		Damage = 2f, Velocity = 1.5f, Scale = 2.5f, ScaleChange = 0.995f, ControlAngle = MathHelper.Pi
	};

	public override void SafeSetDefaults()
	{
		Item.useTime = 45;
		Item.damage = 86;
		Item.rare = ItemRarityID.Blue;
		Item.width = 54;
		Item.height = 54;
		Item.knockBack = 5f;
		Item.shootSpeed = 10f;
		Item.value = Item.sellPrice(0, 0, 30);

		SheathOffset = new Vector2(3f, 1f);
	}

	public override Color GetColor(Player player, OrchidGuardian guardian, Projectile anchor) => new (40, 160, 215);

	public override void ExtraAIFencingBlade(Player player, OrchidGuardian guardian, Projectile anchor)
	{
		if (anchor.ModProjectile is GuardianFencingBladeAnchor blade)
		{
			if (anchor.ai[0] is > 36f and <= 41f)
			{
				Dust.NewDustPerfect(anchor.Center, DustID.t_Slime, Vector2.UnitY.RotatedBy(anchor.ai[2]) * Main.rand.NextFloat(2f, 4f), newColor: new Color(0, 80, 255, 0));
			}
		}
	}

	public override void OnHit(Player player, OrchidGuardian guardian, NPC target, Projectile projectile, NPC.HitInfo hit)
	{
		target.AddBuff(BuffID.Slimed, 180);
		SoundEngine.PlaySound(SoundID.NPCDeath28 with { PitchRange = (0.2f, 0.8f), Volume = 0.4f }, target.Center);
	}
}