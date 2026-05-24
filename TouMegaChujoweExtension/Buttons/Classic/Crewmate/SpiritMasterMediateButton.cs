using MiraAPI.GameOptions;
using MiraAPI.Keybinds;
using MiraAPI.Modifiers;
using MiraAPI.Utilities;
using MiraAPI.Utilities.Assets;
using TouMegaChujoweExtension.Modifiers.Crewmate;
using TouMegaChujoweExtension.Options.Roles.Crewmate;
using TouMegaChujoweExtension.Roles.Classic.Crewmate;
using TownOfUs;
using TownOfUs.Assets;
using TownOfUs.Buttons;
using TownOfUs.Modules.Localization;
using UnityEngine;
using Object = UnityEngine.Object;

namespace TouMegaChujoweExtension.Buttons.Crewmate;

public sealed class SpiritMasterMediateButton : TownOfUsRoleButton<SpiritMasterRole>
{
    public override string Name => TouLocale.GetParsed("ExtensionRoleSpiritMasterMediate", "Mediate");
    public override BaseKeybind Keybind => Keybinds.SecondaryAction;
    public override Color TextOutlineColor => TownOfUsColors.Medium;
    public override float Cooldown => Math.Clamp(OptionGroupSingleton<SpiritMasterOptions>.Instance.MediateCooldown + MapCooldown, 0.001f, 120f);
    public override LoadableAsset<Sprite> Sprite => TouCrewAssets.MediateSprite;

    public override bool ZeroIsInfinite { get; set; } = true;

    protected override void OnClick()
    {
        var deadPlayers = PlayerControl.AllPlayerControls.ToArray()
            .Where(plr => plr.Data.IsDead &&
                          !plr.Data.Disconnected &&
                          Object.FindObjectsOfType<DeadBody>().Any(x => x.ParentId == plr.PlayerId) &&
                          !plr.HasModifier<SpiritMasterMediatedModifier>())
            .ToList();

        if (deadPlayers.Count == 0)
        {
            return;
        }

        var targets = OptionGroupSingleton<SpiritMasterOptions>.Instance.WhoIsRevealed.Value switch
        {
            SpiritMasterRevealedTargets.NewestDead => [deadPlayers[0]],
            SpiritMasterRevealedTargets.AllDead => deadPlayers,
            SpiritMasterRevealedTargets.OldestDead => [deadPlayers[^1]],
            SpiritMasterRevealedTargets.RandomDead => deadPlayers.Randomize(),
            _ => []
        };

        foreach (var plr in targets)
        {
            SpiritMasterRole.RpcMediate(PlayerControl.LocalPlayer, plr);
        }
    }
}
