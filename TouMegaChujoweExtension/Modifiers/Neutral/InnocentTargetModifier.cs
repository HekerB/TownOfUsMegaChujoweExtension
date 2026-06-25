using MiraAPI.Modifiers;
using TouMegaChujoweExtension.Roles.Classic.Neutral;

namespace TouMegaChujoweExtension.Modifiers.Neutral;

public sealed class InnocentTargetModifier(byte innocentPlayerId) : BaseModifier
{
    public byte InnocentPlayerId => innocentPlayerId;

    public override string ModifierName => "Innocent Target";
    public override bool HideOnUi => true;

    public override void OnMeetingStart()
    {
        if (InnocentRole.ActiveInnocents.TryGetValue(InnocentPlayerId, out var innocent))
        {
            if (innocent.TauntedKillerId != Player.PlayerId)
            {
                Player.RemoveModifier(this);
            }
        }
        else
        {
            Player.RemoveModifier(this);
        }
    }
}
