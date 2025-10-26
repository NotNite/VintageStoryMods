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

    // insane bullshit hack: spaces between VTML elements aren't preserved properly. this makes two emojis
    // in a row, or an emoji at the start of the message (because of the name) look wrong. the fix is to detect
    // the end of a VTML component and add an extra space - this isn't perfect, but it works decently
    [GeneratedRegex("<\\/[^>]*> $")]
    private static partial Regex VtmlEndRegex();

    [HarmonyPatch(typeof(HudDialogChat), "OnNewServerToClientChatLine")]
    [HarmonyPrefix]
    private static void OnNewServerToClientChatLine(ref string message) {
        // codepoints before shortcodes since the VTML causes needless multi-char lookup
        ReplaceCodepoints(ref message);
        ReplaceShortcodes(ref message);
    }

    // patch emojis in from shortcode (e.g. :fire:)
    private static void ReplaceShortcodes(ref string message) {
        // adjustment for when we edit the size of the string - because matches always go forward in the string, this is safe to do
        var adjustment = 0;

        foreach (var match in ShortcodeRegex().EnumerateMatches(message)) {
            var index = adjustment + match.Index;

            // add one here to remove the colons, remove 2 since it's length and not end pos
            var shortcode = message.Substring(index + 1, match.Length - 2);

            if (ChatEmojisModSystem.EmojiShortcodes?.GetValueOrDefault(shortcode) is { } filename) {
                var replacement = $"<icon path=\"chatemojis:emojis/{filename}\"></icon>";

                var prev = message[..index];
                if (VtmlEndRegex().IsMatch(prev)) replacement = " " + replacement;

                message = message.Remove(index, match.Length).Insert(index, replacement);
                adjustment += replacement.Length - match.Length;
            }
        }
    }

    // patch emojis in from codepoints (e.g. \uD83D\uDD25)
    private static void ReplaceCodepoints(ref string message) {
        // unlike ReplaceShortcodes, we have to index by the absolute position anyway
        var stringPosition = 0;

        var runes = message.EnumerateRunes().ToList();
        for (var runeIdx = 0; runeIdx < runes.Count; runeIdx++) {
            var rune = runes[runeIdx];
            var codepoint = (uint) rune.Value;
            var runeLength = rune.Utf16SequenceLength;

            if (ChatEmojisModSystem.MultiCodepointEmojis?.GetValueOrDefault(codepoint) is { } sequences) {
                var found = false;

                // peek ahead using the first character as a weak form of optimization
                // this is a list of sequences because a single starting character can lead to multiple emojis
                // (e.g. U+1F468 "MAN" for all the possible combinations)
                foreach (var sequence in sequences) {
                    if (runeIdx + sequence.Count >= runes.Count) continue;

                    var snippet = runes.Slice(runeIdx + 1, sequence.Count);
                    if (snippet.Select(r => (uint) r.Value).SequenceEqual(sequence)) {
                        // we do this in the SVG copy script, so have to do it here too
                        // https://github.com/jdecked/twemoji/blob/50c7abfe6813680455781862f7b34305cd1eb9f5/scripts/build.js#L344
                        List<uint> codepoints = [codepoint, ..sequence];
                        if (codepoints.Contains(0xFE0F) && !codepoints.Contains(0x200D)) {
                            codepoints = codepoints.Where(p => p != 0xFE0F).ToList();
                        }

                        var filename = string.Join('-', codepoints.Select(c => $"{c:x}")) + ".svg";

                        var replacement = $"<icon path=\"chatemojis:emojis/{filename}\"></icon>";

                        var prev = message[..stringPosition];
                        if (VtmlEndRegex().IsMatch(prev)) replacement = " " + replacement;

                        var runesLength = runeLength + snippet.Sum(r => r.Utf16SequenceLength);
                        message = message.Remove(stringPosition, runesLength).Insert(stringPosition, replacement);

                        stringPosition += replacement.Length;
                        runeIdx += sequence.Count;
                        found = true;
                        break;
                    }
                }

                if (found) continue;
            }

            if (ChatEmojisModSystem.SingleCodepointEmojis?.Contains(codepoint) == true) {
                // emoji fits in one codepoint, it's fine to directly replace
                // this goes after multi codepoint emojis since they may use single codepoint emojis
                // (e.g. flags use regional indicators)

                var filename = $"{codepoint:x}.svg";
                var replacement = $"<icon path=\"chatemojis:emojis/{filename}\"></icon>";

                var prev = message[..stringPosition];
                if (VtmlEndRegex().IsMatch(prev)) replacement = " " + replacement;

                message = message.Remove(stringPosition, runeLength).Insert(stringPosition, replacement);
                stringPosition += replacement.Length;
                continue;
            }

            // normal character
            stringPosition += runeLength;
        }
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

    // premultiply emoji after rasterization so they don't look crusty
    // thanks @belomaximka in the Vintage Story Discord server :purple_heart:
    [HarmonyPatch(typeof(SvgLoader), "rasterizeSvg")]
    [HarmonyPostfix]
    public static void RasterizeSvgPostfix(IAsset svgAsset, ref byte[] __result) {
        if (svgAsset.Location.Domain == "chatemojis") {
            var pixelCount = __result.Length / 4;

            for (var p = 0; p < pixelCount; p++) {
                var i = p * 4;
                var r = __result[i + 0];
                var g = __result[i + 1];
                var b = __result[i + 2];
                var a = __result[i + 3];

                if (a == 255) continue;
                if (a == 0) {
                    __result[i + 0] = 0;
                    __result[i + 1] = 0;
                    __result[i + 2] = 0;
                    continue;
                }

                __result[i + 0] = (byte) ((r * a + 127) / 255);
                __result[i + 1] = (byte) ((g * a + 127) / 255);
                __result[i + 2] = (byte) ((b * a + 127) / 255);
            }
        }
    }
}
