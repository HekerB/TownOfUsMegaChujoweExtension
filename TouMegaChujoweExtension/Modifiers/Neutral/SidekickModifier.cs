using MiraAPI.Modifiers;
using MiraAPI.Modifiers.Types;
using TownOfUs.Modifiers.Game.Alliance;
using TownOfUs.Modifiers.Game;
using UnityEngine;
using MiraAPI.Utilities.Assets;
using TouMegaChujoweExtension.Assets;
using TownOfUs.Modules.Wiki;
using TownOfUs.Roles;
using TownOfUs.Modules.Localization;
using Il2CppInterop.Runtime.Attributes;
using TownOfUs.Utilities;
using MiraAPI.GameOptions;
using TownOfUs;
using MiraAPI.GameEnd;

namespace TouMegaChujoweExtension.Modifiers.Neutral;

public sealed class SidekickModifier : AllianceGameModifier, IWikiDiscoverable
{
    public override string ModifierName => "Recruit";
    public override bool HideOnUi => false;
    public override LoadableAsset<Sprite> ModifierIcon => TouExtensionIcons.SidekickModifierIcon;
    public override Color FreeplayFileColor => TouExtensionColors.Jackal;

    public byte JackalId { get; set; } = 255;
    public bool HasBetrayed { get; set; } = true;
    public bool WasNotified { get; set; } = false;

    public override AlliedFaction TrueFactionType => AlliedFaction.Neutral;
    public override bool CountTowardsTrueFaction => true;
    public override ModifierFaction FactionType => ModifierFaction.Alliance;
    public override bool GetsPunished => false;

    public SidekickModifier() : base() { }

    public SidekickModifier(byte jackalId) : base()
    {
        JackalId = jackalId;
    }

    public override string LocaleKey => "Sidekick";
    public override string GetDescription() => TouLocale.GetParsed("SidekickTabDescription");

    public string ShortName => TouLocale.Get("ExtensionModifierSidekickShortName");

    public override int GetAssignmentChance() => 0;

    [HideFromIl2Cpp]
    public List<CustomButtonWikiDescription> Abilities => [];

    public string GetAdvancedDescription()
    {
        return TouLocale.GetParsed("SidekickWikiDescription") +
               MiscUtils.AppendOptionsText(GetType());
    }

    public override void OnDeactivate()
    {
        base.OnDeactivate();

        // Find the Jackal this recruit was tied to
        var jackalPlayer = PlayerControl.AllPlayerControls.ToArray()
            .FirstOrDefault(p => p != null && p.Pointer != IntPtr.Zero && p.PlayerId == JackalId);

        if (jackalPlayer != null)
        {
            var jackal = jackalPlayer.GetRole<JackalRole>();
            if (jackal != null)
            {
                jackal.OnRecruitDie(); // Count and trigger vengeance if needed
            }
        }
    }

    public override void OnActivate()
    {
        base.OnActivate();
        
        var player = Player;
        if (player == null || JackalId != 255) return;

        // Use the synced dictionary (assigned by host and synced via RPC)
        if (Patches.Roles.Jackal.JackalStartPatch.PendingAssignments.TryGetValue(player.PlayerId, out var jackalId))
        {
            JackalId = jackalId;
            UnityEngine.Debug.Log($"[TOUMCE] Sidekick {player.Data?.PlayerName} discovered their Jackal: {jackalId}");
        }
    }

    public override void Update()
    {
        base.Update();
        
        if (JackalId == 255 && Player != null && Player.Pointer != IntPtr.Zero)
        {
            if (Patches.Roles.Jackal.JackalStartPatch.PendingAssignments.TryGetValue(Player.PlayerId, out var jackalId))
            {
                JackalId = jackalId;
                UnityEngine.Debug.Log($"[TOUMCE] Sidekick {Player.Data?.PlayerName} finally discovered their Jackal: {jackalId}");
            }
        }
    }

    public override bool? DidWin(GameOverReason reason)
    {
        var jackalPlayer = PlayerControl.AllPlayerControls.ToArray()
            .FirstOrDefault(p => p != null && p.Pointer != IntPtr.Zero && p.PlayerId == JackalId);

        if (jackalPlayer != null)
        {
            var jackal = jackalPlayer.GetRole<JackalRole>();
            if (jackal != null)
            {
                return jackal.DidWin(reason);
            }
        }

        return false;
    }
}
