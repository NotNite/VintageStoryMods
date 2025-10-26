# Updating the emojis

I do this process using my Discord client mod [moonlight](https://moonlight-mod.github.io/). There are other dumps of Discord's emoji shortcode list (such as <https://emzi0767.mzgit.io/discord-emoji/>), but I prefer doing it myself. If you're following along at home, make sure to [enable DevTools](https://moonlight-mod.github.io/ext-dev/devtools/#enabling-devtools) and [setup Spacepack](https://moonlight-mod.github.io/ext-dev/helpful-exts/#spacepack) so these snippets work.

## Updating Twemoji

First, figure out what Twemoji version Discord is using (`v15.1.0` as of writing):

```js
spacepack.inspect(spacepack.findByCode("jdecked/twemoji")[0].id)
````

Grab the Twemoji repository at that version (e.g. `https://github.com/jdecked/twemoji/archive/refs/tags/v15.1.0.zip`) and place it in `../local/twemoji` (such that it's at the root of the repository, so `VintageStoryMods/local/twemoji/assets` exists).

## Extracting Discord's shortcode table

This snippet copies the shortcode JSON to your clipboard. If DevTools is popped out, make sure to tab back into Discord so that the page is in focus (that's what the `setTimeout` is for).

```js
setTimeout(async () => {
  try {
    const shortcodes = spacepack.require(spacepack.findByCode("face_holding_back_tears", "surrogates")[0].id);
    const shortcodesStr = JSON.stringify(shortcodes);
    await navigator.clipboard.writeText(shortcodesStr);
    console.log("ok copied");
  } catch (e) {
    console.error(e);
  }
}, 3000);
````

Paste that file into `../local/shortcodes.json` (such that it's at the root of the repository, so `VintageStoryMods/local/shortcodes.json` exists).

## Generate the new SVGs

Run the script in this folder using [Deno](https://deno.com/):

```shell
deno run --allow-read=../local --allow-read=./assets --allow-write=./assets ./updateEmojis.ts
```
