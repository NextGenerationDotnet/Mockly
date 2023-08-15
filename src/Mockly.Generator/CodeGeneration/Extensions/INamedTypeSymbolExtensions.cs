namespace Mockly.Generator.CodeGeneration.Extensions;

public static class INamedTypeSymbolExtensions
{
    internal static string GetAccessibilityString(this INamedTypeSymbol namedTypeSymbol) 
        => namedTypeSymbol.DeclaredAccessibility switch
        {
            Accessibility.Public => "public",
            Accessibility.Internal => "internal",
            _ => throw new InvalidOperationException("Accessibility not valid for a named type symbol.")
        };
}