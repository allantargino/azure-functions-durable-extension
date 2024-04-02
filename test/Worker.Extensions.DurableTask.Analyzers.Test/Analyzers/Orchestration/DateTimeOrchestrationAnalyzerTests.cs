using Worker.Extensions.DurableTask.Analyzers.Analyzers.Orchestration;
using VerifyCS = Worker.Extensions.DurableTask.Analyzers.Tests.CSharpAnalyzerVerifier<Worker.Extensions.DurableTask.Analyzers.Analyzers.Orchestration.DateTimeOrchestrationAnalyzer>;

namespace Worker.Extensions.DurableTask.Analyzers.Test.Analyzers.Orchestration
{
    public class DateTimeOrchestrationAnalyzerTests
    {
        [Fact]
        public async Task EmptyCodeNoDiag()
        {
            string code = @"";

            await VerifyCS.VerifyAnalyzerAsync(code);
        }

        [Fact]
        public async Task FunctionsCalledByOrchestratorHaveDiag()
        {
            string code = @"
using System;
using Microsoft.Azure.Functions.Worker;
using Microsoft.DurableTask;

class Orchestrator
{   
    [Function(""Run"")]
    public string Run([OrchestrationTrigger] TaskOrchestrationContext context)
    {
        var _ = CalledFromOrchestrator();

        return ""done"";
    }

    public int CalledFromOrchestrator(){
        DateTime a = {|#0:DateTime.Now|};
        DateTime b = {|#1:DateTime.UtcNow|};
        DateTime c = {|#2:DateTime.Today|};

        return 1;
    }

    public int NotCalled(){
        DateTime d = DateTime.Now;
        return 1;
    }
}";

            var expectedNow = VerifyCS
                .Diagnostic(DateTimeOrchestrationAnalyzer.DiagnosticId)
                .WithLocation(0)
                .WithArguments("System.DateTime.Now");

            var expectedUtcNow = VerifyCS
                .Diagnostic(DateTimeOrchestrationAnalyzer.DiagnosticId)
                .WithLocation(1)
                .WithArguments("System.DateTime.UtcNow");

            var expectedToday = VerifyCS
                .Diagnostic(DateTimeOrchestrationAnalyzer.DiagnosticId)
                .WithLocation(2)
                .WithArguments("System.DateTime.Today");

            await VerifyCS.VerifyDFAnalyzerAsync(code, expectedNow, expectedUtcNow, expectedToday);
        }
    }
}
