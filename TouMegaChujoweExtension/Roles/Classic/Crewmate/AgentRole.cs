using Il2CppInterop.Runtime.Attributes;
using MiraAPI.GameOptions;
using MiraAPI.Patches.Stubs;
using MiraAPI.Roles;
using MiraAPI.Utilities;
using System;
using System.Collections.Generic;
using TownOfUs.Assets;
using TownOfUs.Extensions;
using TownOfUs.Modules.Localization;
using TownOfUs.Modules.Wiki;
using TownOfUs.Roles;
using TownOfUs.Utilities;
using UnityEngine;
using TouMegaChujoweExtension.Options.Roles.Crewmate;
using TouMegaChujoweExtension.Assets;

namespace TouMegaChujoweExtension.Roles.Classic.Crewmate
{
    public sealed class AgentRole(IntPtr cppPtr) : CrewmateRole(cppPtr), ITownOfUsRole, IWikiDiscoverable, IDoomable
    {
        public DoomableType DoomHintType => DoomableType.Insight;
        public string LocaleKey => "Agent";
        public string RoleName => TouLocale.Get($"ExtensionRole{LocaleKey}");
        public string RoleDescription => TouLocale.GetParsed($"ExtensionRole{LocaleKey}IntroBlurb");
        public string RoleLongDescription => TouLocale.GetParsed($"ExtensionRole{LocaleKey}TabDescription");

        public string GetAdvancedDescription()
        {
            return TouLocale.GetParsed($"ExtensionRole{LocaleKey}WikiDescription") + MiscUtils.AppendOptionsText(GetType());
        }

        [HideFromIl2Cpp]
        public List<CustomButtonWikiDescription> Abilities => [];

        public Color RoleColor => TouExtensionColors.Agent;
        public ModdedRoleTeams Team => ModdedRoleTeams.Crewmate;
        public RoleAlignment RoleAlignment => RoleAlignment.CrewmateInvestigative;

        public CustomRoleConfiguration Configuration => new(this)
        {
            Icon = TouExtensionIcons.AgentRoleIcon,
            IntroSound = TouAudio.DetectiveIntroSound,
            OptionsScreenshot = TouBanners.CrewmateRoleBanner,
            CanUseVent = OptionGroupSingleton<AgentOptions>.Instance.CanVent,
        };

        public bool IsGuessable => true;
        public RoleBehaviour AppearAs => this;

        public override void Initialize(PlayerControl player)
        {
            RoleBehaviourStubs.Initialize(this, player);
            if (player.AmOwner)
            {
                RefreshVentButton();
            }
        }

        public override void Deinitialize(PlayerControl targetPlayer)
        {
            RoleBehaviourStubs.Deinitialize(this, targetPlayer);
            if (targetPlayer.AmOwner && HudManager.Instance?.ImpostorVentButton != null)
            {
                HudManager.Instance.ImpostorVentButton.graphic.sprite = TouAssets.VentSprite.LoadAsset();
            }
        }

        public override void OnDeath(DeathReason reason)
        {
            Deinitialize(Player);
        }

        private void RefreshVentButton()
        {
            if (Player == null || !Player.AmOwner || HudManager.Instance?.ImpostorVentButton == null)
                return;

            var ventButton = HudManager.Instance.ImpostorVentButton;
            ventButton.graphic.sprite = TouExtensionNeuAssets.ShroudVentSprite.LoadAsset();
            ventButton.buttonLabelText.SetOutlineColor(RoleColor);
        }
    }
}
