// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis;
using System.Linq;

namespace Worker.Extensions.DurableTask.Analyzers.Helpers
{
    internal static class RoslynExtensions
    {
        public static bool ContainsAttribute(this ISymbol? symbol, INamedTypeSymbol attributeSymbol)
        {
            if (symbol == null)
            {
                return false;
            }

            return symbol.GetAttributes()
                .Any(a => attributeSymbol.Equals(a.AttributeClass, SymbolEqualityComparer.Default));
        }

        public static bool ContainsAttributeInAnyMethodArguments(this ISymbol? symbol, INamedTypeSymbol attributeSymbol)
        {
            if (symbol is not IMethodSymbol methodSymbol)
            {
                return false;
            }

            return methodSymbol.Parameters
                .SelectMany(p => p.GetAttributes())
                .Any(a => attributeSymbol.Equals(a.AttributeClass, SymbolEqualityComparer.Default));
        }

        public static void ReportDiagnostic(this CompilationAnalysisContext ctx, DiagnosticDescriptor descriptor, IOperation operation, params string[] messageArgs)
        {
            ctx.ReportDiagnostic(BuildDiagnostic(descriptor, operation.Syntax, messageArgs));
        }

        public static Diagnostic BuildDiagnostic(DiagnosticDescriptor descriptor, SyntaxNode syntaxNode, params string[] messageArgs)
        {
            return Diagnostic.Create(descriptor, syntaxNode.GetLocation(), messageArgs);
        }
    }
}
