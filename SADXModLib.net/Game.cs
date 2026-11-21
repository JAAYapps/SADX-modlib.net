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
}