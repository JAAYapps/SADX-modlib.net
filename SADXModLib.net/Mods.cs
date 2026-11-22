using System.Runtime.InteropServices;

namespace SADXModLib.net;

public static unsafe class Mods
{
    [DllImport("kernel32.dll", CharSet = CharSet.Ansi)]
    static extern nint GetModuleHandleA(string name);
    [DllImport("kernel32.dll", CharSet = CharSet.Ansi)]
    static extern nint GetProcAddress(nint module, string name);

    static readonly Dictionary<string, nint> found = new();

    // Calls "int __cdecl Name(void)" exported by another mod's DLL.
    // Returns 0 if that mod isn't loaded or doesn't have the export.
    public static int CallInt(string dll, string export)
    {
        string key = dll + "!" + export;
        if (!found.TryGetValue(key, out nint fn))
        {
            nint module = GetModuleHandleA(dll);
            fn = module == 0 ? 0 : GetProcAddress(module, export);
            if (fn == 0) return 0;                  // not remembered, so it's found if the mod shows up later
            found[key] = fn;
        }
        return ((delegate* unmanaged[Cdecl]<int>)fn)();
    }
}