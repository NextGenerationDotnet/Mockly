using System.Text;

using Mockly.Generator.CodeGeneration.Extensions;

namespace Mockly.Generator.CodeGeneration;

public class ContainingHierarchyBuilder
{
    private int _hierarchyLevelCount = 0;
    private readonly Dictionary<int, string> _indentationCache = new();
    private readonly StringBuilder _builder;

    public int CurrentIndentationLevel { get; private set; }

    public ContainingHierarchyBuilder(StringBuilder builder)
    {
        _builder = builder;
        CurrentIndentationLevel = 0;
    }

    public ContainingHierarchyBuilder AddContainingHierarchy(IEnumerable<INamedTypeSymbol> containingClassSymbols, string containingNamespace)
    {
        AddContainingNamespace(containingNamespace);

        foreach (var containingClassSymbol in containingClassSymbols)
        {
            AddContainingClass(containingClassSymbol);
        }

        return this;
    }

    private ContainingHierarchyBuilder AddContainingNamespace(string containingNamespace)
    {
        _builder.AppendLine($"namespace {containingNamespace}");
        _builder.AppendLine("{");
        CurrentIndentationLevel += Constants.IndentationPerLevel;
        return this;
    }

    private ContainingHierarchyBuilder AddContainingClass(INamedTypeSymbol containingClassSymbol)
    {
        _hierarchyLevelCount++;

        var indentation = GetOrCreateIndentationLevel(CurrentIndentationLevel);
        var containingClassAccessibility = containingClassSymbol.GetAccessibilityString();

        var containingTypeStatic = containingClassSymbol.IsStatic ? "static " : string.Empty;

        var containingTypeKind = containingClassSymbol.TypeKind switch
        {
            TypeKind.Class => "class",
            TypeKind.Struct => "struct",
            _ => throw new InvalidOperationException("Only classes and structs can be used as containing types."),
        };

        var containingClassName = containingClassSymbol.Name;

        _builder.AppendLine($"{indentation}{containingClassAccessibility} {containingTypeStatic}partial {containingTypeKind} {containingClassName}");
        _builder.AppendLine($"{indentation}{{");

        CurrentIndentationLevel += Constants.IndentationPerLevel;
        return this;
    }

    public ContainingHierarchyBuilder CloseContainingHierarchy()
    {
        for (var i = 1; i < _hierarchyLevelCount + 1; i++)
        {
            var indentation = GetOrCreateIndentationLevel(CurrentIndentationLevel - i * Constants.IndentationPerLevel);
            _builder.AppendLine($"{indentation}}}");
        }

        // Close containing namespace
        _builder.AppendLine("}");

        return this;
    }

    private string GetOrCreateIndentationLevel(int indentationLevel)
    {
        if (_indentationCache.TryGetValue(indentationLevel, out var indentation))
        {
            return indentation;
        }

        var indentationString = new string(' ', indentationLevel);
        _indentationCache.Add(indentationLevel, indentationString);
        return indentationString;
    }

    public string Build()
    {
        return _builder.ToString();
    }
}