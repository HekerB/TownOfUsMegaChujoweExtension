using System;
using System.Collections.Generic;
using System.Text;
using Il2CppInterop.Runtime.Attributes;
using MiraAPI.GameOptions;
using MiraAPI.Hud;
using MiraAPI.Modifiers;
using MiraAPI.Roles;
using MiraAPI.Patches.Stubs;
using TouMegaChujoweExtension.Options.Roles.Neutral;
using TouMegaChujoweExtension.Assets;
using TouMegaChujoweExtension.Buttons.Classic.Neutral;
using TownOfUs;
using TownOfUs.Assets;
using TownOfUs.Extensions;
using TownOfUs.Modifiers;
using TownOfUs.Modules.Localization;
using TownOfUs.Modules.Wiki;
using TownOfUs.Roles;
using TownOfUs.Roles.Neutral;
using TownOfUs.Utilities;
using TownOfUs.Utilities.Appearances;
using TownOfUs.Patches;
using Reactor.Utilities;
using UnityEngine;
using UnityEngine.UI;

namespace TouMegaChujoweExtension.Roles.Classic.Neutral;

public sealed class PoltergeistRole(IntPtr cppPtr)
    : NeutralGhostRole(cppPtr), ITownOfUsRole, IGhostRole, IWikiDiscoverable
{
    public override void SpawnTaskHeader(PlayerControl playerControl)
    {
        if (playerControl != PlayerControl.LocalPlayer)
        {
            return;
        }
        ImportantTextTask orCreateTask = PlayerTask.GetOrCreateTask<ImportantTextTask>(playerControl, 0);
        orCreateTask.Text = $"{RoleColor.ToTextColor()}{RoleName}</color>";
    }

    public int DecoysReported { get; set; } = 0;

    public bool Setup { get; set; }
    public bool Caught { get; set; }
    public bool Faded { get; set; }

    public bool CanBeClicked
    {
        get
        {
            if (Caught) return false;
            var req = (int)OptionGroupSingleton<PoltergeistOptions>.Instance.DecoysReportedBeforeClickable;
            return DecoysReported >= req;
        }
        set {}
    }

    public bool GhostActive => Setup && !Caught;

    public bool CanCatch()
    {
        return true;
    }

    public void Spawn()
    {
        Setup = true;

        if (HudManagerPatches.CamouflageCommsEnabled)
        {
            Player.SetCamouflage(false);
        }

        Player.gameObject.layer = LayerMask.NameToLayer("Players");

        Player.gameObject.GetComponent<PassiveButton>().OnClick = new Button.ButtonClickedEvent();
        Player.gameObject.GetComponent<PassiveButton>().OnClick.AddListener((Action)(() => {
            if (PlayerControl.LocalPlayer != null && PlayerControl.LocalPlayer.PlayerId != Player.PlayerId)
            {
                Player.OnClick();
            }
        }));
        Player.gameObject.GetComponent<BoxCollider2D>().enabled = true;

        if (Player.AmOwner)
        {
            Player.SpawnAtRandomVent();
            Player.MyPhysics.ResetMoveState();

            HudManager.Instance.SetHudActive(false);
            HudManager.Instance.SetHudActive(true);
            HudManager.Instance.AbilityButton.SetDisabled();
            HudManagerPatches.ResetZoom();
        }
    }

    public void FadeUpdate()
    {
        if (!Caught && Setup)
        {
            Player.GhostFade();
            Faded = true;
        }
        else if (Faded)
        {
            Player.ResetAppearance();
            Player.cosmetics.ToggleNameVisible(true);

            Player.cosmetics.currentBodySprite.BodySprite.color = Color.white;
            Player.gameObject.layer = LayerMask.NameToLayer("Ghost");
            Player.MyPhysics.ResetMoveState();

            Faded = false;
        }
    }

    public void FixedUpdate()
    {
        if (Player == null || Player.Data.Role is not PoltergeistRole || MeetingHud.Instance)
        {
            return;
        }

        FadeUpdate();
    }

    public void Clicked()
    {
        Caught = true;
        Player.Exiled();

        if (Player.AmOwner)
        {
            HudManager.Instance.AbilityButton.SetEnabled();
            var decoyBtn = CustomButtonSingleton<PoltergeistDecoyButton>.Instance;
            if (decoyBtn != null)
            {
                decoyBtn.SetActive(false, this);
            }
        }
    }

    public string LocaleKey => "Poltergeist";
    public override string RoleName => TouLocale.Get($"ExtensionRole{LocaleKey}", "Poltergeist");
    public override string RoleDescription => TouLocale.GetParsed($"ExtensionRole{LocaleKey}IntroBlurb");
    public override string RoleLongDescription => TouLocale.GetParsed($"ExtensionRole{LocaleKey}TabDescription");

    [HideFromIl2Cpp]
    public StringBuilder SetTabText()
    {
        var sb = ITownOfUsRole.SetNewTabText(this);
        var winCount = (int)OptionGroupSingleton<PoltergeistOptions>.Instance.RequiredDecoysReported;
        sb.AppendLine();
        sb.AppendLine(TownOfUsPlugin.Culture, $"<size=70%><color=#{ColorUtility.ToHtmlStringRGBA(RoleColor)}>Decoy Traps Triggered: {DecoysReported} / {winCount}</color></size>");
        return sb;
    }

    public string GetAdvancedDescription()
    {
        return TouLocale.GetParsed($"ExtensionRole{LocaleKey}WikiDescription") + MiscUtils.AppendOptionsText(GetType());
    }

    public override Color RoleColor => TouExtensionColors.Poltergeist;
    public override RoleAlignment RoleAlignment => RoleAlignment.NeutralAfterlife;
    public ModdedRoleTeams Team => ModdedRoleTeams.Custom;

    public override CustomRoleConfiguration Configuration => new(this)
    {
        Icon = TouExtensionIcons.PoltergeistRoleIcon,
        HideSettings = false,
        ShowInFreeplay = true
    };

    public override bool WinConditionMet()
    {
        return OptionGroupSingleton<PoltergeistOptions>.Instance.PoltergeistWin == PoltergeistWinOptions.EndsGame &&
               DecoysReported >= (int)OptionGroupSingleton<PoltergeistOptions>.Instance.RequiredDecoysReported;
    }

    public override void UseAbility()
    {
        if (GhostActive)
        {
            return;
        }

        base.UseAbility();
    }

    public override void Initialize(PlayerControl player)
    {
        RoleBehaviourStubs.Initialize(this, player);
        if (!Player.HasModifier<BasicGhostModifier>())
        {
            Player.AddModifier<BasicGhostModifier>();
        }

        if (TutorialManager.InstanceExists)
        {
            Setup = true;

            if (HudManagerPatches.CamouflageCommsEnabled)
            {
                Player.SetCamouflage(false);
            }

            Coroutines.Start(SetTutorialCollider(Player));

            if (Player.AmOwner)
            {
                Player.MyPhysics.ResetMoveState();

                HudManager.Instance.SetHudActive(false);
                HudManager.Instance.SetHudActive(true);
                HudManager.Instance.AbilityButton.SetDisabled();
                HudManagerPatches.ResetZoom();
            }
        }
    }

    private static System.Collections.IEnumerator SetTutorialCollider(PlayerControl player)
    {
        yield return new WaitForSeconds(0.01f);

        player.gameObject.layer = LayerMask.NameToLayer("Players");

        player.gameObject.GetComponent<PassiveButton>().OnClick = new Button.ButtonClickedEvent();
        player.gameObject.GetComponent<PassiveButton>().OnClick.AddListener((Action)(() => {
            if (PlayerControl.LocalPlayer != null && PlayerControl.LocalPlayer.PlayerId != player.PlayerId)
            {
                player.OnClick();
            }
        }));
        player.gameObject.GetComponent<BoxCollider2D>().enabled = true;
    }

    public override void Deinitialize(PlayerControl targetPlayer)
    {
        RoleBehaviourStubs.Deinitialize(this, targetPlayer);
        if (TutorialManager.InstanceExists)
        {
            Player.ResetAppearance();
            Player.cosmetics.ToggleNameVisible(true);

            Player.cosmetics.currentBodySprite.BodySprite.color = Color.white;
            Player.gameObject.layer = LayerMask.NameToLayer("Ghost");
            Player.MyPhysics.ResetMoveState();

            Faded = false;
        }
    }

    public override bool CanUse(IUsable console)
    {
        var validUsable = console.TryCast<Console>() ||
                          console.TryCast<DoorConsole>() ||
                          console.TryCast<OpenDoorConsole>() ||
                          console.TryCast<DeconControl>() ||
                          console.TryCast<PlatformConsole>() ||
                          console.TryCast<Ladder>() ||
                          console.TryCast<ZiplineConsole>();

        return GhostActive && validUsable;
    }

    public override bool DidWin(GameOverReason gameOverReason)
    {
        return DecoysReported >= (int)OptionGroupSingleton<PoltergeistOptions>.Instance.RequiredDecoysReported;
    }

    [HideFromIl2Cpp]
    public List<Type> RoleButtons => new List<Type> { typeof(PoltergeistDecoyButton) };

    [HideFromIl2Cpp]
    public List<CustomButtonWikiDescription> Abilities =>
    [
        new(
            TouLocale.GetParsed("Decoy", "Decoy"),
            TouLocale.GetParsed("ExtensionRolePoltergeistDecoyWikiDescription", "Place a fake crewmate body that triggers a point for you when someone tries to report it."),
            TouExtensionIcons.PoltergeistRoleIcon)
    ];

    public void CheckWinConditions()
    {
        SpawnTaskHeader(Player);
    }
}
