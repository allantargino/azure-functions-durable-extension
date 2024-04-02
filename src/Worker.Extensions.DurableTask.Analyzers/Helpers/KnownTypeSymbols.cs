using System;
using Microsoft.Azure.Functions.Worker;
using Microsoft.CodeAnalysis;

namespace Worker.Extensions.DurableTask.Analyzers.Helpers
{
    /// <summary>
    /// Provides a set of well-known types that are used by the analyzers.
    /// Inspired by KnownTypeSymbols class in
    /// <see href="https://github.com/dotnet/runtime/blob/2a846acb1a92e811427babe3ff3f047f98c5df02/src/libraries/System.Text.Json/gen/Helpers/KnownTypeSymbols.cs">System.Text.Json.SourceGeneration</see> source code.
    /// Lazy initialization is used to avoid the the initialization of all types during class construction, since not all symbols are used in all analyzers.
    /// </summary>
    internal sealed class KnownTypeSymbols
    {
        private readonly Compilation compilation;

        public KnownTypeSymbols(Compilation compilation)
        {
            this.compilation = compilation;
        }

        public INamedTypeSymbol? OrchestrationTriggerAttribute => this.GetOrResolveType(typeof(OrchestrationTriggerAttribute), ref this.orchestrationTriggerAttribute);
        private Cached<INamedTypeSymbol?> orchestrationTriggerAttribute;

        private INamedTypeSymbol? GetOrResolveType(Type type, ref Cached<INamedTypeSymbol?> field)
        {
            return this.GetOrResolveType(type.FullName, ref field);
        }

        private INamedTypeSymbol? GetOrResolveType(string fullyQualifiedName, ref Cached<INamedTypeSymbol?> field)
        {
            if (field.HasValue)
            {
                return field.Value;
            }

            INamedTypeSymbol? type = this.compilation.GetTypeByMetadataName(fullyQualifiedName);
            field = new(type);
            return type;
        }

        // We could use Lazy<T> here, but because we need to use the `compilation` variable instance,
        // that would require us to initiate the Lazy<T> lambdas in the constructor.
        // Because not all analyzers use all symbols, we would be allocating unnecessary lambdas.
        private readonly struct Cached<T>
        {
            public readonly bool HasValue;
            public readonly T Value;

            public Cached(T value)
            {
                this.HasValue = true;
                this.Value = value;
            }
        }
    }
}
