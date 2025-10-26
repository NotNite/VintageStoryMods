# VintageStoryMods

My Vintage Story mods.

## Setup

To build this solution, create a `Directory.Build.props.user` in this folder with your Vintage Story game install in it:

```xml
<Project>
    <PropertyGroup>
        <VINTAGE_STORY>/home/julian/.local/share/flatpak/app/at.vintagestory.VintageStory/current/active/files/extra/vintagestory</VINTAGE_STORY>
    </PropertyGroup>
</Project>
```
