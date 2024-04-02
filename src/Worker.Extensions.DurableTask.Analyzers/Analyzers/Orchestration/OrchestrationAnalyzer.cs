// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;
using Worker.Extensions.DurableTask.Analyzers.Helpers;

namespace Worker.Extensions.DurableTask.Analyzers.Analyzers.Orchestration
{
    public abstract class OrchestrationAnalyzer : DiagnosticAnalyzer
    {
        public override void Initialize(AnalysisContext context)
        {
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
            context.EnableConcurrentExecution();

            context.RegisterCompilationStartAction(context =>
            {
                var knownSymbols = new KnownTypeSymbols(context.Compilation);

                if (knownSymbols.OrchestrationTriggerAttribute == null)
                {
                    return;
                }
                
                ConcurrentDictionary<ISymbol, MethodDeclarationSyntax> methodsReachableByOrchestrations = new(SymbolEqualityComparer.Default);

                context.RegisterSyntaxNodeAction(ctx =>
                {
                    ctx.CancellationToken.ThrowIfCancellationRequested();

                    // Checks whether the declared method is an orchestration
                    ISymbol? methodSymbol = ctx.ContainingSymbol;
                    if (!methodSymbol.ContainsAttributeInAnyMethodArguments(knownSymbols.OrchestrationTriggerAttribute))
                    {
                        return;
                    }

                    var methodSyntax = (MethodDeclarationSyntax)ctx.Node;

                    bool added = methodsReachableByOrchestrations.TryAdd(methodSymbol!, methodSyntax);
                    Debug.Assert(added, "an orchestration method declaration must not be visited twice");

                    this.FindAndAddInvokedMethods(ctx.SemanticModel, methodSyntax, methodsReachableByOrchestrations);
                }, SyntaxKind.MethodDeclaration);

                // allows concrete implementations to register specific actions/analysis and then compare against methodsReachableByOrchestrations
                this.RegisterAdditionalCompilationStartAction(context, methodsReachableByOrchestrations);
            });
        }

        private void FindAndAddInvokedMethods(SemanticModel semanticModel, MethodDeclarationSyntax callerSyntax, ConcurrentDictionary<ISymbol, MethodDeclarationSyntax> methodsReachableByOrchestrations)
        {
            foreach (InvocationExpressionSyntax invocation in callerSyntax.DescendantNodes().OfType<InvocationExpressionSyntax>())
            {
                IOperation? calleOperation = semanticModel.GetOperation(invocation);
                if (calleOperation == null || calleOperation is not IInvocationOperation calleInvocation)
                {
                    continue;
                }

                IMethodSymbol calleeSymbol = calleInvocation.TargetMethod;
                if (calleeSymbol == null)
                {
                    continue;
                }

                // iterating over multiple syntax references is needed because the same method can be declared in multiple places (e.g. partial classes)
                IEnumerable<MethodDeclarationSyntax> calleeSyntaxes = calleeSymbol.DeclaringSyntaxReferences.Select(r => r.GetSyntax()).OfType<MethodDeclarationSyntax>();
                foreach (MethodDeclarationSyntax calleeSyntax in calleeSyntaxes)
                {
                    // if the method was previously visited, skip it
                    if (!methodsReachableByOrchestrations.TryAdd(calleeSymbol, calleeSyntax))
                    {
                        continue;
                    }

                    this.FindAndAddInvokedMethods(semanticModel, calleeSyntax, methodsReachableByOrchestrations);
                }
            }
        }

        /// <summary>
        /// Register additional actions to be executed after the compilation has started.
        /// It is expected from a concrete implementation of <see cref="OrchestrationAnalyzer"/> to register a
        /// <see cref="CompilationStartAnalysisContext.RegisterCompilationEndAction"/>
        /// and then compare that any discovered violations happened in any of the methods in <paramref name="methodsReachableByOrchestrations"/>.
        /// </summary>
        /// <param name="context">Context originally provided by <see cref="AnalysisContext.RegisterCompilationAction"/></param>
        /// <param name="methodsReachableByOrchestrations">Collection of Orchestration methods or methods that are invoked by them</param>
        protected abstract void RegisterAdditionalCompilationStartAction(CompilationStartAnalysisContext context, ConcurrentDictionary<ISymbol, MethodDeclarationSyntax> methodsReachableByOrchestrations);
    }
}
