using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Scripting;
using Microsoft.CodeAnalysis.Scripting;
using Microsoft.CodeAnalysis.Scripting.Hosting;

namespace SADXModLib.net;

public class ScriptHost
{
    static readonly List<(string name, ScriptGlobals globals)> loaded = new();

    static readonly InteractiveAssemblyLoader assemblyLoader = MakeLoader();
    
    static InteractiveAssemblyLoader MakeLoader()
    {
        var l = new InteractiveAssemblyLoader();
        l.RegisterDependency(typeof(Game).Assembly);
        return l;
    }
    
    public static void Load(string file)
    {
        string name = Path.GetFileName(Path.GetDirectoryName(file)) ?? file;
        try
        {
            var options = ScriptOptions.Default
                .WithReferences(typeof(Game).Assembly)
                .WithImports("System", "System.IO", "System.Linq", "SADXModLib.net");
            string code = File.ReadAllText(file);
            var script = CSharpScript.Create(code, options, typeof(ScriptGlobals), assemblyLoader);
            
            var problems = script.Compile();
            foreach (var p in problems)
                Game.PrintDebug($"[script] {name}: {p}\n");
            if (problems.Any(p => p.Severity == DiagnosticSeverity.Error))
                return;

            var report = ScriptReport.Find(script.GetCompilation());
            foreach (var f in report)
                Game.PrintDebug($"[script] {name}: {(f.Risky ? "RISKY " : "")}{f.What} (line {f.Line}: {f.Name})\n");

            var risky = report.Where(f => f.Risky).ToList();
            if (risky.Count > 0 && !Approval.Ask(name, code, risky))
            {
                Game.PrintDebug($"[script] {name} not run, the player said No\n");
                return;
            }
            
            var globals = new ScriptGlobals();
            script.RunAsync(globals).GetAwaiter().GetResult();
            loaded.Add((name, globals)!);
            Game.PrintDebug($"[script] {name} loaded\n");
        }
        catch (Exception e)
        {
            Game.PrintDebug($"[script] {name} failed: {e.Message}\n");
        }
    }

    public static void Frame()
    {
        foreach (var (name, globals) in loaded)
        {
            try { globals.RunFrame(); }
            catch (Exception e) { Game.PrintDebug($"[script] {name} OnFrame: {e.Message}\n"); }
        }
    }
}