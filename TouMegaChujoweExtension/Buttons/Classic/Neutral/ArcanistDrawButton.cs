using MiraAPI.GameOptions;
using MiraAPI.Keybinds;
using MiraAPI.Utilities.Assets;
using TouMegaChujoweExtension.Assets;
using TouMegaChujoweExtension.Options.Roles.Neutral;
using TouMegaChujoweExtension.Roles.Classic.Neutral;
using TownOfUs.Assets;
using TownOfUs.Buttons;
using TownOfUs.Modules.Localization;
using TownOfUs.Extensions;
using TownOfUs.Roles;
using TownOfUs.Roles.Neutral;
using TownOfUs.Roles.Crewmate;
using TownOfUs.Utilities;
using MiraAPI.Roles;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using RoleTypes = AmongUs.GameOptions.RoleTypes;

namespace TouMegaChujoweExtension.Buttons.Classic.Neutral;

public sealed class ArcanistDrawButton : TownOfUsRoleButton<ArcanistRole>
{
    public override string Name => TouLocale.Get("ExtensionRoleArcanistDraw", "Draw Card");
    public override LoadableAsset<Sprite> Sprite => TouExtensionIcons.ArcanistRoleIcon;
    public override BaseKeybind Keybind => Keybinds.SecondaryAction;

    public override float Cooldown =>
        Math.Clamp(OptionGroupSingleton<ArcanistOptions>.Instance.Cooldown * (Role?.CooldownMultiplier ?? 1f) + MapCooldown, 5f, 120f);

    public override int MaxUses => (int)OptionGroupSingleton<ArcanistOptions>.Instance.DeckSize;
    public override bool ZeroIsInfinite { get; set; } = true;

    public override void CreateButton(Transform parent)
    {
        base.CreateButton(parent);
        if (Button != null)
        {
            Button.transform.localScale = new Vector3(0.8f, 0.8f, 1f);
            Button.usesRemainingSprite.sprite = TouAssets.AbilityCounterBasicSprite.LoadAsset();
            Button.usesRemainingSprite.gameObject.SetActive(MaxUses > 0);
        }
    }

    public override bool CanUse()
    {
        return base.CanUse() && Role != null && Role.CardsLeft > 0;
    }

    public override void ClickHandler()
    {
        if (!CanClick() || Role == null || Role.CardsLeft <= 0) return;

        Timer = Cooldown;
        OnClick();
    }

    protected override void OnClick()
    {
        if (Role == null || Role.CardsLeft <= 0) return;

        var card = ArcanistRole.GetRandomCard();
        ushort subRole = 0;
        byte targetId = 255;

        switch (card)
        {
            case TarotCard.TheHierophant:
                var benigns = new List<ushort> {
                    RoleId.Get<AmnesiacRole>(),
                    RoleId.Get<ExecutionerRole>(),
                    (ushort)RoleTypes.GuardianAngel,
                    RoleId.Get<JesterRole>(),
                    RoleId.Get<SurvivorRole>()
                };
                subRole = benigns[UnityEngine.Random.Range(0, benigns.Count)];
                break;
            case TarotCard.TheLovers:
                var player = PlayerControl.LocalPlayer;
                var evils = PlayerControl.AllPlayerControls.ToArray()
                    .Where(p => p != player && p.Data != null && !p.Data.IsDead && (p.IsImpostorAligned() || p.Is(RoleAlignment.NeutralKilling)))
                    .ToList();
                if (evils.Count > 0)
                {
                    targetId = evils[UnityEngine.Random.Range(0, evils.Count)].PlayerId;
                }
                break;
            case TarotCard.WheelOfFortune:
                if (UnityEngine.Random.Range(0, 100) < 50)
                {
                    var nks = new List<ushort> { RoleId.Get<ArsonistRole>(), RoleId.Get<TouMegaChujoweExtension.Roles.Classic.Neutral.SerialKillerRole>(), RoleId.Get<WerewolfRole>(), RoleId.Get<JuggernautRole>() };
                    subRole = nks[UnityEngine.Random.Range(0, nks.Count)];
                }
                else
                {
                    subRole = RoleId.Get<AmnesiacRole>();
                }
                break;
            case TarotCard.Death:
                if (UnityEngine.Random.Range(0, 100) < 20)
                {
                    subRole = 0xFFFF;
                }
                else
                {
                    var otherRoles = new List<ushort> {
                        RoleId.Get<TouMegaChujoweExtension.Roles.Classic.Crewmate.DoctorRole>(),
                        (ushort)RoleTypes.Engineer,
                        RoleId.Get<MayorRole>()
                    };
                    subRole = otherRoles[UnityEngine.Random.Range(0, otherRoles.Count)];
                }
                break;
        }

        ArcanistRole.RpcDrawCard(PlayerControl.LocalPlayer, card, subRole, targetId);
    }

    protected override void FixedUpdate(PlayerControl playerControl)
    {
        base.FixedUpdate(playerControl);
        if (Role != null)
        {
            SetUses(Role.CardsLeft);
        }
    }
}