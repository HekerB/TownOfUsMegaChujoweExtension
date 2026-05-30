using System;
using MiraAPI.Utilities.Assets;
using Reactor.Utilities;
using UnityEngine;

namespace TouMegaChujoweExtension.Assets;

public static class TouExtensionAudio
{
    private const string AudioPath = "TouMegaChujoweExtension.Resources.Audio";
    private static readonly Lazy<AssetBundle?> AudioBundle = new(LoadAudioBundle);

    public static LoadableAsset<AudioClip> WitchLaugh { get; } = Audio("witch_laugh", "witch_laugh.wav");
    public static LoadableAsset<AudioClip> ObjectionSound { get; } = Audio("objection", "objection.wav");  // replaced with ours (by radzik360)
    public static LoadableAsset<AudioClip> WraithDashSound { get; } = Audio("wraith_dash", "wraith_dash.wav");
    public static LoadableAsset<AudioClip> LanternBreakSound { get; } = Audio("lantern_break", "lantern_break.wav");
    public static LoadableAsset<AudioClip> DecoyPlaceSound { get; } = Audio("decoy_place", "decoy_place.wav");
    public static LoadableAsset<AudioClip> DecoyDestroySound { get; } = Audio("decoy_destroy", "decoy_destroy.wav");
    public static LoadableAsset<AudioClip> HackerJamSound { get; } = Audio("hacker_jam", "hacker_jam.wav");
    public static LoadableAsset<AudioClip> VultureEatSound { get; } = Audio("vulture_eat", "vulture_eat.wav");
    public static LoadableAsset<AudioClip> DraftPickSound { get; } = Audio("draft_pick", "draft_pick.wav");
    public static LoadableAsset<AudioClip> DraftAlertSound { get; } = Audio("draft_alert", "draft_alert.wav");
    public static LoadableAsset<AudioClip> DraftMusic { get; } = Audio("draft_music", "draft_music.wav");
    public static LoadableAsset<AudioClip> JokerLaugh { get; } = Audio("joker_laugh", "joker_laugh.wav"); //  by radzik360
    public static LoadableAsset<AudioClip> SwallowSound { get; } = Audio("swallow_sound", "swallow_sound.wav"); //  by radzik360
    public static LoadableAsset<AudioClip> DeploySound { get; } = Audio("deploy", "deploy.wav");
    public static LoadableAsset<AudioClip> RcSound { get; } = Audio("RC_sound", "RC_sound.wav");
    public static LoadableAsset<AudioClip> RcExplosionSound { get; } = Audio("RC_explosion", "RC_explosion.wav");
    public static LoadableAsset<AudioClip> RCXDIntro { get; } = Audio("RC_XD_Intro", "RC_XD_Intro.wav");
    public static LoadableAsset<AudioClip> PopeJudgementSound { get; } = Audio("pope_judgement", "pope_judgement.wav");
    public static LoadableAsset<AudioClip> PopeAlarmSound { get; } = Audio("pope_alarm", "pope_alarm.wav");
    public static LoadableAsset<AudioClip> PopeIntroSound { get; } = Audio("pope_intro", "pope_intro.wav");
    public static LoadableAsset<AudioClip> BountyHunterIntroSound { get; } = Audio("bounty_hunter_intro", "bounty_hunter_intro.wav");
    public static LoadableAsset<AudioClip> DraftStartAlert { get; } = Audio("DraftStartHexBombAlarm", "DraftStartHexBombAlarm.wav");   //  lol
    public static LoadableAsset<AudioClip> DeathNoteLaughSound { get; } = Audio("light_laughing", "light_laughing.wav");
    public static LoadableAsset<AudioClip> KamikazeExplodeSound { get; } = Audio("allahu_akbar", "allahu_akbar.wav");    // by radzik360
    public static LoadableAsset<AudioClip> C4Beep { get; } = Audio("c4_beep", "c4_beep.wav");
    public static LoadableAsset<AudioClip> SniperShootSound { get; } = Audio("heavy-sniper-sound", "heavy-sniper-sound.wav");

    private static LoadableAsset<AudioClip> Audio(string bundleAssetName, string resourceFileName)
    {
        var bundle = AudioBundle.Value;
        return bundle == null
            ? new LoadableAudioResourceAsset($"{AudioPath}.{resourceFileName}")
            : new LoadableBundleAsset<AudioClip>(bundleAssetName, bundle);
    }

    private static AssetBundle? LoadAudioBundle()
    {
        try
        {
            return AssetBundleManager.Load("toumega-audio");
        }
        catch
        {
            return null;
        }
    }
}
