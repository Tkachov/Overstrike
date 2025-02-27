# Scripts Proxy

This folder contains source code of 'scripts_proxy.dll' that Overstrike uses. It's a proxy of 'winmm.dll', which is loaded by the game's executable. When loaded, it reads 'scripts.txt' and expects to find DLLs with names given in that file under 'scripts/', and loads them too.

(I kinda disagree on the bit where DLLs are called scripts, but this is what community knows them as, thus I'm calling this "scripts proxy".)

**This code was written and provided by LDD565.**

# Dependencies

- [MinHook](https://github.com/TsudaKageyu/minhook);
- [Termcolor](https://github.com/ikalnytskyi/termcolor).

# How to build?

To avoid having dependencies in the repo, I'm using [vcpkg](https://vcpkg.io/en/). So, to build, you need to install it (and run `vcpkg integrate install`, so VS knows about it). Then, simply building the project in VS should do it.

I also had some warning about pwsh missing. If you want to get rid of it, [install PowerShell](https://learn.microsoft.com/en-us/powershell/scripting/install/installing-powershell-on-windows).
