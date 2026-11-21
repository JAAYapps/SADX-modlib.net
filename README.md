# Sonic Adventure DX Mod Library .NET

## Ever wanted to make a SADX mod in .NET? Well, now you CAN!!!!

# Requirements

* The 32-bit (x86) .NET 10 Runtime: https://dotnet.microsoft.com/download/dotnet/10.0
* SADX (I would think this would be self-explanatory, but I wanted to say it anyway)
* SA Mod Manager (it comes with the mod loader to load the mod)

Note: on Linux with Lutris or Wine, if `DOTNET_ROOT` is set, the mod finds the 64-bit .NET instead and fails with "No .NET 10 x86 runtime installed". Fix it by adding the environment variable `DOTNET_ROOT_X86` = `C:\Program Files (x86)\dotnet` (in Lutris: Configure > System options > Environment variables).

Side note: Microsoft's docs don't explain how to load .NET DLLs from a native program very well, so the hosting setup was worked out with help from AI (Claude, by Anthropic).

# Credits

* SADX Mod Loader headers (`programming` folder) by X-Hax: https://github.com/X-Hax/sadx-mod-loader (no license; X-Hax's disclaimer: informational and educational use only, no commercial use)
* nethost and hostfxr from the .NET runtime (MIT License): https://github.com/dotnet/runtime
* `AudioFile.h` by Adam Stark (MIT License), included in the X-Hax headers
