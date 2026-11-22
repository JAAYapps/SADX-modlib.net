using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace SADXModLib.net;

public record Finding(bool Risky, string What, int Line, string Name);

public static class ScriptReport
{
    // Checked top to bottom, first match wins:
    // the narrow "read" rules sit above the broad "System.IO." rule.
    static readonly (string start, bool risky, string what)[] rules =
    {
        ("SADXModLib.net.ScriptGlobals.",              false, "game events"),
        ("SADXModLib.net.Game.",                       false, "game memory (expected)"),
        ("System.IO.Path.",                            false, "file names only"),
        ("System.IO.File.ReadAll",                     false, "files: read"),
        ("System.IO.File.ReadLines",                   false, "files: read"),
        ("System.IO.File.Exists",                      false, "files: read"),
        ("System.IO.File.OpenRead",                    false, "files: read"),
        ("System.IO.Directory.Exists",                 false, "files: read"),
        ("System.IO.Directory.GetFiles",               false, "files: read"),
        ("System.IO.Directory.GetDirectories",         false, "files: read"),
        ("System.IO.Directory.EnumerateFiles",         false, "files: read"),
        ("System.IO.Directory.EnumerateDirectories",   false, "files: read"),
        ("System.IO.",                                 true,  "files: write/delete"),
        ("System.Diagnostics.Process",                 true,  "run programs"),
        ("System.Net.",                                true,  "network"),
        ("Microsoft.Win32.",                           true,  "Windows registry"),
        ("System.Environment.Exit",                    true,  "close the game"),
        ("System.Environment.FailFast",                true,  "close the game"),
        ("System.Reflection.",                         true,  "load hidden code"),
        ("System.Runtime.Loader.",                     true,  "load hidden code"),
        ("System.Activator.",                          true,  "load hidden code"),
        ("System.AppDomain.",                          true,  "load hidden code"),
        ("System.Type.Get",                            true,  "load hidden code"),
        ("System.Type.InvokeMember",                   true,  "load hidden code"),
        ("System.Runtime.InteropServices.",            true,  "raw memory / native calls"),
        ("System.Runtime.CompilerServices.Unsafe",     true,  "raw memory / native calls"),
    };

    public static List<Finding> Find(Compilation compilation)
    {
        var found = new List<Finding>();
        foreach (var tree in compilation.SyntaxTrees)        // includes files pulled in with #load
        {
            var model = compilation.GetSemanticModel(tree);
            var root = (CompilationUnitSyntax)tree.GetRoot();

            foreach (var r in root.GetReferenceDirectives())  // #r "something.dll"
                Add(found, true, "load hidden code", r, "#r " + r.File.ValueText);

            foreach (var name in root.DescendantNodes().OfType<SimpleNameSyntax>())
            {
                if (model.GetTypeInfo(name).Type is IDynamicTypeSymbol)
                {
                    Add(found, true, "load hidden code", name, "dynamic");
                    continue;
                }
                
                if (name.Parent is MemberAccessExpressionSyntax m && m.Expression == name)
                    continue;
                
                var symbol = model.GetSymbolInfo(name).Symbol;
                string? full = symbol switch
                {
                    null or INamespaceSymbol             => null,          // "System", "IO" on their own do nothing
                    INamedTypeSymbol t                   => t.ToDisplayString() + ".",
                    _ when symbol.ContainingType != null => symbol.ContainingType.ToDisplayString() + "." + symbol.Name,
                    _                                    => null,          // locals like "frames"
                };
                if (full == null) continue;
                

                foreach (var (start, risky, what) in rules)
                {
                    if (full.StartsWith(start, StringComparison.Ordinal))
                    {
                        Add(found, risky, what, name, full);
                        break;
                    }
                }
            }
        }
        return found;
    }

    static void Add(List<Finding> found, bool risky, string what, SyntaxNode node, string name)
    {
        if (found.Any(f => f.What == what)) return;           // first use of each kind is enough
        int line = node.GetLocation().GetLineSpan().StartLinePosition.Line + 1;
        found.Add(new Finding(risky, what, line, name));
    }
}