using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Audio;
using Terraria.ID;

namespace OrchidMod.Content.Guardian.Weapons.FencingBlades;

public class KingSlimeFencingBlade : OrchidModGuardianFencingBlade
{
	public override void SafeSetDefaults()
	{
		Item.useTime = 45;
		Item.damage = 150;
		Item.rare = ItemRarityID.Blue;
		Item.width = 54;
		Item.height = 54;
		Item.knockBack = 3f;
		Item.value = Item.sellPrice(0, 0, 30);

		SheathOffset = new Vector2(3f, 1f);
	}

	public override Color GetColor() => new (40, 160, 215);

	public override void OnHit(Player player, OrchidGuardian guardian, NPC target, Projectile projectile, NPC.HitInfo hit)
	{
		target.AddBuff(BuffID.Slimed, 180);
		SoundEngine.PlaySound(SoundID.NPCDeath28 with { PitchRange = (0.2f, 0.8f), Volume = 0.4f }, target.Center);
	}
}