using AmongUs.Data.Player;
using AmongUs.Data;
using HarmonyLib;
using Il2CppInterop.Runtime.InteropTypes.Arrays;
using Reactor.Utilities;
using static Reactor.Utilities.Logger<TouMegaChujoweExtension.TouMegaChujoweExtensionPlugin>;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Collections;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using System;
using TownOfUs.Assets;
using TownOfUs;
using UnityEngine.Networking;
using UnityEngine;

namespace TouMegaChujoweExtension.Patches.UI;

public class ExtensionModNews
{
    public ExtensionModNews(int Number, string Title, string SubTitle, string ShortTitle, string Text, string Date)
    {
        this.Number = Number;
        this.Title = Title;
        this.SubTitle = SubTitle;
        this.ShortTitle = ShortTitle;
        this.Text = Text;
        this.Date = Date;
    }

    public global::Assets.InnerNet.Announcement ToAnnouncement()
    {
        return new global::Assets.InnerNet.Announcement
        {
            Date = Date,
            Number = Number,
            ShortTitle = ShortTitle,
            SubTitle = SubTitle,
            Title = Title,
            Text = Text,
            Language = (uint)DataManager.Settings.Language.CurrentLanguage,
            Id = "ExtensionModNews"
        };
    }

    public string Date { get; set; }
    public int Number { get; set; }
    public string ShortTitle { get; set; }
    public string SubTitle { get; set; }
    public string Title { get; set; }
    public string Text { get; set; }
}

public static class ExtensionModNewsFetcher
{
    private const string ModNewsUrl =
        "https://raw.githubusercontent.com/HekerB/TownOfUsMegaChujoweExtension/main/TouMegaChujoweExtension/Resources/Announcements/news.json";

    private static bool _downloaded;

    public static void CheckForNews()
    {
        Info("Running Extension Mod News Fetcher...");
        Coroutines.Start(FetchNews());
    }

    public static IEnumerator FetchNews()
    {
        if (_downloaded)
        {
            yield break;
        }

        _downloaded = true;
        
        // Cache buster
        var urlWithCacheBuster = $"{ModNewsUrl}?t={DateTime.UtcNow.Ticks}";
        var request = UnityWebRequest.Get(urlWithCacheBuster);
        yield return request.SendWebRequest();

        if (request.result is UnityWebRequest.Result.ConnectionError or UnityWebRequest.Result.ProtocolError)
        {
            _downloaded = false;
            Error($"[ExtensionNews] Couldn't fetch mod news from github: {request.error}");
            LoadNewsFromResources();
            yield break;
        }

        try
        {
            using var jsonDocument = JsonDocument.Parse(request.downloadHandler.text);
            var newsArray = jsonDocument.RootElement.GetProperty("News");

            ExtensionModNewsHistory.AllModNews = ImmutableList<ExtensionModNews>.Empty;

            foreach (var newsElement in newsArray.EnumerateArray())
            {
                var dateString = newsElement.GetProperty("Date").GetString() ?? "Unknown Date";
                var numberString = newsElement.GetProperty("Number").GetString();
                var number = numberString != null ? int.Parse(numberString, TouMegaChujoweExtensionPlugin.Culture) : 0;
                var shortTitle = CensorNewsTitle(newsElement.GetProperty("ShortTitle").GetString() ?? "No Short Title");
                var subTitle = CensorNewsTitle(newsElement.GetProperty("SubTitle").GetString() ?? "No Subtitle");
                var title = CensorNewsTitle(newsElement.GetProperty("Title").GetString() ?? "No Title");
                
                // Join with empty string to maintain formatting from news.json
                var body = string.Join("",
                    newsElement.GetProperty("Text").EnumerateArray().Select(element => element.GetString()));
                
                var modNew = new ExtensionModNews(number, title, subTitle, shortTitle, body, dateString);
                ExtensionModNewsHistory.AllModNews = ExtensionModNewsHistory.AllModNews.Add(modNew);
            }
            Info($"[ExtensionNews] Successfully fetched {ExtensionModNewsHistory.AllModNews.Count} news from GitHub.");
        }
        catch (Exception ex)
        {
            Error($"[ExtensionNews] Couldn't fetch mod news from github, loading from resources instead: {ex.Message}");
            LoadNewsFromResources();
        }
    }

    private static void LoadNewsFromResources()
    {
        try 
        {
            var assembly = Assembly.GetExecutingAssembly();
            var resourceName = "TouMegaChujoweExtension.Resources.Announcements.news.json";

            using var resourceStream = assembly.GetManifestResourceStream(resourceName);
            if (resourceStream == null)
            {
                Error($"[ExtensionNews] Resource not found: {resourceName}");
                return;
            }

            using StreamReader reader = new(resourceStream);
            using var jsonDocument = JsonDocument.Parse(reader.ReadToEnd());
            var newsArray = jsonDocument.RootElement.GetProperty("News");

            ExtensionModNewsHistory.AllModNews = ImmutableList<ExtensionModNews>.Empty;

            foreach (var newsElement in newsArray.EnumerateArray())
            {
                var dateString = newsElement.GetProperty("Date").GetString() ?? "Unknown Date";
                var numberString = newsElement.GetProperty("Number").GetString();
                var number = numberString != null ? int.Parse(numberString, TouMegaChujoweExtensionPlugin.Culture) : 0;
                var shortTitle = CensorNewsTitle(newsElement.GetProperty("ShortTitle").GetString() ?? "No Short Title");
                var subTitle = CensorNewsTitle(newsElement.GetProperty("SubTitle").GetString() ?? "No Subtitle");
                var title = CensorNewsTitle(newsElement.GetProperty("Title").GetString() ?? "No Title");
                var body = string.Join("",
                    newsElement.GetProperty("Text").EnumerateArray().Select(element => element.GetString()));
                
                var modNew = new ExtensionModNews(number, title, subTitle, shortTitle, body, dateString);
                ExtensionModNewsHistory.AllModNews = ExtensionModNewsHistory.AllModNews.Add(modNew);
            }
            Info($"[ExtensionNews] Successfully loaded {ExtensionModNewsHistory.AllModNews.Count} news from resources.");
        }
        catch (Exception ex)
        {
            Error($"[ExtensionNews] Error loading local news: {ex.Message}");
        }
    }

    private static string CensorNewsTitle(string text)
        => TouMegaChujoweExtensionPlugin.CensorVisibleText(text);

    [HarmonyPatch]
    public static class ExtensionModNewsHistory
    {
        public static ImmutableList<ExtensionModNews> AllModNews = ImmutableList<ExtensionModNews>.Empty;

        [HarmonyPatch(typeof(PlayerAnnouncementData), nameof(PlayerAnnouncementData.SetAnnouncements))]
        [HarmonyPrefix]
        public static void SetModAnnouncements_Prefix(ref Il2CppReferenceArray<global::Assets.InnerNet.Announcement> aRange)
        {
            if (AllModNews.Count == 0)
            {
                Error($"No mod news were found.");
                return;
            }

            var aArray = aRange.ToArray();

            var finalAllNews = AllModNews.Select(n => n.ToAnnouncement()).ToList();
            finalAllNews.AddRange(aArray.Where(news => AllModNews.All(x => x.Number != news.Number)));
            finalAllNews.Sort((a1, a2) => DateTime.Compare(
                DateTime.Parse(a2.Date, TouMegaChujoweExtensionPlugin.Culture),
                DateTime.Parse(a1.Date, TouMegaChujoweExtensionPlugin.Culture)));

            var newArray = new global::Assets.InnerNet.Announcement[finalAllNews.Count];
            for (var i = 0; i < finalAllNews.Count; i++)
            {
                newArray[i] = finalAllNews[i];
            }

            aRange = newArray;
        }

        [HarmonyPatch(typeof(AnnouncementPanel), nameof(AnnouncementPanel.SetUp))]
        [HarmonyPostfix]
        public static void SetUpPanel_Postfix(AnnouncementPanel __instance,
            [HarmonyArgument(0)] global::Assets.InnerNet.Announcement announcement)
        {
            // We use Number 200000+ for our extension news
            if (announcement.Number < 200000)
            {
                return;
            }

            var obj = new GameObject("ModLabel");
            obj.transform.SetParent(__instance.transform);
            obj.transform.localPosition = new Vector3(-0.8f, 0.13f, 0.5f);
            obj.transform.localScale = new Vector3(0.9f, 0.9f, 0.9f);
            var renderer = obj.AddComponent<SpriteRenderer>();
            renderer.sprite = TouAssets.AuAvengersSprite.LoadAsset();
            renderer.maskInteraction = SpriteMaskInteraction.VisibleInsideMask;
        }
    }
}











