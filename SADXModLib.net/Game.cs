using System.Runtime.InteropServices;

namespace SADXModLib.net;

public static unsafe class Game
{
    // variable: Rings, Sint16 at 0x3B0F0E4 (62976228)   SADXVariables.h:2140
    public static ref short Rings => ref *(short*)0x3B0F0E4;

    // function: PrintDebug at 0x401000 (4198400)        SADXFunctions.h:29
    // C# can't call "..." functions, so always call it as PrintDebug("%s", text)
    static readonly delegate* unmanaged[Cdecl]<byte*, byte*, void> printDebug =
        (delegate* unmanaged[Cdecl]<byte*, byte*, void>)0x401000;

    public static void PrintDebug(string text)
    {
        nint fmt  = Marshal.StringToHGlobalAnsi("%s");
        nint line = Marshal.StringToHGlobalAnsi(text);
        printDebug((byte*)fmt, (byte*)line);
        Marshal.FreeHGlobal(line);
        Marshal.FreeHGlobal(fmt);
    }
    
    // function: DisplayHintText at 0x4B79C0 (4946368)   SADXFunctions.h:767
    // lines: each one starts with \a like the game's own; \n = new line in the box
    // frames: 180 = 3 seconds (what the game itself uses)
    static readonly delegate* unmanaged[Cdecl]<nint*, int, void> displayHintText =
        (delegate* unmanaged[Cdecl]<nint*, int, void>)0x4B79C0;

    // The game reads the text while the box is up, so it is never freed.
    // Each different message is only made once.
    static readonly Dictionary<string, nint> hintLists = new();

    public static void DisplayHintText(string[] lines, int frames)
    {
        string key = string.Join("\n\n", lines);
        if (!hintLists.TryGetValue(key, out nint list))
        {
            list = Marshal.AllocHGlobal((lines.Length + 1) * sizeof(nint));
            for (int i = 0; i < lines.Length; i++)
                ((nint*)list)[i] = Marshal.StringToHGlobalAnsi(lines[i]);
            ((nint*)list)[lines.Length] = 0;        // NULL = end of the list
            hintLists[key] = list;
        }
        displayHintText((nint*)list, frames);
    }
}