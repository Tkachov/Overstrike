Modding Tool allows you to extract and replace assets. Packs mods to .stage or .modular.

# About

Supported games:
- Marvel's Spider-Man Remastered
- Marvel's Spider-Man: Miles Morales
- Ratchet & Clank: Rift Apart
- Marvel's Spider-Man 2

Nexus Mods pages:
https://www.nexusmods.com/marvelsspidermanremastered/mods/4395
https://www.nexusmods.com/spidermanmilesmorales/mods/613
https://www.nexusmods.com/ratchetandclankriftapart/mods/27
https://www.nexusmods.com/marvelsspiderman2/mods/32

Shares part of the codebase with Overstrike.
Source code is available in its Github repository:
https://github.com/Tkachov/Overstrike

# Installation

Like Overstrike, it is made for 64-bit version of Windows.
On Linux, you can try using Proton to run it, but it's not guaranteed to work.

To run, it needs x64 version of .NET 7.0 Desktop Runtime installed:
https://dotnet.microsoft.com/en-us/download/dotnet/thank-you/runtime-desktop-7.0.10-windows-x64-installer?cid=getdotnetcore

Before you run it, you might want to rename one of the hashes file to 'hashes.txt'.
The one named so by default is for MSMR, MM, RCRA and MSM2.
There are also 'hashes_i30.txt' and 'hashes_i33.txt' for i30 and i33.
If you want the tool to know file names from one of these, rename one of those
files into 'hashes.txt' and then run the 'ModdingTool.exe'.
If you don't have those, get a 'hashes.zip' from Releases.

# Usage

Use File menu to load 'toc' file.

On the left, folder tree would be displayed. Clicking one of the folders there
shows the assets from that folder on the right. Assets that don't have a name
in 'hashes.txt' are grouped by archive file in the [UNKNOWN] section. Audio
assets (.wem) are grouped by archive file in the [WEM] section.

Right click folders in the free or assets in the table to open context menu.
It allows you to extract the assets, copy their full file names or short
unique references, or replace an asset with a file from your disk.

## Search

You can search for assets through Search window.
If you type multiple words (separated with a space), only assets that match
all of the words would be displayed. Use the same context menu as in the main
window, or double click an asset to view the folder it's in.

Use Search > Jump to path or ref... to quickly open a folder where specified
asset is in.

## Replace

When you replace an asset with file, it's not done instantly. You can see the
list of assets replaced (and disk files used for them) if you use Mods > Pack
replaced as .stage...

There, you can choose which game your mod is for, specify your name and name
of your mod. You can also remove replacements by pressing Delete key after
selecting them. Press "Pack as .stage" to save the mod as .stage file.

## Stages

In addition to extracting and replacing assets one by one, there are also
options to extract to stage and replace from stage. Here, "stage" means
a subfolder of 'stages/' folder next to 'ModdingTool.exe'. Assets in stage
follow the original folders structure, and are split by spans.

For example, if your stage is called "test", asset 'characters\hero\
hero_spiderman\hero_spiderman_body.model' from span 0 would be extracted to
'stages/test/0/characters/hero/hero_spiderman/hero_spiderman_body.model'.

You can extract all the assets you need into a stage, modify the files there,
and then easily replace all the assets at once with files from stage instead
of replacing them one by one.
