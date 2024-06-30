Overstrike -- an open-source mod manager for PC ports of Insomniac Games' games.

README
----------------------------------------------------------------------
- 1. About
- 2. Installation
- 3. First launch
- 4. Migrating from SMPC Tool
- 5. Using Overstrike
	- 5.1. Mods tab
	- 5.2. Settings tab
	- 5.3. Suits Menu tab
- 6. Troubleshooting
- 7. Making mods
- 8. License


1. About
----------------------------------------------------------------------
Overstrike is a mod manager app. It allows you to install or reinstall
mods created by other people. It has a user-friendly and intuitive
interface, handles technical differences of mods formats installation
for you, and is open-source. The latter is important, because anyone
could make sure that this app doesn't do anything malicious (unlike
tools that don't have the source disclosed), and anyone can help
developing this program or new programs based on it.

Supported games:
- Marvel's Spider-Man Remastered
- Marvel's Spider-Man: Miles Morales
- Ratchet & Clank: Rift Apart
- i30 (Marvel's Spider-Man 2)
- i33 (Marvel's Wolverine)

Supported mod formats:
- .smpcmod/.mmpcmod
- .suit
- .stage
- .modular

Supported archives formats:
- .7z
- .zip
- .rar


2. Installation
----------------------------------------------------------------------
Since you're reading this, you probably have already downloaded
Overstrike and extracted the archive with it. If not, or if you want
to make sure you have the latest version, you can head to one of the
Nexus Mods pages or to Releases section of Github repository to get it.
It's the same app in all of those locations, and you don't need to
have multiple copies of it for different games (unless you want to).

	Github:
	https://github.com/Tkachov/Overstrike/

	Nexus:
	https://www.nexusmods.com/marvelsspidermanremastered/mods/4199/
	https://www.nexusmods.com/spidermanmilesmorales/mods/577/
	https://www.nexusmods.com/ratchetandclankriftapart/mods/1/

Once you download the archive, just extract it to a place you want.
I'd recommend to have it in a separate folder, just so its files don't
get mixed with game files or anything else.

Before you run 'Overstrike.exe', you'd need to install Microsoft's
.NET 7.0 Desktop Runtime. If it's not installed, Overstrike should
show a message suggesting to install it, but it's also possible it
simply won't launch without it at all.

	Here is a direct link:
	https://dotnet.microsoft.com/en-us/download/dotnet/thank-you/runtime-desktop-7.0.10-windows-x64-installer?cid=getdotnetcore

The games are built for 64-bit operating systems, and so is Overstrike.
Be sure to install x64 version of .NET runtime.

Overstrike is only built for Windows. However, it should be possible
to run it on Linux systems (for example, on Steam Deck) through Proton.

When you've installed .NET and extracted the files where you want,
simply run 'Overstrike.exe'.


3. First launch
----------------------------------------------------------------------
On first launch, Overstrike detects supported games in their usual
installation locations, and suggests to create a profile for each. You
can switch between these profiles on the fly, and create as many
profiles as you'd like.

In the opened window, all the detected games would be listed. If you
don't want to create a profile for some of these, you can untick the
checkbox next to them.

If Overstrike didn't find some of your games (for example, if you have
them installed in an unusual place), you can add these manually by
pressing "Add game" button.

That would open another window, where you should press "Browse..."
button and navigate to the folder where the game is installed. Press
"Select Folder" button and the path will be tested. If Overstrike
detects the game under that path, the message would say "Detected
game: " and the name of that game. Then, type a profile name in
a field above, and press "Create" button. As it creates a file with
that name, the same restrictions apply (for example, symbols : or *
are not allowed).

Add as many profiles as you'd like and press "Create profiles".

Overstrike would create 'Profiles/' folder and a file for each of your
profiles in there. If you want, you can rename, delete or duplicate
those files. The files are in JSON format, so you can also open them
with a text editor if you need to edit them, but it's unlikely you
would need to.

Overstrike would also create 'Mods Library/' folder. This is where you
can add all of your mods. This folder is automatically scanned on
every launch, and all new mods are added to the end of the list in
your profiles. Scan is recursive, meaning you can arrange subfolders
in 'Mods Library/' any way you like, and mods still will be found.
Scan also detects mods in the archives, so you can download mods
directly in there without extracting.

After profiles are created, the main window of Overstrike will be
opened with one of the profiles active.


4. Migrating from SMPC Tool
----------------------------------------------------------------------
If you were using SMPC Tool for Marvel's Spider-Man Remastered or
Marvel's Spider-Man: Miles Morales, you can bring your mods into
Overstrike.

For that, Overstrike needs to launched from SMPC Tool folder --
that is, 'Overstrike.exe' should be next to 'ModManager/' folder and
'assetArchiveDir.txt' file. When you press "Create profiles" on first
launch, it'll ask you whether you want to copy the mods from there.

After migrating, you can move Overstrike anywhere else. Just don't
forget to move all its files with it, including 'Profiles/' and
'Mod Library/' folders.


5. Using Overstrike
----------------------------------------------------------------------
The main window of Overstrike contains a profile selection dropdown
and two tabs: Mods and Settings. For games that support .suit mods,
there is also a Suits Menu tab.

To switch between profiles, simply choose one in the dropdown. If you
want to create a new one, choose "Add new profile...".


5.1. Mods tab
----------------------------------------------------------------------
In this tab, all of the mods that were found in your 'Mods Library/'
folder are listed. Only mods that are compatible with selected profile
are displayed (for example, if your profile is for Miles Morales game,
you won't see Ratchet & Clank mods in the list).

In the top right corner, there are two icons: "Add mods" (green plus
icon), which allows to copy files to 'Mods Library/' folder,
and "Refresh" (blue circle arrows), which allows to run a scan without
restart.

On the left side there's a Filter field, allowing you to narrow down
the list if you want to find a particular mod by name.

In the middle, the mods are listed. Each mod has a checkbox which
determines whether that mod is enabled or not. You can drag the mods
to change the order they install in. This list supports multiselect
with Ctrl and Shift. Changing checkbox for one of the selected mods
changes it for all other selected mods, allowing you to quickly enable
or disable multiple mods at once.

.modular mods can be customized. Right click such mod and press "Edit
modules..." to open a window that allows to choose from options mod's
author have provided.

You can delete mods using Delete key. This deletes them from your
'Mods Library/' folder. If mod you have selected is in archive, that
archive will be deleted. This means that if there were other mods in
that archive, they will be deleted too, even if you didn't select them.

In the bottom right corner is "Install mods" button. When you press it,
all the enabled mods will be installed to the game. The application
will be darkened while installation is going. When it's done, a "Mods
installed!" message will be shown in the left bottom corner.
If the process stops abruptly, message would say "Error occurred."
and a detailed message would be added to the end of 'errors.log' file.

If order was changed, mods were added, removed, enabled or disabled,
this button needs to be pressed again. If you have multiple profiles
for the same game, you'd also need to reinstall mods after switching
between such profiles.

Right clicking the button opens a small menu:

- Uninstall all mods
  Uninstalls the mods without disabling them in the list.

- Launch the game
  Launches the game.


5.2. Settings tab
----------------------------------------------------------------------
If the game supports .suit mods, this is where you can choose the
language that needs to be modded with suit names. You can choose not
to install suit names too. That setting is separate for every profile.

Other settings are global for this installation of Overstrike.
Currently, there are just two:

- Cache Mods Library contents
  
  Controls whether library scan should be using a cache file.
  This makes scans faster, because old mods would be quickly verified
  and only new mods would need to be processed.

- Skip Mods Library sync if cache present
  
  Allows to disable the automatic scan on launch, and list the mods
  from cache. If cache doesn't exist, scan will have to happen. Since
  cache is only updated after scan, this means you'd be able to remove
  or add mods without Overstrike knowing about it. You would need
  to press "Refresh" button so it notices the changes in
  'Mods Library/' folder. This option could be useful if you have
  a lot of mods and rarely change them -- this way, Overstrike won't
  spend time scanning the library unless you manually ask it to.


5.3. Suits Menu tab
----------------------------------------------------------------------
For Marvel's Spider-Man Remastered and Marvel's Spider-Man: Miles
Morales games, Overstrike allows you to organize the suits displayed
in game's menu the way you'd like.

On the left side it displays suits icons in a grid, same way as they'd
appear in game. You can drag suit to reorder it.

Controls on the right side allow to modify currently selected suit.
You can choose another model or icon to be used in there. And "Delete"
button allows to remove it from the list.

Deleted suits are displayed with icon faded. You can select it
and press "Restore" to return it to the list. If you want to preview
how the list would look like in game, without the deleted items, you
can untick "Show deleted" checkbox on the bottom.

Below that checkbox are current status label and the buttons.
If you didn't change anything, there would only be a "Reset" button.
It allows you to undo all of your saved changes, so the list appears
the same way it would if there was no Suits Menu. You can press "Undo
reset" to return to your saved changes.

If you do change something, there would be "Undo" and "Save" buttons
instead. First one allows you to undo all the unsaved changes.
Second saves your changes.

The changes are stored in the profile. You can have different profiles
with differently configured Suits Menu. And, these changes are not
reset when you reinstall mods, so you don't have to reorganize
the suits after every install.

When you add new .suit mods, you need to install them with "Install
mods" so they appear in Suits Menu.

Pressing "Save" only saves your changes to profile. You need to press
"Install mods" again so updated "Suits Menu" mod is applied
to the game. This mod is displayed the very last in the mods list
and cannot be moved or deleted. You can disable it if you don't want
your Suits Menu changes to be applied.


6. Troubleshooting
----------------------------------------------------------------------
If Overstrike fails to install a mod, it leaves the game unmodded
and writes a detailed report in 'errors.log' file (creating it if it
doesn't exist next to 'Overstrike.exe'). Latest report is added to the
end of the file. It could help you figure out the problem.

If you don't know how to solve it, you could try asking about it
in the comments section of Nexus Mods page, or other places, like
Reddit communities or Discord servers. Be polite and patient, as
people there are not obliged to help you. If you attach screenshots
and describe details of your problem, it'd probably be easier to help
you. Before you ask, do try searching if this problem was already
discussed, and experiment to see if you can do something to fix it
on your own.

The most frequent problem users encounter is game crashing on launch.
That usually happens because of one of two reasons:

- Corrupted 'toc' file

  In the game folder there is a small file called 'toc'. It controls
  where the game would look for its assets (models, textures, sounds).
  If it gets corrupted, the game won't know where to look, or will
  try to look in a wrong place, and that'd lead to crash.

  Overstrike creates a copy of that file called 'toc.BAK'. This copy
  is used to restore the original (unmodded) state of the game, so
  new mods could be applied on top of that clean state.

  Due to various reasons 'toc' and/or 'toc.BAK' could be unexpectingly
  modified. Since this leads to crash, we say it's corrupted.

  The fix for that is known as "toc reset". What you should do is:
  1) delete both 'toc' and 'toc.BAK' from game folder;
  2) verify game files through Steam or EGS, so it downloads you
     a clean 'toc'.

  After you do this, the game should be in unmodded state, and launch
  without any problems. If it still crashes, either you missed
  something, or crash happens because of something else.

  If the unmodded game works, you can open Overstrike and press
  "Install mods". That should be it. If the game starts crashing again,
  this could be due to one of the mods you try to install. It could
  also be a bug in Overstrike, even though that's very unlikely.

  Some of the reasons that could lead to 'toc' corrupting:
  - the game received an update, so 'toc.BAK' became outdated;
  - mods installation process was interrupted;
  - other tools modified the files in an unexpected way;
  - files were removed or weren't fully downloaded.

  Depending on the game, 'toc' is either in 'asset_archive/' subfolder
  of game's folder, or in game's folder itself.

- Corrupted save files / Reaching .suits limit

  The games also crash if their save files are incorrect. These files
  are usually stored in your 'Documents/' folder, in a subfolder named
  according to the game name. Files themselves are typically named
  'slotN-s.save' or 'slotN-s-manual-M.save' (where N and M are numbers).

  While there could probably be other reasons leading to saves
  corruption, there is one that certainly does: installing too many
  .suit mods. We call this "suits limit".

  There is nothing that could be done about this limit currently.
  Limit is about 70-80 suits, and it includes the .suit mods you have
  uninstalled. The game "remembers" that you had a suit in the save
  file, and something in it breaks when you reach the limit. You still
  can play the game when you use .suit mods you've installed earlier,
  but installing any "new" suits on top of that would lead to crash.

  You can reset the limit by using clean saves. Find some on Nexus,
  or backup your saves while the game still works fine, delete
  the ones you have in 'Documents/' and put these clean ones instead.

  If the game still crashes after you put fresh saves, it could be
  some other problem. But, it could also be because you have cloud
  saves sync enabled in Steam or EGS. If instead of those fresh saves
  you see your old saves appearing in 'Documents/' after you launch
  the game, simply disable cloud sync option, put fresh saves and try
  again.

If none of the above helps, you've tried everything and something
still doesn't work, or clearly doesn't work as described, you can
report a bug. Use one of the Nexus Mods pages or Issues section
of the Github repository for that. Please provide as many details
as you can, because that could greatly help in finding out the cause
of the problem.

It could take some time to figure out why the bug happens and how to
fix it. If you're experienced modder or programmer, you can try to do
this yourself. Fixes or new features can be submitted on Github as
Pull Requests.


7. Making mods
----------------------------------------------------------------------
Overstrike is designed to be an end-user app, and doesn't provide any
mod making capabilities.

If you want to make a mod, you should search for a tutorial, ask
someone in the community for directions or figure it out yourself.

To explore games' asset archives, you can use Modding Tool. It shares
part of the codebase with Overstrike, and its code is also available
in the same Github repository.

	Modding Tool Nexus page:
	https://www.nexusmods.com/marvelsspidermanremastered/mods/4395/


8. License
----------------------------------------------------------------------
This program is free software, and can be redistributed
and/or modified by you. It is provided 'as-is', without any warranty.

For more details, terms and conditions, see GNU General Public License.
A copy of the that license should come with this program (LICENSE.txt).

	If not, see:
	http://www.gnu.org/licenses/
