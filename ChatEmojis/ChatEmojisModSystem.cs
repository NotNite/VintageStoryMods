using System.Collections.Frozen;
using HarmonyLib;
using Vintagestory.API.Client;
using Vintagestory.API.Common;

namespace ChatEmojis;

#pragma warning disable CA2211
public class ChatEmojisModSystem : ModSystem {
    public static FrozenDictionary<string, string>? EmojiShortcodes;
    public static FrozenSet<uint>? SingleCodepointEmojis;
    public static FrozenDictionary<uint, FrozenSet<List<uint>>>? MultiCodepointEmojis;

    private Harmony? harmony;

    public override void StartClientSide(ICoreClientAPI api) {
        this.harmony = new Harmony(this.Mod.Info.ModID);
        this.harmony.PatchAll();
    }

    public override void AssetsLoaded(ICoreAPI api) {
        var shortcodes = api.Assets.Get<Dictionary<string, string>>("chatemojis:config/shortcodes.json");
        EmojiShortcodes = shortcodes.ToFrozenDictionary();

        var singleCodepoints = api.Assets.Get<List<uint>>("chatemojis:config/single_codepoints.json");
        SingleCodepointEmojis = singleCodepoints.ToFrozenSet();

        var multiCodepoints =
            api.Assets.Get<Dictionary<uint, List<List<uint>>>>("chatemojis:config/multi_codepoints.json");
        // FrozenSet isn't ordered so the inner part has to be a list still
        MultiCodepointEmojis = multiCodepoints.ToFrozenDictionary(
            kvp => kvp.Key,
            kvp => kvp.Value.Select(l => l).ToFrozenSet()
        );
    }

    public override void Dispose() {
        this.harmony?.UnpatchAll(this.Mod.Info.ModID);
        this.harmony = null;
        EmojiShortcodes = null;
    }
}
