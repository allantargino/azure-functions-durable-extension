// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using Worker.Extensions.DurableTask.Analyzers.Analyzers.Orchestration;
using VerifyCS = Worker.Extensions.DurableTask.Analyzers.Tests.CSharpAnalyzerVerifier<Worker.Extensions.DurableTask.Analyzers.Analyzers.Orchestration.DateTimeOrchestrationAnalyzer>;

namespace Worker.Extensions.DurableTask.Analyzers.Test.Analyzers.Orchestration
{
    public class DateTimeOrchestrationAnalyzerTests
    {
        // checks that an empty code with no assembly references of Durable Functions has no diagnostics
        // this guarantees that if someone adds our analyzer to a project that doesn't use Durable Functions,
        // the analyzer won't crash/they won't get any diagnostics
        [Fact]
        public async Task EmptyCodeHasNoDiag()
        {
            string code = @"";

            await VerifyCS.VerifyAnalyzerAsync(code);
        }

        // checks that an empty code with access to assembly references of Durable Functions has no diagnostics
        [Fact]
        public async Task EmptyCodeWithSymbolsAvailableHasNoDiag()
        {
            string code = @"";

            await VerifyCS.VerifyDurableTaskAnalyzerAsync(code);
        }

        [Fact]
        public async Task NonOrchestrationHasNoDiag()
        {
            string code = @"
using System;

class NotOrchestration
{   
    public void Run(){
        Console.WriteLine(DateTime.Now);
    }
}
";

            await VerifyCS.VerifyDurableTaskAnalyzerAsync(code);
        }

        [Fact]
        public async Task OrchestrationUsingDateTimeNowHasDiag()
        {
            string code = @"
using System;
using Microsoft.Azure.Functions.Worker;
using Microsoft.DurableTask;

class Orchestrator
{   
    [Function(""Run"")]
    public DateTime Run([OrchestrationTrigger] TaskOrchestrationContext context)
    {
        DateTime date = {|#0:DateTime.Now|};
        return date;
    }
}";

            var expected = VerifyCS
                .Diagnostic(DateTimeOrchestrationAnalyzer.DiagnosticId)
                .WithLocation(0)
                .WithArguments("System.DateTime.Now");

            await VerifyCS.VerifyDurableTaskAnalyzerAsync(code, expected);
        }

        [Fact]
        public async Task OrchestrationUsingDateTimeUtcNowHasDiag()
        {
            string code = @"
using System;
using Microsoft.Azure.Functions.Worker;
using Microsoft.DurableTask;

class Orchestrator
{   
    [Function(""Run"")]
    public DateTime Run([OrchestrationTrigger] TaskOrchestrationContext context)
    {
        DateTime date = {|#0:DateTime.UtcNow|};
        return date;
    }
}";

            var expected = VerifyCS
                .Diagnostic(DateTimeOrchestrationAnalyzer.DiagnosticId)
                .WithLocation(0)
                .WithArguments("System.DateTime.UtcNow");

            await VerifyCS.VerifyDurableTaskAnalyzerAsync(code, expected);
        }

        [Fact]
        public async Task OrchestrationUsingDateTimeTodayNowHasDiag()
        {
            string code = @"
using System;
using Microsoft.Azure.Functions.Worker;
using Microsoft.DurableTask;

class Orchestrator
{   
    [Function(""Run"")]
    public DateTime Run([OrchestrationTrigger] TaskOrchestrationContext context)
    {
        DateTime date = {|#0:DateTime.Today|};
        return date;
    }
}";

            var expected = VerifyCS
                .Diagnostic(DateTimeOrchestrationAnalyzer.DiagnosticId)
                .WithLocation(0)
                .WithArguments("System.DateTime.Today");

            await VerifyCS.VerifyDurableTaskAnalyzerAsync(code, expected);
        }

        [Fact]
        public async Task FunctionCalledByOrchestratorHasDiag()
        {
            string code = @"
using System;
using Microsoft.Azure.Functions.Worker;
using Microsoft.DurableTask;

class Orchestrator
{   
    [Function(""Run"")]
    public long Run([OrchestrationTrigger] TaskOrchestrationContext context)
    {
        return CalledFromOrchestrator1();
    }

    public long CalledFromOrchestrator1() => CalledFromOrchestrator2();
    
    public long CalledFromOrchestrator2() => CalledFromOrchestrator3();

    public long CalledFromOrchestrator3(){
        DateTime a = {|#0:DateTime.Now|};
        DateTime b = {|#1:DateTime.UtcNow|};
        DateTime c = {|#2:DateTime.Today|};

        return a.Ticks + b.Ticks + c.Ticks;
    }
}";

            var expected = VerifyCS.Diagnostic(DateTimeOrchestrationAnalyzer.DiagnosticId);
            var expectedNow = expected.WithLocation(0).WithArguments("System.DateTime.Now");
            var expectedUtcNow = expected.WithLocation(1).WithArguments("System.DateTime.UtcNow");
            var expectedToday = expected.WithLocation(2).WithArguments("System.DateTime.Today");

            await VerifyCS.VerifyDurableTaskAnalyzerAsync(code, expectedNow, expectedUtcNow, expectedToday);
        }

        [Fact]
        public async Task FunctionCalledByMultipleOrchestratorHasDiag()
        {
            string code = @"
using System;
using Microsoft.Azure.Functions.Worker;
using Microsoft.DurableTask;

class Orchestrator
{   
    [Function(""Run1"")]
    public long Run1([OrchestrationTrigger] TaskOrchestrationContext context)
    {
        return CalledFromOrchestrator();
    }

    [Function(""Run2"")]
    public long Run2([OrchestrationTrigger] TaskOrchestrationContext context)
    {
        return CalledFromOrchestrator();
    }

    public long CalledFromOrchestrator(){
        return {|#0:DateTime.Now|}.Ticks;
    }
}";

            var expected = VerifyCS
                .Diagnostic(DateTimeOrchestrationAnalyzer.DiagnosticId)
                .WithLocation(0)
                .WithArguments("System.DateTime.Now");

            await VerifyCS.VerifyDurableTaskAnalyzerAsync(code, expected);
        }

        [Fact]
        public async Task RecursiveFunctionCalledByOrchestratorHasSingleDiag()
        {
            string code = @"
using System;
using Microsoft.Azure.Functions.Worker;
using Microsoft.DurableTask;

class Orchestrator
{   
    [Function(""Run"")]
    public long Run([OrchestrationTrigger] TaskOrchestrationContext context)
    {
        return RecursiveFunction(0);
    }

    public long RecursiveFunction(int i){
        if (i == 10) return 1;
        DateTime date = {|#0:DateTime.Now|};
        return date.Ticks + RecursiveFunction(i + 1);
    }
}";

            var expected = VerifyCS
                .Diagnostic(DateTimeOrchestrationAnalyzer.DiagnosticId)
                .WithLocation(0)
                .WithArguments("System.DateTime.Now");

            await VerifyCS.VerifyDurableTaskAnalyzerAsync(code, expected);
        }

        [Fact]
        public async Task FunctionNotCalledByOrchestratorHasNoDiag()
        {
            string code = @"
using System;
using Microsoft.Azure.Functions.Worker;
using Microsoft.DurableTask;

class Orchestrator
{   
    [Function(""Run"")]
    public DateTime Run([OrchestrationTrigger] TaskOrchestrationContext context)
    {
        return new DateTime(2024, 1, 1);
    }

    public DateTime NotCalled(){
        return DateTime.Now;
    }
}";

            await VerifyCS.VerifyDurableTaskAnalyzerAsync(code);
        }

        [Fact]
        public async Task OrchestrationUsingDateTimeInLambdasHasDiag()
        {
            string code = @"
using System;
using Microsoft.Azure.Functions.Worker;
using Microsoft.DurableTask;

class Orchestrator
{   
    [Function(""Run"")]
    public void Run([OrchestrationTrigger] TaskOrchestrationContext context)
    {
        static DateTime fn0() => {|#0:DateTime.Now|};
        Func<DateTime> fn1 = () => {|#1:DateTime.Now|};
        Func<int, DateTime> fn2 = days => {|#2:DateTime.Now|}.AddDays(days);
        Action<int> fn3 = days => Console.WriteLine({|#3:DateTime.Now|}.AddDays(days));
    }
}";

            var expected = Enumerable.Range(0, 4).Select(i => VerifyCS
                .Diagnostic(DateTimeOrchestrationAnalyzer.DiagnosticId)
                .WithLocation(i)
                .WithArguments("System.DateTime.Now")).ToArray();

            await VerifyCS.VerifyDurableTaskAnalyzerAsync(code, expected);
        }

        [Fact]
        public async Task OrchestrationUsingAsyncInvocationsHasDiag()
        {
            string code = @"
using System;
using System.Threading.Tasks;
using Microsoft.Azure.Functions.Worker;
using Microsoft.DurableTask;

class Orchestrator
{   
    [Function(nameof(Run))]
    public async Task<DateTime> Run([OrchestrationTrigger] TaskOrchestrationContext context)
    {
        _ = await ValueTaskInvocation();
        return await TaskInvocation();
    }

    public static ValueTask<DateTime> ValueTaskInvocation() => ValueTask.FromResult({|#0:DateTime.Now|});

    public static Task<DateTime> TaskInvocation() => Task.FromResult({|#1:DateTime.Now|});
}";

            var expected = Enumerable.Range(0, 2).Select(i => VerifyCS
                .Diagnostic(DateTimeOrchestrationAnalyzer.DiagnosticId)
                .WithLocation(i)
                .WithArguments("System.DateTime.Now")).ToArray();

            await VerifyCS.VerifyDurableTaskAnalyzerAsync(code, expected);
        }

        // TODO: Can we add FunctionName to the diagnostic message? (e.g. "Orchestration 'Run' has non-deterministic behavior by the direct usage of System.DateTime.Now" or "Orchestration 'Run' has non-deterministic behavior by calling the method 'Blah', which uses System.DateTime.Now")
        // Orchestration 'Run' has non-deterministic behavior by the direct usage of System.DateTime.Now
        // Orchestration 'Run' has non-deterministic behavior by calling the method 'Blah', which uses System.DateTime.Now
        // Orchestrations 'Run1', 'Run2' have non-deterministic behavior by calling the method 'Blah', which uses System.DateTime.Now
    }
}
