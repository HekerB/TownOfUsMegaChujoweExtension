using MiraAPI.Utilities.Assets;
using UnityEngine;

namespace TouMegaChujoweExtension.Assets;

public static class TouExtensionAudio
{
    private const string AudioPath = "TouMegaChujoweExtension.Resources.Audio";
    public static LoadableAsset<AudioClip> WitchLaugh { get; } = new LoadableAudioResourceAsset($"{AudioPath}.witch_laugh.wav");
    public static LoadableAsset<AudioClip> ObjectionSound { get; } = new LoadableAudioResourceAsset($"{AudioPath}.objection.wav");  // replaced with ours (by radzik360)
    public static LoadableAsset<AudioClip> WraithDashSound { get; } = new LoadableAudioResourceAsset($"{AudioPath}.wraith_dash.wav");
    public static LoadableAsset<AudioClip> LanternBreakSound { get; } = new LoadableAudioResourceAsset($"{AudioPath}.lantern_break.wav");
    public static LoadableAsset<AudioClip> DecoyPlaceSound { get; } = new LoadableAudioResourceAsset($"{AudioPath}.decoy_place.wav");
    public static LoadableAsset<AudioClip> DecoyDestroySound { get; } = new LoadableAudioResourceAsset($"{AudioPath}.decoy_destroy.wav");
    public static LoadableAsset<AudioClip> HackerJamSound { get; } = new LoadableAudioResourceAsset($"{AudioPath}.hacker_jam.wav");
    public static LoadableAsset<AudioClip> VultureEatSound { get; } = new LoadableAudioResourceAsset($"{AudioPath}.vulture_eat.wav");
    public static LoadableAsset<AudioClip> DraftPickSound { get; } = new LoadableAudioResourceAsset($"{AudioPath}.draft_pick.wav");
    public static LoadableAsset<AudioClip> DraftAlertSound { get; } = new LoadableAudioResourceAsset($"{AudioPath}.draft_alert.wav");
    public static LoadableAsset<AudioClip> DraftMusic { get; } = new LoadableAudioResourceAsset($"{AudioPath}.draft_music.wav");
    public static LoadableAsset<AudioClip> JokerLaugh { get; } = new LoadableAudioResourceAsset($"{AudioPath}.joker_laugh.wav"); //  by radzik360
	public static LoadableAsset<AudioClip> SwallowSound { get; } = new LoadableAudioResourceAsset($"{AudioPath}.swallow_sound.wav"); //  by radzik360
	public static LoadableAsset<AudioClip> DeploySound { get; } = new LoadableAudioResourceAsset($"{AudioPath}.deploy.wav");
	public static LoadableAsset<AudioClip> RcSound { get; } = new LoadableAudioResourceAsset($"{AudioPath}.RC_sound.wav");
	public static LoadableAsset<AudioClip> RcExplosionSound { get; } = new LoadableAudioResourceAsset($"{AudioPath}.RC_explosion.wav");
	public static LoadableAsset<AudioClip> RCXDIntro { get; } = new LoadableAudioResourceAsset($"{AudioPath}.RC_XD_Intro.wav");
	public static LoadableAsset<AudioClip> PopeJudgementSound { get; } = new LoadableAudioResourceAsset($"{AudioPath}.pope_judgement.wav");
	public static LoadableAsset<AudioClip> PopeAlarmSound { get; } = new LoadableAudioResourceAsset($"{AudioPath}.pope_alarm.wav");
	public static LoadableAsset<AudioClip> PopeIntroSound { get; } = new LoadableAudioResourceAsset($"{AudioPath}.pope_intro.wav");
	public static LoadableAsset<AudioClip> BountyHunterIntroSound { get; } = new LoadableAudioResourceAsset($"{AudioPath}.bounty_hunter_intro.wav");
	public static LoadableAsset<AudioClip> DraftStartAlert { get; } = new LoadableAudioResourceAsset($"{AudioPath}.DraftStartHexBombAlarm.wav");   //  lol
	public static LoadableAsset<AudioClip> DeathNoteLaughSound { get; } = new LoadableAudioResourceAsset($"{AudioPath}.light_laughing.wav");
	public static LoadableAsset<AudioClip> KamikazeExplodeSound { get; } = new LoadableAudioResourceAsset($"{AudioPath}.allahu_akbar.wav");    // by radzik360
	public static LoadableAsset<AudioClip> C4Beep { get; } = new LoadableAudioResourceAsset($"{AudioPath}.c4_beep.wav");
	public static LoadableAsset<AudioClip> SniperShootSound { get; } = new LoadableAudioResourceAsset($"{AudioPath}.heavy-sniper-sound.wav");
}










