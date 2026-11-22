using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace SADXModLib.net;

public class ModLib
{
    [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvCdecl) })]
    public static void Init(IntPtr path)
    {
        Approval.ListFile = Path.Combine(Marshal.PtrToStringAnsi(path)!, "approved.txt");
        Game.PrintDebug("Dotnet Loaded\n");
    }

    [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvCdecl) })]
    public static void LoadScript(IntPtr folder)
    {
        string file = Path.Combine(Marshal.PtrToStringAnsi(folder)!, "mod.csx");
        if (File.Exists(file))
            ScriptHost.Load(file);
    }
    
    [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvCdecl) })]
    public static void OnFrame()
    {
        ScriptHost.Frame();
    }
    
    [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvCdecl) })]
    public static void OnInitEnd()
    {
        
    }
    
    [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvCdecl) })]
    public static void OnInput()
    {
        // Executed before the game processes input
    }

    [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvCdecl) })]
    public static void OnControl()
    {
        // Executed when the game processes input
    }

    [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvCdecl) })]
    public static void OnRenderDeviceReset()
    {
        // Executed when the window size changes
    }

    [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvCdecl) })]
    public static void OnRenderDeviceLost()
    {
        // Executed when the game fails to render the scene
    }

    [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvCdecl) })]
    public static void OnRenderSceneStart()
    {
        // Executed before the game starts rendering the scene
    }

    [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvCdecl) })]
    public static void OnRenderSceneEnd()
    {
        // Executed when the game finishes rendering the scene
    }
	    
    [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvCdecl) })]
    public static void OnExit()
    {
        // Executed when the game is about to terminate
    }
}