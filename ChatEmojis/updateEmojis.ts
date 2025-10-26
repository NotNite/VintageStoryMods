// simple deno script to copy emoji SVGs

// nice default type
import _twemoji from "@twemoji/api";
const twemoji = _twemoji as unknown as (typeof _twemoji)["default"];

const twemojiPath = "../local/twemoji/assets/svg";
const shortcodesPath = "./assets/chatemojis/config/shortcodes.json";
const singleCodepointsPath = "./assets/chatemojis/config/single_codepoints.json";
const multiCodepointsPath = "./assets/chatemojis/config/multi_codepoints.json";
const outPath = "./assets/chatemojis/textures/emojis";

const exists = (path: string) => Deno.stat(path).then(() => true).catch(() => false);

if (await exists(shortcodesPath)) await Deno.remove(shortcodesPath);
if (await exists(outPath)) await Deno.remove(outPath, { recursive: true });
await Deno.mkdir(outPath, { recursive: true });

type DiscordShortcodes = {
  emojis: {
    names: string[];
    surrogates: string;
    hasDiversityParent?: boolean;
    hasMultiDiversityParent?: boolean;
  }[];
};

const discordShortcodes: DiscordShortcodes = JSON.parse(await Deno.readTextFile("../local/shortcodes.json"));
const shortcodes: Record<string, string> = {};
const singleCodepoints: number[] = [];
const multiCodepoints: Record<number, number[][]> = {};

for (const emoji of discordShortcodes.emojis) {
  // not doing combinators today chief
  if (emoji.hasDiversityParent || emoji.hasMultiDiversityParent) continue;

  let parts = twemoji.convert.toCodePoint(emoji.surrogates).split("-");
  const numberedParts = parts.map(p => parseInt(p, 16));
  if (parts.length === 0) continue;

  // https://github.com/jdecked/twemoji/blob/50c7abfe6813680455781862f7b34305cd1eb9f5/scripts/build.js#L344
  if (parts.includes("fe0f") && !parts.includes("200d")) {
    parts = parts.filter(part => part != "fe0f");
  }

  if (numberedParts.length === 1) {
    const codepoint = numberedParts[0];
    if (!singleCodepoints.includes(codepoint)) singleCodepoints.push(codepoint);
  } else {
    const first = numberedParts[0];
    if (!multiCodepoints[first]) multiCodepoints[first] = [];
    multiCodepoints[first].push(numberedParts.slice(1));
  }

  const filename = parts.join("-") + ".svg";
  for (const name of emoji.names) {
    shortcodes[name] = filename;
  }

  await Deno.copyFile(
    `${twemojiPath}/${filename}`,
    `${outPath}/${filename}`
  );
}

await Deno.writeTextFile(
  shortcodesPath,
  JSON.stringify(shortcodes) + "\n"
);

await Deno.writeTextFile(
  singleCodepointsPath,
  JSON.stringify(singleCodepoints) + "\n"
);

await Deno.writeTextFile(
  multiCodepointsPath,
  JSON.stringify(multiCodepoints) + "\n"
);
