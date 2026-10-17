using System;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;

namespace OrchidMod.Content.Guardian
{
	public abstract class OrchidModGuardianDeflectItem : OrchidModGuardianItem
	{
		/// <summary>Iframes given by a deflect.</summary>
		public int InvincibilityDuration;
		/// <summary>Multiplier for the item's damage to be dealt to deflected NPCs.</summary>
		public float DeflectDamage;

		/// <summary>Called at the beginning of a deflect. Allows modifying deflect parameters or overriding deflect behavior for this weapon. Return false to prevent default deflect code from running.</summary>
		/// <remarks>See <c>OrchidGuardian.DoDeflect</c> for information on parameters.</remarks>
		public virtual bool PreDeflect(ref Rectangle targetArea, ref bool success, ref bool caughtProjectile, ref bool killedNPC, ModProjectile anchor, ref Predicate<NPC> EnemyPredicate, ref Predicate<Projectile> ProjPredicate, ref float range, ref float lenience, ref Vector2 origin, ref int damage) => true;
		/// <summary>Called once at the end of a deflect. Use <c>OrchidGuardian.DeflectedEnemies</c> to access enemies hit by the deflect.</summary>
		public virtual void PostDeflect(Player player, OrchidGuardian guardian, ModProjectile anchor, bool caughtProjectile, bool killedNPC) { }
		/// <summary>Called for <b>each</b> NPC deflected by a deflect. Return false to prevent that NPC from being deflected. Note that this only runs for enemies in the deflect's area of effect and matching the deflect's <c>EnemyPredicate</c> - see <c>PreDeflect</c> for overriding parameters or deflect behavior.</summary>
		public virtual bool OnDeflectNPC(Player player, OrchidGuardian guardian, NPC npc, ModProjectile anchor) => true;
		/// <summary>Called for <b>each</b> projectile deflected by a deflect. Return false to prevent that projectile from being deflected. Note that this only runs for projectiles in the deflect's area of effect and matching the deflect's <c>ProjPredicate</c> - see <c>PreDeflect</c> for overriding parameters or deflect behavior.</summary>
		public virtual bool OnDeflectProjectile(Player player, OrchidGuardian guardian, Projectile projectile, ModProjectile anchor) => true;
		/// <summary>Responsible for playing the sound when the player deflects with this weapon.</summary>
		public virtual void PlayDeflectSound(Player player, OrchidGuardian guardian, Projectile anchor, bool success)
		{
			SoundEngine.PlaySound(SoundID.Item52.WithPitchOffset(Main.rand.NextFloat(0.4f, 0.6f)), player.Center);
			if (success) SoundEngine.PlaySound(SoundID.Item37.WithPitchOffset(Main.rand.NextFloat(0.4f, 0.6f)), player.Center);
		}
	}
}
