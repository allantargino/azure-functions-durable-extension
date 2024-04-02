// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Testing;

namespace Worker.Extensions.DurableTask.Analyzers.Tests
{
    public static partial class CSharpAnalyzerVerifier<TAnalyzer>
        where TAnalyzer : DiagnosticAnalyzer, new()
    {
        public static async Task VerifyDFAnalyzerAsync(string source, params DiagnosticResult[] expected)
        {
            var test = new Test()
            {
                TestCode = source,
                ReferenceAssemblies = ReferenceAssemblies.Net.Net60
            };

            var references = new List<MetadataReference>
            {
                MetadataReference.CreateFromFile(typeof(Microsoft.Azure.Functions.Worker.FunctionAttribute).Assembly.Location),             // Microsoft.Azure.Functions.Worker.Extensions.Abstractions
                MetadataReference.CreateFromFile(typeof(Microsoft.Azure.Functions.Worker.OrchestrationTriggerAttribute).Assembly.Location), // Microsoft.Azure.Functions.Worker.Extensions.DurableTask
                MetadataReference.CreateFromFile(typeof(Microsoft.DurableTask.TaskOrchestrationContext).Assembly.Location),                 // Microsoft.DurableTask.Abstractions
            };                                                                                                                              

            test.TestState.AdditionalReferences.AddRange(references);

            test.ExpectedDiagnostics.AddRange(expected);

            await test.RunAsync(CancellationToken.None);
        }
    }
}
