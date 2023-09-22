using System.Collections.Immutable;
using System.Diagnostics;
using System.Reflection.Metadata;
using System.Text;

using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Text;

namespace Mockly.Generator;

[Generator]
public class MocklyGenerator : IIncrementalGenerator
{
    const string MocklifyAttributeName = "Mockly.MocklifyAttribute";

    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        IncrementalValuesProvider<MethodDeclarationSyntax?> methodDeclarationSyntax = context.SyntaxProvider
            .ForAttributeWithMetadataName(
                MocklifyAttributeName,
                predicate: static (s, _) => IsSyntaxTargetForGeneration(s),
                transform: static (ctx, _) => GetSemanticTargetForGeneration(ctx))
            .Where(static m => m is not null);

        IncrementalValueProvider<(AnalyzerConfigOptionsProvider OptionsProvider, (Compilation Compilation, ImmutableArray<MethodDeclarationSyntax?> MethodDeclarations) Syntax)> methodDeclarationsAndAnalyzerConfigOptions
            = context.AnalyzerConfigOptionsProvider.Combine(context.CompilationProvider.Combine(methodDeclarationSyntax.Collect()));

        context.RegisterSourceOutput(methodDeclarationsAndAnalyzerConfigOptions,
            static (spc, source) => Execute(source.Syntax.Compilation, source.Syntax.MethodDeclarations!, spc, source.OptionsProvider));
    }

    private static bool IsSyntaxTargetForGeneration(SyntaxNode syntaxNode) =>
        syntaxNode is MethodDeclarationSyntax { AttributeLists.Count: > 0 };

    private static MethodDeclarationSyntax? GetSemanticTargetForGeneration(GeneratorAttributeSyntaxContext context)
    {
        var methodDeclarationSyntax = context.TargetNode as MethodDeclarationSyntax;

        foreach (var attributeList in methodDeclarationSyntax!.AttributeLists)
        {
            foreach (var attribute in attributeList.Attributes)
            {
                if (ModelExtensions.GetSymbolInfo(context.SemanticModel, attribute).Symbol is not IMethodSymbol attributeSymbol)
                {
                    continue;
                }

                INamedTypeSymbol attributeContainingTypeSymbol = attributeSymbol.ContainingType;
                string fullName = attributeContainingTypeSymbol.ToDisplayString();

                if (fullName == MocklifyAttributeName)
                {
                    return methodDeclarationSyntax;
                }
            }
        }

        return null;
    }

    static void Execute(Compilation compilation, ImmutableArray<MethodDeclarationSyntax> methods, SourceProductionContext context, AnalyzerConfigOptionsProvider optionsProvider)
    {
        if (methods.IsDefaultOrEmpty)
        {
            return;
        }

        bool debugOutputEnabled = optionsProvider.GlobalOptions.TryGetValue("build_property.ArgumentativeFilters_WriteDebug", out string? _);

        StringBuilder sb = new();

        sb.Append(CodeSnippets.ArgumentativeFilterFileHeader);
        Stopwatch codegenTimer = new();

        if (debugOutputEnabled)
        {
            codegenTimer.Start();
        }

        foreach (var filter in methods)
        {
            GenerateFilterFactory(compilation, filter, sb);
            sb.AppendLine();
        }

        sb.AppendLine(ConstantTypeCode.ArgumentativeFiltersParameterHelpers);

        var sourceText = SourceText.From(sb.ToString(), Encoding.UTF8);
        if (debugOutputEnabled)
        {
            var elapsed = codegenTimer.Elapsed;
            sourceText = sourceText.WithChanges(new List<TextChange>(1)
            {
                new(TextSpan.FromBounds(sourceText.Length, sourceText.Length),
                    $"\n// Generated filter factories in {elapsed.TotalMilliseconds}ms.\n")
            });
        }

        context.AddSource($"ArgumentativeFilters.g.cs", sourceText);
    }
}