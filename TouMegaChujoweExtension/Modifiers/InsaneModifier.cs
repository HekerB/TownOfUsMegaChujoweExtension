using MiraAPI.GameOptions;
using MiraAPI.Modifiers;
using MiraAPI.Utilities.Assets;
using TownOfUs;
using TownOfUs.Assets;
using TownOfUs.Extensions;
using TownOfUs.Modifiers.Game;
using TownOfUs.Modules.Localization;
using TownOfUs.Modules.Wiki;
using TownOfUs.Utilities;
using TouMegaChujoweExtension.Assets;
using TouMegaChujoweExtension.Options;
using UnityEngine;
using System;
using System.Linq;

namespace TouMegaChujoweExtension.Modifiers;

public sealed class InsaneModifier : UniversalGameModifier, IWikiDiscoverable
{
    public override string LocaleKey => "Insane";
    public override string ModifierName => TouLocale.Get($"ExtensionModifier{LocaleKey}", "Insane");
    public override string IntroInfo => TouLocale.GetParsed($"ExtensionModifier{LocaleKey}IntroBlurb");
    public override LoadableAsset<Sprite> ModifierIcon => TouExtensionModifierIcons.InsaneModifierIcon;
    public override bool Unique => true;

    // Crewmate - show only when all tasks are done
    // Neutral - never
    // Impostor - never
    public override bool HideOnUi
    {
        get
        {
            if (Player == null || Player.Data == null)
            {
                return true;
            }

            if (Player.IsCrewmate())
            {
                return !CompletedAllTasks();
            }

            return true;
        }
    }

    public override string GetDescription()
    {
        return TouLocale.GetParsed($"ExtensionModifier{LocaleKey}TabDescription");
    }

    public string GetAdvancedDescription()
    {
        return TouLocale.GetParsed($"ExtensionModifier{LocaleKey}WikiDescription")
               + MiscUtils.AppendOptionsText(GetType());
    }

    public override Color FreeplayFileColor => new Color32(200, 80, 200, 255);
    public override ModifierFaction FactionType => ModifierFaction.UniversalPassive;
    public List<CustomButtonWikiDescription> Abilities { get; } = [];

    public override int GetAssignmentChance()
    {
        return (int)OptionGroupSingleton<UniversalModifierOptions>.Instance.InsaneChance;
    }

    public override int GetAmountPerGame()
    {
        return (int)OptionGroupSingleton<UniversalModifierOptions>.Instance.InsaneAmount;
    }

    public override bool IsModifierValidOn(RoleBehaviour role)
    {
        if (!base.IsModifierValidOn(role))
        {
            return false;
        }

        var player = role.Player;
        if (player == null || player.Data == null || player.Data.IsDead)
        {
            return false;
        }

        return true;
    }

    private bool CompletedAllTasks()
    {
        if (Player == null || Player.Data == null) return false;
        
        var total = 0;
        var completed = 0;
        
        if (Player.myTasks != null && Player.myTasks.Count > 0)
        {
            var tasks = Player.myTasks.ToArray().Where(x => !PlayerTask.TaskIsEmergency(x) && !x.TryCast<ImportantTextTask>());
            foreach (var t in tasks)
            {
                total++;
                var taskInfo = Player.Data.FindTaskById(t.Id);
                var isComplete = taskInfo != null ? taskInfo.Complete : t.IsComplete;
                if (isComplete)
                {
                    completed++;
                }
            }
        }
        else
        {
            foreach (var info in Player.Data.Tasks)
            {
                total++;
                if (info.Complete)
                {
                    completed++;
                }
            }
        }
        
        return total > 0 && completed == total;
    }

    private float _flashTimer = 0f;
    private float _nextFlashInterval = 10f;

    public override void Update()
    {
        base.Update();

        if (Player == null || !Player.AmOwner)
        {
            return;
        }

        // Medic check: random screen flash for local player if they are Medic
        if (Player.IsRole<TownOfUs.Roles.Crewmate.MedicRole>())
        {
            _flashTimer += Time.deltaTime;
            if (_flashTimer >= _nextFlashInterval)
            {
                _flashTimer = 0f;
                _nextFlashInterval = UnityEngine.Random.Range(10f, 25f);
                
                // Trigger screen flash (exactly how Medic flash is triggered)
                Reactor.Utilities.Coroutines.Start(MiscUtils.CoFlash(TownOfUsColors.Medic, alpha: 0.5f));
            }
        }
    }
}
