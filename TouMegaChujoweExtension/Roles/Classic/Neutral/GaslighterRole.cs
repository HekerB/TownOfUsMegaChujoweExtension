using System;
using TownOfUs;
using System.Collections.Generic;
using Il2CppInterop.Runtime.Attributes;
using MiraAPI.GameOptions;
using MiraAPI.Modifiers;
using MiraAPI.Patches.Stubs;
using MiraAPI.Roles;
using Reactor.Networking.Attributes;
using TouMegaChujoweExtension.Assets;
using TouMegaChujoweExtension.Modifiers.Neutral;
using TouMegaChujoweExtension.Networking;
using TouMegaChujoweExtension.Options.Roles.Neutral;
using MiraAPI.Keybinds;
using TownOfUs.Assets;
using TownOfUs.Extensions;
using TownOfUs.Modules.Localization;
using TownOfUs.Modules.Wiki;
using TownOfUs.Roles;
using TownOfUs.Roles.Neutral;
using TownOfUs.Utilities;
using MiraAPI.Utilities;
using UnityEngine;

namespace TouMegaChujoweExtension.Roles.Neutral;

public enum GaslighterAbility
{
    Kill = 0,
    Knight = 1,
    Curse = 2,
    Shield = 3
}

public sealed class GaslighterRole(IntPtr cppPtr) : NeutralRole(cppPtr), ITownOfUsRole, IWikiDiscoverable
{
    public string LocaleKey => "Gaslighter";
    public string RoleName => TouLocale.Get($"ExtensionRole{LocaleKey}", "Gaslighter");
    public string RoleDescription => TouLocale.GetParsed($"ExtensionRole{LocaleKey}IntroBlurb");
    public string RoleLongDescription => TouLocale.GetParsed($"ExtensionRole{LocaleKey}TabDescription");

    public string GetAdvancedDescription()
    {
        return TouLocale.GetParsed($"ExtensionRole{LocaleKey}WikiDescription") + MiscUtils.AppendOptionsText(GetType());
    }

    public Color RoleColor => TouExtensionColors.Gaslighter;
    public ModdedRoleTeams Team => ModdedRoleTeams.Custom;
    public RoleAlignment RoleAlignment => RoleAlignment.NeutralOutlier;

    public CustomRoleConfiguration Configuration => new(this)
    {
        UseVanillaKillButton = false,
        CanUseVent = OptionGroupSingleton<GaslighterOptions>.Instance.CanVent,
        Icon = TouRoleIcons.Vampire, // Placeholder
        IntroSound = TouAudio.MediumIntroSound
    };

    [HideFromIl2Cpp]
    public List<CustomButtonWikiDescription> Abilities =>
    [
        new("Kill", "Kill players in the 1st round of each cycle.", TouAssets.KillSprite),
        new("Knight", "Grant extra votes to players in the 2nd round of each cycle.", TouRoleIcons.Monarch),
        new("Curse", "Mark players for death in the 3rd round of each cycle.", TouExtensionImpAssets.SpellButtonSprite),
        new("Shield", "Protect players in the 4th round of each cycle.", TouRoleIcons.Medic)
    ];

    public int MeetingCount { get; set; } = 0;

    /// <summary>
    /// The randomly assigned ability for the current round.
    /// </summary>
    public GaslighterAbility CurrentCycleAbility { get; set; } = GaslighterAbility.Kill;

    private static readonly System.Random _rng = new();

    /// <summary>
    /// Picks a new random ability from the 4 available for the next round.
    /// </summary>
    public void RandomizeAbility()
    {
        CurrentCycleAbility = (GaslighterAbility)_rng.Next(0, 4);
    }

    public override void Initialize(PlayerControl player)
    {
        RoleBehaviourStubs.Initialize(this, player);
        RandomizeAbility(); // First round gets a random ability

        if (Player.AmOwner && OptionGroupSingleton<GaslighterOptions>.Instance.CanVent)
        {
            HudManager.Instance.ImpostorVentButton.buttonLabelText.SetOutlineColor(TouExtensionColors.Gaslighter);
        }
    }

    public override void SpawnTaskHeader(PlayerControl playerControl)
    {
        if (playerControl != PlayerControl.LocalPlayer) return;
        ImportantTextTask orCreateTask = PlayerTask.GetOrCreateTask<ImportantTextTask>(playerControl, 0);
        orCreateTask.Text = $"{TownOfUsColors.Neutral.ToTextColor()}{TouLocale.GetParsed("NeutralOutlierTaskHeader")}</color>";
    }

    public override void Deinitialize(PlayerControl targetPlayer)
    {
        RoleBehaviourStubs.Deinitialize(this, targetPlayer);

        if (Player.AmOwner && OptionGroupSingleton<GaslighterOptions>.Instance.CanVent)
        {
            HudManager.Instance.ImpostorVentButton.buttonLabelText.SetOutlineColor(TownOfUsColors.Impostor);
        }
    }

    public override bool CanUse(IUsable usable)
    {
        if (!GameManager.Instance.LogicUsables.CanUse(usable, Player)) return false;
        var console = usable.TryCast<Console>();
        return console == null || console.AllowImpostor;
    }

    public override bool DidWin(GameOverReason gameOverReason)
    {
        // Neutral Outlier: wins alongside whoever wins if alive at end
        return !Player.HasDied();
    }

    [MethodRpc((uint)ExtensionRpc.GaslighterKnight)]
    public static void RpcGaslighterKnight(PlayerControl sender, PlayerControl target)
    {
        if (sender.Data.Role is not GaslighterRole) return;

        var targetName = target.CachedPlayerData.PlayerName;
        var icon = TouRoleIcons.Monarch.LoadAsset();

        if (target.HasDied())
        {
            if (sender.AmOwner)
            {
                ShowNotification($"{targetName} died before you could knight them.");
            }
            return;
        }

        target.AddModifier<GaslighterKnightedModifier>();
        target.AddModifier<TownOfUs.Modifiers.KnightedModifier>();

        if (sender.AmOwner)
        {
            ShowNotification($"{targetName} was knighted!");
        }

        if (target.AmOwner)
        {
            ShowNotification($"You were knighted by a {TownOfUsColors.Monarch.ToTextColor()}Monarch</color>. You gained {(int)OptionGroupSingleton<TownOfUs.Options.Roles.Crewmate.MonarchOptions>.Instance.VotesPerKnight} vote(s)!");
        }

        void ShowNotification(string message)
        {
            var notif = Helpers.CreateAndShowNotification($"<b>{message}</b>", Color.white, new Vector3(0f, 1f, -20f), spr: icon);
            notif.Text.SetOutlineThickness(0.35f);
        }
    }

    [MethodRpc((uint)ExtensionRpc.GaslighterCurse)]
    public static void RpcGaslighterCurse(PlayerControl sender, PlayerControl target)
    {
        if (sender.Data.Role is not GaslighterRole role) return;
        if (target == null || target.HasDied()) return;

        var shouldSpell = true;
        if (target.HasModifier<TownOfUs.Modifiers.Neutral.GuardianAngelProtectModifier>())
        {
            shouldSpell = false;
        }

        if (shouldSpell && !target.HasModifier<GaslighterCursedModifier>())
        {
            target.AddModifier<GaslighterCursedModifier>(sender.PlayerId, role.MeetingCount);
        }

        if (shouldSpell)
        {
            if (PlayerControl.LocalPlayer == sender)
            {
                TouAudio.PlaySound(TouExtensionAudio.WitchLaugh);
            }
        }
    }

    [MethodRpc((uint)ExtensionRpc.GaslighterShield)]
    public static void RpcGaslighterShield(PlayerControl sender, PlayerControl target)
    {
        if (sender.Data.Role is not GaslighterRole) return;
        if (target == null || target.HasDied()) return;

        // Ensure only one player can have the Gaslighter shield at a time globally
        foreach (var player in PlayerControl.AllPlayerControls)
        {
            if (player != null && player.TryGetModifier<GaslighterShieldModifier>(out var mod))
            {
                player.RemoveModifier(mod);
            }
        }

        target.AddModifier<GaslighterShieldModifier>();
    }

    public void OnMeetingEnd()
    {

        // Handle Curse kills at the end of meeting
        if (AmongUsClient.Instance.AmHost)
        {
            foreach (var pc in PlayerControl.AllPlayerControls)
            {
                if (pc != null && !pc.HasDied() && pc.TryGetModifier<GaslighterCursedModifier>(out var curse))
                {
                    if (curse.GaslighterId == Player.PlayerId)
                    {
                        Player.RpcSpecialMurder(pc, isIndirect: true, teleportMurderer: false, causeOfDeath: "Gaslighted");
                        pc.RemoveModifier(curse);
                    }
                }
            }
        }
    }

    public override void OnMeetingStart()
    {
        RoleBehaviourStubs.OnMeetingStart(this);
        MeetingCount++;
        RandomizeAbility(); // Randomize ability for the next round
    }
}
