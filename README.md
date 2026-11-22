# Sonic Adventure DX Mod Library .NET

## Ever wanted to make a SADX mod in .NET? Well, now you CAN!!!!

# Requirements

* The 32-bit (x86) .NET 10 Runtime: https://dotnet.microsoft.com/download/dotnet/10.0
* SADX (I would think this would be self-explanatory, but I wanted to say it anyway)
* SA Mod Manager (it comes with the mod loader to load the mod)

Note: on Linux with Lutris or Wine, if `DOTNET_ROOT` is set, the mod finds the 64-bit .NET instead and fails with "No .NET 10 x86 runtime installed". Fix it by adding the environment variable `DOTNET_ROOT_X86` = `C:\Program Files (x86)\dotnet` (in Lutris: Configure > System options > Environment variables).

Side note: Microsoft's docs don't explain how to load .NET DLLs from a native program very well, so the hosting setup was worked out with help from AI (Claude, by Anthropic).

# Script mods

A script mod is a plain C# script (`mod.csx`) instead of a DLL, so anyone can open it and read exactly what it does.

## Making one

Make a folder in `mods` with two files:

```
mods\Ring Test\
    mod.ini
    mod.csx
```

`mod.ini` (no `DLLFile` line):

```
Name=Ring Test
Author=Your Name
Version=0.1
Description=Keeps your rings at 10 or more
```

`mod.csx`:

```csharp
Game.PrintDebug("Ring Test loaded\n");

OnFrame += () =>
{
    if (Game.Rings < 10)
        Game.Rings = 10;
};
```

Enable both SADXModLib.net and your script mod in SA Mod Manager. SADXModLib.net looks for `mod.csx` in every enabled mod's folder, after all the mods have loaded, and runs each one it finds.

## What a script can use

| Name | What it is |
|---|---|
| `OnFrame += () => { };` | runs your code once every frame |
| `Game.Rings` | the ring count; read it or set it |
| `Game.PrintDebug("text\n")` | writes a line to the mod loader log |

`System`, `System.IO`, `System.Linq` and `SADXModLib.net` are already imported, so a script doesn't need `using` lines for them.

## The script check

Before a script runs, SADXModLib.net lists everything the script uses and writes it to the mod loader log (`mods\.modloader\SADXModLoader.log`):

```
[script] Ring Test: game memory (expected) (line 1: SADXModLib.net.Game.PrintDebug)
[script] Ring Test: game events (line 3: SADXModLib.net.ScriptGlobals.OnFrame)
```

If the script uses something risky, the game asks first:

```
+--------------------------------------------------------------+
| SADXModLib.net script check                                  |
|                                                              |
| Ring Test wants to:                                          |
|                                                              |
| ⚠ files: write/delete (line 11: System.IO.File.WriteAllText) |
|                                                              |
| Run it? Yes is remembered until the script changes.          |
|                                          [ Yes ]  [ No ]     |
+--------------------------------------------------------------+
```

* **Yes** runs it, and saves the script's fingerprint to `mods\SADXModLib.net\approved.txt`. You won't be asked again until the script's text changes, even by one letter.
* **No** skips it. You'll be asked again on the next launch.
* To forget every Yes, delete `approved.txt`.

What counts as risky:

| Shown as | Script uses |
|---|---|
| files: write/delete | anything in `System.IO` except reading (reading files, `Path`, and listing folders are fine) |
| run programs | `System.Diagnostics.Process` |
| network | `System.Net` |
| Windows registry | `Microsoft.Win32` |
| close the game | `Environment.Exit`, `Environment.FailFast` |
| load hidden code | `System.Reflection`, `System.Runtime.Loader`, `Activator`, `AppDomain`, `Type.Get...`, `Type.InvokeMember`, `dynamic`, `#r` |
| raw memory / native calls | `System.Runtime.InteropServices`, `System.Runtime.CompilerServices.Unsafe` |

## This is a check, not a sandbox

.NET has no way to lock code inside a game, so a script can do anything a DLL can. The check catches the obvious cases and tells you where to look, but someone determined can still hide something. Only run scripts from people you trust, and read any script that triggers the box before saying Yes.

Things the check can't see:

* Code inside a DLL that a script loads with `#r`. That's why `#r` always counts as risky.
* Changes to a file pulled in with `#load`. Only `mod.csx` is fingerprinted, so if just the other file changes, you won't be asked again.

## If a script doesn't run

Look in the mod loader log for its `[script]` lines:

| Log line | Meaning |
|---|---|
| `[script] Ring Test loaded` | it's running |
| `[script] Ring Test: (9,9): error CS0103: ...` | the script has a mistake; (9,9) is line 9, column 9 |
| `[script] Ring Test not run, the player said No` | you said No in the box |
| `[script] Ring Test failed: ...` | it crashed while starting; the rest of the line says why |
| `[script] Ring Test OnFrame: ...` | its `OnFrame` code crashed; it keeps being called every frame |
| no `[script]` lines at all | the mod isn't enabled, or the file isn't named `mod.csx` |

# Credits

* SADX Mod Loader headers (`programming` folder) by X-Hax: https://github.com/X-Hax/sadx-mod-loader (no license; X-Hax's disclaimer: informational and educational use only, no commercial use)
* nethost and hostfxr from the .NET runtime (MIT License): https://github.com/dotnet/runtime
* Roslyn C# scripting (`Microsoft.CodeAnalysis.CSharp.Scripting`, MIT License): https://github.com/dotnet/roslyn
* `AudioFile.h` by Adam Stark (MIT License), included in the X-Hax headers
