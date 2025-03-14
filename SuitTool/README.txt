Suit Tool allows you to create .suit and .suit_style for MSM2.

# About

Supported games:
- Marvel's Spider-Man 2

Nexus Mods page:
https://www.nexusmods.com/marvelsspiderman2/mods/772

Shares part of the codebase with Overstrike.
Source code is available in its Github repository:
https://github.com/Tkachov/Overstrike

# Installation

Like Overstrike, it is made for 64-bit version of Windows.
On Linux, you can try using Proton to run it, but it's not guaranteed to work.

To run, it needs x64 version of .NET 7.0 Desktop Runtime installed:
https://dotnet.microsoft.com/en-us/download/dotnet/thank-you/runtime-desktop-7.0.10-windows-x64-installer?cid=getdotnetcore

You can put tool files anywhere you'd like.

# Usage

Tool allows you to create and open .suit_project files.

This file should be placed in a folder that has subfolders named like '0/' or 
'1/', similarly to Modding Tool stage folders. Here, 0 and 1 indicate which 
span this file will correspond to, and rest of the path would be used for its 
asset path. For example, if you have a texture and its HD counterpart called 
'my.texture', you can arrange them like this:

	MyProject/
		0/
			my.texture
		1/
			my.texture
		my.suit_project

Resulting .suit would create 'my.texture' asset in game and add to spans #0 
and #1 contents of those files.

The tool will detect all files in these subfolders, and you'll be able to use 
them when you select which .model your suit should use, which .texture is its 
icon, etc. These files will then be packed into .suit (and .suit_style).

You're free to arrange your files the way you want, but you need to be aware 
that you mod will likely be installed with others, so it's best for them to 
have unique names. For example, you can use the following structure:

	MyProject/
		0/
			suits/
				<MySuitID>/
					icons/
					materials/
					models/
					textures/
		1/
			suits/
				<MySuitID>/
					textures/
		my.suit_project

Obviously, you'd need to edit your .model files to look for .material assets 
by path that corresponds to your folder structure, same with .materials and 
.textures they're using.

If you change the files while the tool is open, you can use "Assets > Refresh" 
menu option to make it scan the folders again. In that menu, you can also find 
"Show paths..." option, which opens a window with a list of all detected files.
That could be useful when you need to copy one of those paths somewhere.

In the main window, there are
- mod information section;
- Main suit section;
- Styles section.

In the information section, you need to specify mod name and author that'd be 
displayed in Overstrike. Then, there's a field for text that will be displayed 
in game's suit menu. Then, field for your suit ID (should be unique). Lastly, 
you can choose there whether it's suit for Peter or Miles.

In the Main suit section, you just need to choose the main model and icon 
texture. Optionally, you can choose the mask model there. Finally, there are 
additional settings, such as iron legs, black webs and tentacle traversal.

In the Styles section, there's a "+" button that adds a new style to the list.
Game cannot show more than 3 styles at a time, but you can make as much styles 
as you want. Each style is then packed into separate .suit_style file, which 
can be installed by user in any order.

Style essentially determines which materials of your .models to override.
Because of that, there'd be a list of all material slots used in your .models,
and dropdowns that let you can specify which .material this style should use 
as override. You can leave such dropdown empty, in case this style should use 
the same material as the main model.

You need to also choose an icon that'll be shown in game for your style, and 
provide a name that will be displayed in Overstrike. Each style should also 
have a unique ID.

When you add the style, its ID by default is "var{N}", where N is the number 
of your style. Tool will try to deduce appropriate icon .texture and material 
overrides automatically. You can press "Auto-Refill" button to make it deduce 
again, in case you didn't have some of the files when you added the style.

For example, if main suit uses "icons/icon.texture" and "mats/eyes.material", 
"icons/icon_var1.texture" and "mats/var1/eyes.material" should automatically 
be selected when style is added (if such files exist in project's folders).

After you've specified all the files for main suit and its styles, you just 
need to press "Pack" button in the mod information section. That'd open a new 
window with the pack log. In case something goes wrong, this should help you 
figure out on which step it does. The log also shows which asset was packed to 
which mod file, and why was it done so. If multiple styles use the same asset, 
it's packed into the main .suit. All unused assets are also packed into the 
main .suit.

If there was no error, tool would create 'out/' subfolder in the project's 
folder, and put .suit and .suit_style files there. Additionally, you'll find 
'configs/' subfolder there. It contains the .config files that Suit Tool 
generates for you. These are already packed into .suit, and are only there for 
reference. If you want, you can override them by placing your own version of 
these .configs to the '0/suits/<MySuitID>/configs/' folder.
