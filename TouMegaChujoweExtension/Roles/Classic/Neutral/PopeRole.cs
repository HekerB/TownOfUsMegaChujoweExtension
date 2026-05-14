using AmongUs.GameOptions;
using Il2CppInterop.Runtime.Attributes;
using MiraAPI.GameOptions;
using MiraAPI.Modifiers;
using MiraAPI.Patches.Stubs;
using MiraAPI.Roles;
using MiraAPI.Utilities;
using Reactor.Networking.Attributes;
using System.Text;
using TownOfUs.Assets;
using TownOfUs.Events;
using TownOfUs.Extensions;
using TownOfUs.Modifiers;
using TownOfUs.Modules.Localization;
using TownOfUs.Modules.Wiki;
using TownOfUs.Roles.Neutral;
using TownOfUs.Roles;
using TownOfUs.Utilities;
using TownOfUs;
using MiraAPI.Hud;
using TownOfUs.Buttons;
using TouMegaChujoweExtension.Buttons.Classic.Neutral;
using UnityEngine;

namespace TouMegaChujoweExtension.Roles.Classic.Neutral;

public sealed class PopeRole(IntPtr cppPtr) : NeutralRole(cppPtr), ITownOfUsRole, IWikiDiscoverable, IDoomable
{
    public DoomableType DoomHintType => DoomableType.Fearmonger;
    public string LocaleKey => "Pope";
    public string RoleName => TouLocale.Get($"ExtensionRole{LocaleKey}");
    public string RoleDescription => TouLocale.GetParsed($"ExtensionRole{LocaleKey}IntroBlurb");
    public string RoleLongDescription => TouLocale.GetParsed($"ExtensionRole{LocaleKey}TabDescription");

    public string GetAdvancedDescription()
    {
        return TouLocale.GetParsed($"ExtensionRole{LocaleKey}WikiDescription") +
               MiscUtils.AppendOptionsText(GetType());
    }

    [HideFromIl2Cpp]
    public List<CustomButtonWikiDescription> Abilities =>
    [
        new(TouLocale.GetParsed($"ExtensionRole{LocaleKey}Canonize", "Canonize"),
            TouLocale.GetParsed($"ExtensionRole{LocaleKey}CanonizeWikiDescription"),
            TouExtensionNeuAssets.PopeCanonizeButtonSprite),
        new(TouLocale.GetParsed($"ExtensionRole{LocaleKey}Judgement", "Judgement"),
            TouLocale.GetParsed($"ExtensionRole{LocaleKey}JudgementWikiDescription"),
            TouExtensionNeuAssets.PopeJudgementButtonSprite)
    ];

    public Color RoleColor => TouExtensionColors.Pope;
    public ModdedRoleTeams Team => ModdedRoleTeams.Custom;
    public RoleAlignment RoleAlignment => RoleAlignment.NeutralEvil;
    public bool HasImpostorVision => false;

    public CustomRoleConfiguration Configuration => new(this)
    {
        CanUseVent = false,
        Icon = TouExtensionIcons.PopeRoleIcon,
        MaxRoleCount = 1,
        IntroSound = TouExtensionAudio.PopeIntroSound,
        GhostRole = (RoleTypes)RoleId.Get<NeutralGhostRole>(),
        OptionsScreenshot = TouBanners.NeutralRoleBanner,
    };

    public void OffsetButtons()
    {
        var canVent = LocalSettingsTabSingleton<TownOfUsLocalSettings>.Instance.OffsetButtonsToggle.Value;
        var canonize = MiraAPI.Hud.CustomButtonSingleton<PopeCanonizeButton>.Instance;
        var judgement = MiraAPI.Hud.CustomButtonSingleton<PopeJudgementButton>.Instance;
        
        Reactor.Utilities.Coroutines.Start(MiscUtils.CoMoveButtonIndex(canonize, !canVent));
        Reactor.Utilities.Coroutines.Start(MiscUtils.CoMoveButtonIndex(judgement, !canVent));
    }

    public override void Initialize(PlayerControl player)
    {
        RoleBehaviourStubs.Initialize(this, player);
        if (PopeJudgementSystem.Instance != null) PopeJudgementSystem.Instance.BombFinished = false;
        PopeJudgementButton.Reset();

        if (player.AmOwner)
        {
            OffsetButtons();
            HudManager.Instance.ImpostorVentButton.graphic.sprite = TouAssets.VentSprite.LoadAsset();
            HudManager.Instance.ImpostorVentButton.buttonLabelText.SetOutlineColor(RoleColor);
        }
    }

    public override void Deinitialize(PlayerControl targetPlayer)
    {
        RoleBehaviourStubs.Deinitialize(this, targetPlayer);
        if (PopeJudgementSystem.Instance != null) PopeJudgementSystem.Instance.BombFinished = false;

        if (targetPlayer.AmOwner)
        {
            HudManager.Instance.ImpostorVentButton.graphic.sprite = TouAssets.VentSprite.LoadAsset();
            HudManager.Instance.ImpostorVentButton.buttonLabelText.SetOutlineColor(TownOfUsColors.Impostor);
        }
    }

    [HideFromIl2Cpp]
    public StringBuilder SetTabText()
    {
        var stringB = ITownOfUsRole.SetNewTabText(this);
        var alivePlayers = PlayerControl.AllPlayerControls.ToArray()
            .Where(x => !DeathHandlerModifier.IsFullyDead(x)).ToList();

        var canonized = alivePlayers
            .Where(p => p.HasModifier<PopeCanonizedModifier>())
            .ToList();

        var uncanonized = alivePlayers
            .Where(p => p.PlayerId != Player.PlayerId && !p.HasModifier<PopeCanonizedModifier>())
            .ToList();

        if (EveryoneCanonized())
        {
            stringB.Append(TownOfUsPlugin.Culture,
                $"\n<b>{TouLocale.Get("ExtensionRolePopeTabAllCanonized")}</b>");
        }
        else
        {
            if (canonized.Count > 0)
            {
                stringB.Append(TownOfUsPlugin.Culture,
                    $"\n<b>{TouLocale.Get("ExtensionRolePopeTabCanonizedInfo")}</b>");
                foreach (var player in canonized)
                {
                    stringB.Append(TownOfUsPlugin.Culture,
                        $"\n<size=75%>{player.Data.PlayerName}</size>");
                }
            }

            stringB.Append(TownOfUsPlugin.Culture,
                $"\n\n<b>{TouLocale.GetParsed("ExtensionRolePopeTabCounter").Replace("<count>", $"{uncanonized.Count}")}</b>");
        }

        return stringB;
    }

    public static bool EveryoneCanonized()
    {
        var targets = PlayerControl.AllPlayerControls
            .ToArray()
            .Where(p => p.Data.Role is not PopeRole && !p.HasDied())
            .ToList();

        return targets.Count > 0 && targets.All(p => p.HasModifier<PopeCanonizedModifier>());
    }

    public override bool CanUse(IUsable usable)
    {
        if (!GameManager.Instance.LogicUsables.CanUse(usable, Player)) return false;
        var console = usable.TryCast<Console>()!;
        return console == null || console.AllowImpostor;
    }

    public override bool DidWin(GameOverReason gameOverReason)
    {
        return PopeJudgementSystem.GlobalBombFinished;
    }

    public bool WinConditionMet() => PopeJudgementSystem.GlobalBombFinished;

    [MethodRpc((uint)Networking.ExtensionRpc.PopeTriggerJudgement)]
    public static void RpcTriggerJudgement(PlayerControl pope)
    {
        var sabId = (SystemTypes)PopeJudgementSystem.SabotageId;
        if (ShipStatus.Instance == null || !ShipStatus.Instance.Systems.ContainsKey(sabId)) return;

        var sabotage = ShipStatus.Instance.Systems[sabId].TryCast<PopeJudgementSystem>();
        if (sabotage != null)
        {
            sabotage.Stage = PopeJudgementStage.Initiate;
            sabotage.TimeRemaining = 1.5f;
            sabotage.IsDirty = true;
        }
    }
}




















