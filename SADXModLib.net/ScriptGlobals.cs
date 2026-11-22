namespace SADXModLib.net;

public class ScriptGlobals
{
    public event Action? OnFrame;
    internal void RunFrame() => OnFrame?.Invoke();
}