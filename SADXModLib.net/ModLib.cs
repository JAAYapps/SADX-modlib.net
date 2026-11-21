using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace SADXModLib.net;

public class ModLib
{
    [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvCdecl) })]
    public static int Init(IntPtr path)
    {
        return 34;
    }

    [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvCdecl) })]
    public static void OnFrame()
    {
        
    }
}