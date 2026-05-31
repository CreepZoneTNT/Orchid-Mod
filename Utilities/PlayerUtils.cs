using Terraria;
using OrchidMod.Content.Guardian;
using OrchidMod.Content.Shapeshifter;

namespace OrchidMod.Utilities;

public static partial class OrchidUtils
{
	public static OrchidGuardian Guardian(this Player player) => player.GetModPlayer<OrchidGuardian>();
	public static OrchidShapeshifter Shapeshifter(this Player player) => player.GetModPlayer<OrchidShapeshifter>();
	public static OrchidAlchemist Alchemist(this Player player) => player.GetModPlayer<OrchidAlchemist>();
	public static OrchidGambler Gambler(this Player player) => player.GetModPlayer<OrchidGambler>();
}