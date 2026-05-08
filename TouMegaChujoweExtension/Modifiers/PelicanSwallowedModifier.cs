using Il2CppInterop.Runtime.Attributes;
using MiraAPI.Modifiers;
using MiraAPI.Modifiers.Types;
using TownOfUs.Interfaces;

namespace TouMegaChujoweExtension.Modifiers;

/// <summary>
/// Applied to players swallowed by the Pelican.
/// Implements IUntransportable so Transporter cannot transport swallowed players.
/// Blocks all abilities and interactions while inside the Pelican's stomach.
/// </summary>
public sealed class PelicanSwallowedModifier : BaseModifier, IUntransportable
{
    public override string ModifierName => "Swallowed";
    public override bool HideOnUi => true;

    // MiraAPI ModifierFactory wymaga bezparametrowego konstruktora.
    // PelicanId ustawiamy RĘCZNIE po AddModifier, nie przez konstruktor.
    [HideFromIl2Cpp]
    public byte PelicanId { get; set; }

    public override void OnActivate()
    {
        // No specific on-activate logic needed.
    }

    public override void OnDeactivate()
    {
        if (Player != null)
        {
            Player.Visible = true;
            Player.moveable = true;
        }
    }
}
