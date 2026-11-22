using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;

namespace SADXModLib.net;

public static class Approval
{
    public static string ListFile = "";   // set in ModLib.Init

    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    static extern int MessageBoxW(IntPtr owner, string text, string caption, uint type); // This is to get a message box just like on the c++ side in the native file code.
    // Tells Windows to give a question message box with a warning icon.
    const uint MB_YESNO = 4, MB_ICONWARNING = 48;
    const int ID_YES = 6;

    public static bool Ask(string name, string code, List<Finding> risky)
    {
        string key = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(code))) + " " + name;
        if (File.Exists(ListFile) && File.ReadAllLines(ListFile).Contains(key))
        {
            Game.PrintDebug($"[script] {name}: approved before, not asking\n");
            return true;
        }

        string text = name + " wants to:\n\n" + string.Join("\n", risky.Select(f => $"⚠ {f.What} (line {f.Line}: {f.Name})")) + "\n\nRun it? Yes is remembered until the script changes.";

        bool yes = MessageBoxW(IntPtr.Zero, text, "SADXModLib.net script check", MB_YESNO | MB_ICONWARNING) == ID_YES;
        if (yes)
        {
            File.AppendAllLines(ListFile, new[] { key });
            Game.PrintDebug($"[script] {name}: player said Yes, saved to {Path.GetFullPath(ListFile)}\n");
        }
        return yes;
    }
}