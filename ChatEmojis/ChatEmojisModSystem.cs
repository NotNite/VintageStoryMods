using HarmonyLib;
using Vintagestory.API.Client;
using Vintagestory.API.Common;

namespace ChatEmojis;

public class ChatEmojisModSystem : ModSystem {
    public static Dictionary<string, string>? EmojiShortcodes;
    private Harmony? harmony;

    public override void StartClientSide(ICoreClientAPI api) {
        this.harmony = new Harmony(this.Mod.Info.ModID);
        this.harmony.PatchAll();
    }

    public override void AssetsLoaded(ICoreAPI api) {
        EmojiShortcodes = api.Assets.Get<Dictionary<string, string>>("chatemojis:config/emojis.json");
    }

    public override void Dispose() {
        this.harmony?.UnpatchAll(this.Mod.Info.ModID);
        this.harmony = null;
        EmojiShortcodes = null;
    }
}
