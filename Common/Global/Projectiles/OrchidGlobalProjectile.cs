using System.Collections.Generic;
using Terraria;
using Terraria.ModLoader;
using static Terraria.ModLoader.ModContent;
using Terraria.ID;
using Terraria.DataStructures;
using OrchidMod.Content.Guardian.Buffs.Debuffs;

namespace OrchidMod.Common.Global.Projectiles
{
	public class OrchidGlobalProjectile : GlobalProjectile
	{
		public bool shamanProjectile = false;
		public bool gamblerProjectile = false;
		public bool dancerProjectile = false;
		public bool alchemistProjectile = false;
		public bool alchemistReactiveProjectile = false;
		public int shamanEmpowermentType = 0;
		public int gamblerInternalCooldown = 0;
		public bool gamblerDummyProj = false;
		public bool gamblerBonusTrigger = false;
		/// <summary>If this projectile originated from an NPC, or originated from a projectile that had this set, it is set to the NPC's WhoAmI. Defaults to 255 and is set to 255 if the NPC dies.</summary>
		public byte NPCOwner = 255;
		/// <summary>If the player's NPCOwner was stunned or becomes stunned, this is set to the duration of the stun. The projectile cannot deal damage while this is above 0.</summary>
		public int StunTimer = 0;

		public delegate void GamblerBonusProjectilesDelegate(Player player, OrchidGambler modPlayer, Projectile projectile, OrchidGlobalProjectile modProjectile, bool dummy = false);
		public GamblerBonusProjectilesDelegate gamblerBonusProjectilesDelegate;
		public delegate void AlchemistCatalyticTriggerDelegate(Player player, Projectile projectile, OrchidGlobalProjectile modProjectile);
		public AlchemistCatalyticTriggerDelegate alchemistCatalyticTriggerDelegate;

		public override bool InstancePerEntity => true;

		//TODO: sync NPCOwner
		public override void OnSpawn(Projectile projectile, IEntitySource source)
		{
			if (source is EntitySource_Parent parent)
			{
				if (parent.Entity is NPC npc)
				{
					NPCOwner = (byte)npc.whoAmI;
				}
				if (parent.Entity is Projectile proj)
				{
					NPCOwner = proj.GetGlobalProjectile<OrchidGlobalProjectile>().NPCOwner;
					StunTimer = proj.GetGlobalProjectile<OrchidGlobalProjectile>().StunTimer;
				}
			}
		}

		public override void PostAI(Projectile projectile)
		{
			if (StunTimer > 0 && projectile.numUpdates == 0)
				StunTimer--;
		}

		public override bool? CanHitNPC(Projectile projectile, NPC target)
		{
			OrchidGlobalProjectile modProjectile = projectile.GetGlobalProjectile<OrchidGlobalProjectile>();
			Player player = Main.player[projectile.owner];
			if (modProjectile.gamblerDummyProj)
			{
				if (target.type != NPCID.TargetDummy && player.HeldItem.type != ItemType<Content.Gambler.GamblerDummyTest>())
				{
					return false;
				}
			}
			return null;
		}

		public override bool CanHitPlayer(Projectile projectile, Player target)
		{
			if (StunTimer > 0) return false;
			else if (NPCOwner != 255 && Main.npc[NPCOwner].HasBuff<GreathammerStunDebuff>())
			{
				StunTimer = Main.npc[NPCOwner].buffTime[Main.npc[NPCOwner].FindBuffIndex(ModContent.BuffType<GreathammerStunDebuff>())];
				return false;
			}
			return true;
		}
	}
}