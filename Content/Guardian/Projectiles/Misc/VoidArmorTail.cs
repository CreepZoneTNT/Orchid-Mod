using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using OrchidMod.Utilities;
using ReLogic.Content;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace OrchidMod.Content.Guardian.Projectiles.Misc;

public class VoidArmorTail : OrchidModGuardianProjectile
{
	
	public override void SafeSetDefaults()
	{
		Projectile.width = 18;
		Projectile.height = 18;
		Projectile.timeLeft = 90000;
		Projectile.friendly = true;
		Projectile.hostile = false;
		Projectile.penetrate = -1;
	}

	public Vector2 stabDirection;
	public override void AI()
	{
		if (IsLocalOwner)
		{
			
			Vector2 target = new (Projectile.ai[0], Projectile.ai[1]);
			
			if (Projectile.ai[2] > 0)
			{
				target = Owner.Center + stabDirection * (80f * (float)Math.Cos((Projectile.ai[2] - 7.0483f) * MathHelper.Pi * 0.1f) + 48f);	
			}

			Projectile.ai[2]--;
			if (Projectile.ai[2] <= 0)
			{
				Projectile.ai[2] = 0;
				stabDirection = Vector2.Zero;
			}
			
			Projectile.Center = Projectile.Center.MoveTowards(target, Owner.Distance(Projectile.Center) >= 128f ? 96f : 16f);
			Projectile.spriteDirection = Projectile.direction = (Projectile.Center.X > Owner.Center.X).ToDirectionInt();

			Projectile.velocity = Owner.velocity * float.Epsilon;
			Projectile.rotation = Projectile.velocity.ToRotation() * 0.1f;
			
		}
	}

	public override void SafeOnHitNPC(NPC target, NPC.HitInfo hit, int damageDone, Player player, OrchidGuardian guardian)
	{
		target.AddBuff(BuffID.ShadowFlame, 180);
	}

	public override bool OrchidPreDraw(SpriteBatch spriteBatch, ref Color lightColor)
	{
		if (IsLocalOwner)
		{
			const float length = 64f;
			float distance = Owner.Center.Distance(Projectile.Center);
			float angleTo = Owner.Center.AngleTo(Projectile.Center);
			if (Owner.direction == -1) angleTo = MathHelper.PiOver2 - angleTo;

			float opposite = MathF.Sqrt(length * length - (length - distance) * (length - distance) * 0.25f);
			float angle = MathHelper.PiOver2 - MathF.Atan2(opposite - Owner.Center.Y, (distance - length) * 0.5f - Owner.Center.X) + angleTo;
			if (Owner.direction == -1) angle = MathHelper.PiOver2 - (angle + angleTo);
			
			Vector2 point1 = Owner.Center + new Vector2(MathF.Cos(angle) * Owner.direction, MathF.Sin(angle)) * length;
			Vector2 point2 = point1 + new Vector2(MathF.Cos(angleTo), MathF.Sin(angleTo)) * length;

			Texture2D chainTexture = ModContent.Request<Texture2D>(Texture + "_Chain", AssetRequestMode.ImmediateLoad).Value;
			spriteBatch.Draw(chainTexture, point1 - Main.screenPosition, null, Color.Blue, 0f, chainTexture.Size() * 0.5f, 1f, SpriteEffects.None, 0f);
			spriteBatch.Draw(chainTexture, point2 - Main.screenPosition, null, Color.Red, 0f, chainTexture.Size() * 0.5f, 1f, SpriteEffects.None, 0f);
			for (float i = 0; i <= 1; i += 0.025f)
			{
				Vector2 point = Bezier(Owner.Center, point1, point2, Projectile.Center, i);
				spriteBatch.Draw(chainTexture, point - Main.screenPosition, null, lightColor, 0f, chainTexture.Size() * 0.5f, 1f, SpriteEffects.None, 0f);
			}
		}

		return true;
	}

	private Vector2 Bezier(Vector2 point1, Vector2 point2, Vector2 point3, Vector2 point4, float amount)
	{
		Vector2 result = point4;
		float recAmount = 1 - amount;
		result = (recAmount * recAmount * recAmount * point1)
		         + (3 * recAmount * recAmount * amount * point2)
		         + (3 * recAmount * amount * amount * point3)
		         + (amount * amount * amount * point4);
		return result;
	}
}