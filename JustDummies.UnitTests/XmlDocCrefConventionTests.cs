#region Usings declarations

using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;

using NFluent;

#endregion

namespace JustDummies.UnitTests;

/// <summary>
///     Locks how a predefined type is spelled inside an XML documentation <c>cref</c>: with its C# keyword
///     (<c>string</c>, <c>int</c>), never with its CLR name (<c>String</c>, <c>Int32</c>). Both bind to the same
///     documentation ID, so the compiler stays silent and the generated XML is identical either way — which is
///     precisely why the deviation drifts in unnoticed. It costs the source, not the package: the rest of the
///     surface documents itself in keywords, and inside <see cref="Any" /> a generic argument written
///     <c>Action{String}</c> reads as the <see cref="Any.String" /> factory rather than as the CLR string.
/// </summary>
/// <remarks>
///     This is a text scan, not reflection: by the time a cref reaches the generated XML the compiler has
///     resolved it to <c>System.String</c> and the spelling under test no longer exists. Reading the sources of
///     the sibling projects creates no assembly reference, so the standalone boundary <c>ArchitectureTests</c>
///     guards is untouched.
/// </remarks>
public sealed class XmlDocCrefConventionTests {

    // The predefined types, which is to say exactly those whose CLR name has a C# keyword. Int128, UInt128 and
    // Half have none, so their PascalCase spelling is the only one and they are legitimately absent here.
    private static readonly IReadOnlyDictionary<string, string> KeywordFor = new Dictionary<string, string>(StringComparer.Ordinal) {
        ["Boolean"] = "bool",
        ["Byte"]    = "byte",
        ["SByte"]   = "sbyte",
        ["Char"]    = "char",
        ["Decimal"] = "decimal",
        ["Double"]  = "double",
        ["Single"]  = "float",
        ["Int16"]   = "short",
        ["UInt16"]  = "ushort",
        ["Int32"]   = "int",
        ["UInt32"]  = "uint",
        ["Int64"]   = "long",
        ["UInt64"]  = "ulong",
        ["Object"]  = "object",
        ["String"]  = "string",
        ["Void"]    = "void"
    };

    private static readonly Regex CrefAttribute = new("cref=\"([^\"]*)\"", RegexOptions.Compiled);
    private static readonly Regex Identifier    = new("[A-Za-z_][A-Za-z0-9_]*", RegexOptions.Compiled);

    [Fact(DisplayName = "Every cref spells a predefined type with its C# keyword, not its CLR name.")]
    public void CrefsSpellPredefinedTypesWithTheirCSharpKeyword() {
        List<string> files = SourceFiles().ToList();

        // Guards the scan itself: a moved root or a renamed project would leave the enumeration empty, and every
        // assertion below would then pass vacuously. The thresholds are floors far under the real counts (126
        // files, ~1200 crefs), so ordinary growth or pruning never trips them.
        Check.WithCustomMessage($"Only {files.Count} source file(s) found under the JustDummies projects; the scan lost its target.")
             .That(files.Count).IsStrictlyGreaterThan(100);

        List<string> offenders = new();
        int          scanned   = 0;

        foreach (string file in files) {
            foreach (Match cref in CrefAttribute.Matches(File.ReadAllText(file))) {
                string reference = cref.Groups[1].Value;
                scanned++;

                foreach (string clrName in ClrNamesIn(reference)) {
                    offenders.Add($"{Path.GetFileName(file)}: cref \"{reference}\" names {clrName}; write {KeywordFor[clrName]}.");
                }
            }
        }

        // The same guard one level down: files found but no cref extracted would prove just as little.
        Check.WithCustomMessage($"Only {scanned} cref(s) scanned across {files.Count} file(s); the extraction lost its target.")
             .That(scanned).IsStrictlyGreaterThan(900);

        Check.WithCustomMessage($"{offenders.Count} cref(s) name a CLR type where C# has a keyword:{Environment.NewLine}{string.Join(Environment.NewLine, offenders)}")
             .That(offenders).IsEmpty();
    }

    private static IEnumerable<string> ClrNamesIn(string cref) {
        // A parameter list and a generic-argument group are pure type positions: every identifier inside one
        // names a type, so a CLR name found there is a violation whatever qualifies it — String and
        // System.String alike.
        foreach (Match identifier in Identifier.Matches(TypePositions(cref))) {
            if (KeywordFor.ContainsKey(identifier.Value)) { yield return identifier.Value; }
        }

        // Elsewhere only the LEADING segment of the member path is a type. Any.String is the factory method and
        // must not be reported; String.Empty and System.String.Empty must.
        string[] path = MemberPath(cref).Split('.');
        if (path.Length > 0 && KeywordFor.ContainsKey(path[0])) { yield return path[0]; }
        if (path.Length > 1 && path[0] == "System" && KeywordFor.ContainsKey(path[1])) { yield return path[1]; }
    }

    // Everything nested inside braces or parentheses, flattened; the delimiters become separators so that
    // adjacent groups can never be read as one identifier.
    private static string TypePositions(string cref) {
        StringBuilder positions = new();
        int           depth     = 0;

        foreach (char character in cref) {
            if (character is '{' or '(') {
                depth++;
                positions.Append(' ');
            } else if (character is '}' or ')') {
                depth--;
                positions.Append(' ');
            } else if (depth > 0) {
                positions.Append(character);
            }
        }

        return positions.ToString();
    }

    private static string MemberPath(string cref) {
        int end = cref.IndexOfAny(new[] { '{', '(' });

        return end < 0 ? cref : cref.Substring(0, end);
    }

    private static IEnumerable<string> SourceFiles() {
        return Directory.EnumerateDirectories(RepositoryRoot(), "JustDummies*")
                        .SelectMany(project => Directory.EnumerateFiles(project, "*.cs", SearchOption.AllDirectories))
                        .Where(file => !IsBuildOutput(file));
    }

    // obj/ carries generated sources (AssemblyInfo, global usings) and bin/ a copy of whatever was compiled;
    // scanning either would double-count and, worse, report a file nobody can fix.
    private static bool IsBuildOutput(string file) {
        return file.Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)
                   .Any(segment => segment is "bin" or "obj");
    }

    private static string RepositoryRoot() {
        AssemblyMetadataAttribute root = typeof(XmlDocCrefConventionTests).Assembly
                                                                         .GetCustomAttributes<AssemblyMetadataAttribute>()
                                                                         .Single(metadata => metadata.Key == "RepositoryRoot");

        return Path.GetFullPath(root.Value!);
    }

}
