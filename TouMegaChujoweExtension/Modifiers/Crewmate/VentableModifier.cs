using MiraAPI.GameOptions;
using MiraAPI.Roles;
using MiraAPI.Utilities.Assets;
using TouMegaChujoweExtension.Assets;
using TouMegaChujoweExtension.Options.Modifiers;
using TownOfUs.Interfaces;
using TownOfUs.Modifiers.Game;
using TownOfUs.Modules.Localization;
using TownOfUs.Modules.Wiki;
using TownOfUs.Roles;
using TownOfUs.Roles.Crewmate;
using TownOfUs.Assets;
using TownOfUs.Utilities;
using UnityEngine;

namespace TouMegaChujoweExtension.Modifiers.Crewmate;

public sealed class VentableModifier : TouGameModifier, IWikiDiscoverable, IColoredModifier
{
    public int VentsRemaining { get; set; }
    public float CooldownTimer { get; set; }
    public float VentDurationTimer { get; set; }
    public bool IsShaking { get; set; }
    private Sprite? _cachedVentSprite;

    public override string LocaleKey => "Ventable";
    public override string ModifierName => TouLocale.Get($"ExtensionModifier{LocaleKey}");
    public override string IntroInfo => TouLocale.GetParsed($"ExtensionModifier{LocaleKey}IntroBlurb");
    public Color ModifierColor => TouExtensionColors.Ventable;

    public override string GetDescription()
    {
        if (VentsRemaining <= 0)
            return TouLocale.GetParsed($"ExtensionModifier{LocaleKey}TabDescriptionEmpty");

        return TouLocale.GetParsed($"ExtensionModifier{LocaleKey}TabDescription")
            .Replace("-uses-", VentsRemaining.ToString());
    }

    public string GetAdvancedDescription()
    {
        return TouLocale.GetParsed($"ExtensionModifier{LocaleKey}WikiDescription")
               + MiscUtils.AppendOptionsText(GetType());
    }

    public List<CustomButtonWikiDescription> Abilities { get; } = [];

    public override LoadableAsset<Sprite>? ModifierIcon => TouExtensionModifierIcons.VentableModifierIcon;
    public override Color FreeplayFileColor => new Color32(50, 150, 220, 255);
    public override ModifierFaction FactionType => ModifierFaction.CrewmateUtility;

    public override void OnActivate()
    {
        if (Player.AmOwner)
        {
            VentsRemaining = (int)OptionGroupSingleton<VentableModifierOptions>.Instance.MaxVentUses.Value;
            CooldownTimer = 0f;
            VentDurationTimer = 0f;
            IsShaking = false;
        }
    }

    public override int GetAssignmentChance() =>
        (int)OptionGroupSingleton<CrewmateModifierOptions>.Instance.VentableChance.Value;

    public override int GetAmountPerGame() =>
        (int)OptionGroupSingleton<CrewmateModifierOptions>.Instance.VentableAmount;

    public override bool IsModifierValidOn(RoleBehaviour role)
    {
        if (!base.IsModifierValidOn(role)) return false;
        if (!role.IsCrewmate()) return false;
        if (role is EngineerRole) return false;
        if (role is EngineerTouRole) return false;
        if (role is ICustomRole custom && custom.Configuration.CanUseVent) return false;
        return true;
    }

    public override bool? CanVent()
    {
        if (Player != null && Player.inVent) return true;
        if (VentsRemaining <= 0) return false;
        if (CooldownTimer > 0f) return false;
        return true;
    }

    public override void FixedUpdate()
    {
        if (Player == null || !Player.AmOwner || Player.Data.IsDead) return;

        if (CooldownTimer > 0f)
            CooldownTimer -= Time.fixedDeltaTime;

        if (Player.inVent)
        {
            VentDurationTimer += Time.fixedDeltaTime;
            float maxDuration = OptionGroupSingleton<VentableModifierOptions>.Instance.VentDuration.Value;

            if (maxDuration > 0f)
            {
                float remaining = maxDuration - VentDurationTimer;
                IsShaking = remaining <= 3f && remaining > 0f;

                if (VentDurationTimer >= maxDuration)
                {
                    IsShaking = false;
                    var currentVent = Vent.currentVent;
                    if (currentVent != null)
                    {
                        currentVent.SetButtons(false);
                        if (currentVent.Left != null) currentVent.Left.SetButtons(false);
                        if (currentVent.Right != null) currentVent.Right.SetButtons(false);
                        if (currentVent.Center != null) currentVent.Center.SetButtons(false);

                        Player.MyPhysics.RpcExitVent(currentVent.Id);
                    }
                }
            }
        }
        else
        {
            VentDurationTimer = 0f;
            IsShaking = false;
        }
    }

    public Sprite GetVentSprite()
    {
        _cachedVentSprite ??= TouExtensionModifierIcons.VentableVentButtonSprite.LoadAsset();
        return _cachedVentSprite!;
    }
}
