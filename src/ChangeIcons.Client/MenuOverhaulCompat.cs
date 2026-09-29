using System;
using System.Linq;
using EFT;
using HarmonyLib;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ChangeIcons.Client;

/// <summary>
/// MoxoPixel's Menu Overhaul hides the game's name row on the main screen (the one that carries
/// the icon and colors) and draws its own "NicknameText" label in its accent color. When that mod
/// is installed, this puts your icon and name colors on its label instead.
///
/// Only when you've changed the icon you show in icons.json; otherwise its accent color stays.
/// Found by name at runtime: no reference to the other mod, and nothing happens without it.
/// </summary>
public static class MenuOverhaulCompat
{
    private const string ControllerType = "MoxoPixel.MenuOverhaul.Helpers.PlayerProfileStatsController";
    private const string IconName = "ChangeIcons_Icon";

    public static void TryPatch(Harmony harmony)
    {
        var type = AccessTools.TypeByName(ControllerType);
        var method = type == null ? null : AccessTools.Method(type, "UpdateTextColors", new[] { typeof(GameObject) });
        if (method == null)
        {
            return;
        }

        try
        {
            // Every color refresh it does comes through here, after it sets the nickname
            harmony.Patch(method, postfix: new HarmonyMethod(typeof(MenuOverhaulCompat), nameof(Postfix)));
            Plugin.Log.LogInfo("MoxoPixel Menu Overhaul found: your icon and name colors go on its main screen name");
        }
        catch (Exception e)
        {
            Plugin.Log.LogWarning($"MoxoPixel Menu Overhaul found, but couldn't hook its name: {e.Message}");
        }
    }

    public static void Postfix(GameObject clonedPlayerModelView)
    {
        try
        {
            var label = clonedPlayerModelView == null ? null : clonedPlayerModelView.transform.Find("BottomField/NicknameText")?.GetComponent<TextMeshProUGUI>();
            if (label == null)
            {
                return;
            }

            var profile = TarkovApplication.Exist(out var app) ? app.GetClientBackEndSession()?.Profile : null;
            var settings = EFTHardSettings.Instance.ChatSpecialIconSettings;
            var row = profile == null ? null : settings.GetDataByMemberCategory(profile.Info.SelectedMemberCategory);

            // Nothing of yours to show: leave its look alone
            if (row == null || !IconTable.IsCustomized(row.Category))
            {
                NameColorizer.Attach(label, null);
                SetIcon(label, null);
                return;
            }

            if (IconTable.StyleFor(row.Category) != null)
            {
                NameColorizer.Attach(label, row.Category);
            }
            else
            {
                NameColorizer.Attach(label, null);
                label.color = row.IconColor;
            }

            SetIcon(label, row.IconSprite);
        }
        catch (Exception e)
        {
            Plugin.Log.LogWarning($"Couldn't style the Menu Overhaul name: {e.Message}");
        }
    }

    // A square icon just left of the name, sized to the text. A child of the label, so the
    // label's own layout (it sizes to its text) is untouched.
    private static void SetIcon(TextMeshProUGUI label, Sprite sprite)
    {
        var existing = label.transform.Cast<Transform>().FirstOrDefault(t => t.name == IconName);
        if (sprite == null)
        {
            if (existing != null)
            {
                existing.gameObject.SetActive(false);
            }

            return;
        }

        GameObject icon;
        if (existing != null)
        {
            icon = existing.gameObject;
        }
        else
        {
            icon = new GameObject(IconName, typeof(RectTransform), typeof(Image));
            icon.transform.SetParent(label.transform, false);
            icon.GetComponent<Image>().preserveAspect = true;
            icon.GetComponent<Image>().raycastTarget = false;
        }

        var rect = (RectTransform)icon.transform;
        var size = label.fontSize * 0.75f;
        rect.anchorMin = rect.anchorMax = new Vector2(0f, 0.5f);
        rect.pivot = new Vector2(1f, 0.5f);
        rect.sizeDelta = new Vector2(size, size);
        rect.anchoredPosition = new Vector2(-size * 0.3f, 0f);
        icon.GetComponent<Image>().sprite = sprite;
        icon.SetActive(true);
    }
}
