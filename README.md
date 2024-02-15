![Overstrike logo](https://github.com/Tkachov/Overstrike/assets/1948111/7d2510a0-7dda-47ad-9b05-ac34dce3186e)

Overstrike is a mod manager for PC ports of Insomniac Games' games.

It supports .smpcmod/.mmpcmod, .suit and .stage mod formats, allows to create multiple profiles per game, automatically detects new mods in its folder (even in archives), and more. It's standalone, unified and user-friendly.

![Overstrike main window screenshot](https://github.com/Tkachov/Overstrike/assets/1948111/92229d23-550c-4d76-b2f3-d669c6b76764)

This repository also contains the source code of Modding Tool and Localization Tool. These tools share part of the codebase with Overstrike, and could be useful to mod authors.

# Usage

For detailed instructions, refer to description on Nexus Mods or README.txt file that comes with Overstrike.

There is an Overstrike page in every supported game section on Nexus, but it's the same app, so you only need to download it once. You can also get it from the [Releases](https://github.com/Tkachov/Overstrike/releases).

In order to run it, you'd need [.NET 7.0 Desktop Runtime](https://dotnet.microsoft.com/en-us/download/dotnet/thank-you/runtime-desktop-7.0.10-windows-x64-installer?cid=getdotnetcore) installed.

On first launch, it detects supported games in their usual installation locations, and suggests to create a profile for each. You can switch between these profiles on the fly, and create as many profiles as you'd like.

On every launch, it scans the **Mods Library/** folder to find all the supported mods. It can also detect and work with mods within .7z, .zip or .rar archives.

Mods can be reordered using drag'n'drop, and disabled by unticking a checkbox next to them. To apply currently enabled mods to the game, you need to press "Install mods" button. If order was changed, mods were added, removed, enabled or disabled, this button needs to be pressed again.

After installation process, it shows a "Mods installed!" message in the left bottom corner, meaning you can launch the game and play. However, if the process stops abruptly, message would say "Error occurred." and a detailed message would be added to the end of 'errors.log' file.

# Contributing

If you want to contribute, you're very welcome!

No contribution is too small. If you've found a bug, or have a suggestion, or want to write a guide for other users — feel free to help the way you can.

Of course, I'd be glad to accept code changes. If you can fix a bug, or implement a feature you've always wanted, or write a tool based on the code here, don't hesitate to send a PR or make a fork.

You can start by looking at the [Issues](https://github.com/Tkachov/Overstrike/issues) page, where you can create a new one if it's a bug or suggestion, or find one you'd like to help with.

# License

Overstrike uses GPLv3 license.

TL;DR: you can do anything you want, but you must disclose your modified source code under the same license. If you don't, you're violating the license. I'm not going to take any legal action against you, but everyone will know that you're an asshole.

Modding is supposed to be about sharing, not gatekeeping, and this license is aimed to have it that way.
