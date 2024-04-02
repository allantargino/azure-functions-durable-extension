// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System;
using System.Collections.Concurrent;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;
using Microsoft.CodeAnalysis;
using Worker.Extensions.DurableTask.Analyzers.Helpers;

namespace Worker.Extensions.DurableTask.Analyzers.Analyzers.Orchestration
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public sealed class DateTimeOrchestrationAnalyzer : OrchestrationAnalyzer
    {
        public const string DiagnosticId = "DF1101";

        private static readonly DiagnosticDescriptor Rule = new(DiagnosticId, "TODO", "TODO", "TODO", DiagnosticSeverity.Warning, isEnabledByDefault: true, description: "TODO");

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

        protected override void RegisterAdditionalCompilationStartAction(CompilationStartAnalysisContext context, ConcurrentDictionary<ISymbol, MethodDeclarationSyntax> methodsReachableByOrchestrators)
        {
            INamedTypeSymbol systemDateTimeSymbol = context.Compilation.GetSpecialType(SpecialType.System_DateTime);
            
            ConcurrentBag<(ISymbol method, IPropertyReferenceOperation operation)> dateTimeUsage = new();

            // search for usages of DateTime.Now, DateTime.UtcNow, DateTime.Today and store them
            context.RegisterOperationAction(ctx =>
            {
                ctx.CancellationToken.ThrowIfCancellationRequested();

                var operation = (IPropertyReferenceOperation)ctx.Operation;
                IPropertySymbol property = operation.Property;

                if (property.ContainingSymbol.Equals(systemDateTimeSymbol, SymbolEqualityComparer.Default) &&
                    property.Name is nameof(DateTime.Now) or nameof(DateTime.UtcNow) or nameof(DateTime.Today))
                {
                    ISymbol method = ctx.ContainingSymbol;
                    dateTimeUsage.Add((method, operation));
                }
            }, OperationKind.PropertyReference);

            // compare whether the found DateTime usages occur in methods reachable by orchestrators
            context.RegisterCompilationEndAction(ctx =>
            {
                foreach ((ISymbol method, IPropertyReferenceOperation operation) in dateTimeUsage)
                {
                    if (methodsReachableByOrchestrators.ContainsKey(method))
                    {
                        ctx.ReportDiagnostic(Rule, operation, operation.Property.ToString());
                    }
                }
            });
        }
    }
}
