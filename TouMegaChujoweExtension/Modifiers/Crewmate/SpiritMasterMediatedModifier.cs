using MiraAPI.Events;
using MiraAPI.GameOptions;
using MiraAPI.Hud;
using MiraAPI.Modifiers;
using Reactor.Utilities;
using Reactor.Utilities.Extensions;
using TouMegaChujoweExtension.Buttons.Crewmate;
using TouMegaChujoweExtension.Options.Roles.Crewmate;
using TouMegaChujoweExtension.Roles.Classic.Crewmate;
using TownOfUs;
using TownOfUs.Events;
using TownOfUs.Events.TouEvents;
using TownOfUs.Modules.Localization;
using TownOfUs.Utilities;
using TownOfUs.Utilities.Appearances;

namespace TouMegaChujoweExtension.Modifiers.Crewmate;

public sealed class SpiritMasterMediatedModifier(byte spiritMasterId) : BaseModifier
{
    private ArrowBehaviour? _arrow;
    private SpiritMasterRole? _spiritMaster;
    private PlayerControl? _spiritMasterPlayer;

    public override string ModifierName => TouLocale.Get("ExtensionModifierSpiritMasterMediated", "Mediated");
    public override bool HideOnUi => true;
    public byte SpiritMasterId { get; } = spiritMasterId;

    public override void OnMeetingStart()
    {
        ModifierComponent?.RemoveModifier(this);
    }

    public override void OnActivate()
    {
        _spiritMaster = GameData.Instance.GetPlayerById(SpiritMasterId).Role as SpiritMasterRole;
        _spiritMasterPlayer = _spiritMaster?.Player;

        if (_spiritMasterPlayer == null || _spiritMaster == null || !Player.Data.IsDead)
        {
            ModifierComponent?.RemoveModifier(this);
            return;
        }

        var touAbilityEvent = new TouAbilityEvent(AbilityType.MediumMediate, _spiritMasterPlayer, Player);
        MiraEventManager.InvokeEvent(touAbilityEvent);

        _spiritMaster.MediatedPlayers.Add(this);

        switch (OptionGroupSingleton<SpiritMasterOptions>.Instance.ArrowVisibility.Value)
        {
            case SpiritMasterVisibility.Both:
                var ownerTransform = Player.AmOwner ? _spiritMasterPlayer.transform : Player.transform;
                _arrow = MiscUtils.CreateArrow(ownerTransform, TownOfUsColors.SoulCollector);
                break;

            case SpiritMasterVisibility.ShowSpiritMaster when Player.AmOwner:
                _arrow = MiscUtils.CreateArrow(_spiritMasterPlayer.transform, TownOfUsColors.SoulCollector);
                break;

            case SpiritMasterVisibility.ShowMediated when _spiritMasterPlayer.AmOwner:
                _arrow = MiscUtils.CreateArrow(Player.transform, TownOfUsColors.SoulCollector);
                break;
        }

        if (_spiritMasterPlayer.AmOwner && !OptionGroupSingleton<SpiritMasterOptions>.Instance.RevealMediateAppearance)
        {
            Player.SetCamouflage();
        }

        Coroutines.Start(MiscUtils.CoFlash(TownOfUsColors.SoulCollector, alpha: 0.5f));
    }

    public override void OnDeactivate()
    {
        if (_spiritMaster != null)
        {
            _spiritMaster.MediatedPlayers.Remove(this);
        }

        if (_spiritMasterPlayer != null && _spiritMasterPlayer.AmOwner)
        {
            CustomButtonSingleton<SpiritMasterMediateButton>.Instance.SetTimerPaused(false);
            CustomButtonSingleton<SpiritMasterMediateButton>.Instance.ResetCooldownAndOrEffect();

            if (!OptionGroupSingleton<SpiritMasterOptions>.Instance.RevealMediateAppearance)
            {
                Player.SetCamouflage(false);
            }
        }

        if (_arrow != null)
        {
            _arrow.gameObject.Destroy();
        }
    }

    public override void FixedUpdate()
    {
        if (!Player.Data.IsDead)
        {
            ModifierComponent?.RemoveModifier(this);
            return;
        }

        if (_spiritMasterPlayer != null && _spiritMasterPlayer.AmOwner)
        {
            Player.Visible = true;
        }

        if (_arrow != null && _arrow.target != _arrow.transform.parent.position)
        {
            _arrow.target = _arrow.transform.parent.position;
        }
    }
}
