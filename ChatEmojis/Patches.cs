using System.Text.RegularExpressions;
using HarmonyLib;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.Client.NoObf;

namespace ChatEmojis;

// ReSharper disable InconsistentNaming
// ReSharper disable UnusedMember.Local
[HarmonyPatch]
public partial class Patches {
    [GeneratedRegex(":[\\d+_a-z-]+:")]
    private static partial Regex ShortcodeRegex();

    [GeneratedRegex("<\\/[^>]*> $")]
    private static partial Regex VtmlEndRegex();

    // patch emojis in from shortcode
    [HarmonyPatch(typeof(HudDialogChat), "OnNewServerToClientChatLine")]
    [HarmonyPrefix]
    private static void OnNewServerToClientChatLine(ref string message) {
        message = ReplaceShortcodes(message);
    }

    private static string ReplaceShortcodes(string message) {
        // adjustment for when we edit the size of the string - because matches always go forward in the string, this is safe to do
        var adjustment = 0;

        foreach (var match in ShortcodeRegex().EnumerateMatches(message)) {
            var index = adjustment + match.Index;

            // add one here to remove the colons, remove 2 since it's length and not end pos
            var shortcode = message.Substring(index + 1, match.Length - 2);

            if (ChatEmojisModSystem.EmojiShortcodes?.GetValueOrDefault(shortcode) is { } filename) {
                var replacement = $"<icon path=\"chatemojis:emojis/{filename}\"></icon>";

                // insane bullshit hack: spaces between VTML elements aren't preserved properly. this makes two emojis
                // in a row, or an emoji at the start of the message (because of the name) look wrong. the fix:
                var prev = message[..index];
                if (VtmlEndRegex().IsMatch(prev)) replacement = " " + replacement;

                message = message.Remove(index, match.Length).Insert(index, replacement);
                adjustment += replacement.Length - match.Length;
            }
        }

        return message;
    }

    // resize SVGs to line height
    [HarmonyPatch(typeof(IconComponent), MethodType.Constructor, [
        typeof(ICoreClientAPI),
        typeof(string),
        typeof(string),
        typeof(CairoFont)
    ])]
    [HarmonyPostfix]
    public static void IconComponentCtor(IconComponent __instance, string? ___iconPath) {
        if (___iconPath?.StartsWith("chatemojis:") == true) __instance.sizeMulSvg = 1;
    }

    // don't use a static color when rendering our emojis
    [HarmonyPatch(typeof(SvgLoader), "rasterizeSvg")]
    [HarmonyPrefix]
    private static void RasterizeSvgPrefix(IAsset svgAsset, ref int? color) {
        if (svgAsset.Location.Domain == "chatemojis") color = null;
    }
}
