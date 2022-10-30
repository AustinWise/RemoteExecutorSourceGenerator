using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace RemoteExecutorSourceGenerator
{
    [Generator]
    public class RemoteExecutorGenerator : IIncrementalGenerator
    {
        public const string GlobalNamespaceValue = "<global namespace>";

        public void Initialize(IncrementalGeneratorInitializationContext context)
        {
            IncrementalValuesProvider<MethodDeclarationSyntax> methodDecs = context.SyntaxProvider.CreateSyntaxProvider(IsRelevantSyntax, TransformSyntaxForGeneration).Where(m => m is not null)!;
            IncrementalValueProvider<(Compilation, ImmutableArray<MethodDeclarationSyntax>)> compilationAndEnums
                = context.CompilationProvider.Combine(methodDecs.Collect());

            context.RegisterSourceOutput(compilationAndEnums,
                static (spc, source) => Execute(source.Item1, source.Item2, spc));
        }

        bool IsRelevantSyntax(SyntaxNode node, CancellationToken ct)
        {
            if (node is MethodDeclarationSyntax meth)
            {
                return meth.AttributeLists.Count > 0;
            }

            ct.ThrowIfCancellationRequested();

            return false;
        }

        MethodDeclarationSyntax? TransformSyntaxForGeneration(GeneratorSyntaxContext ctx, CancellationToken ct)
        {
            var meth = (MethodDeclarationSyntax)ctx.Node;

            foreach (AttributeListSyntax attributeListSyntax in meth.AttributeLists)
            {
                foreach (AttributeSyntax attributeSyntax in attributeListSyntax.Attributes)
                {
                    if (ctx.SemanticModel.GetSymbolInfo(attributeSyntax).Symbol is not IMethodSymbol attributeSymbol)
                    {
                        continue;
                    }

                    INamedTypeSymbol attributeContainingTypeSymbol = attributeSymbol.ContainingType;
                    string fullName = attributeContainingTypeSymbol.ToDisplayString();

                    if (fullName == "RemoteExecutorLib.RemotelyInvokableAttribute")
                    {
                        return meth;
                    }
                }
            }

            ct.ThrowIfCancellationRequested();

            return null;
        }

        static void Execute(Compilation compilation, ImmutableArray<MethodDeclarationSyntax> methods, SourceProductionContext context)
        {
            if (methods.IsDefaultOrEmpty)
            {
                return;
            }

            var sb = new StringBuilder();

            foreach (var meth in methods)
            {
                context.CancellationToken.ThrowIfCancellationRequested();

                ProcessOneMethod(meth, sb, compilation, context);
            }

            context.AddSource("RemotelyInvokable.g.cs", SourceText.From(sb.ToString(), Encoding.UTF8));
        }

        static void ProcessOneMethod(MethodDeclarationSyntax meth, StringBuilder sb, Compilation compilation, SourceProductionContext context)
        {
            // TODO: check types. currently assuming all parameters are strings and return type is either void or int.
            // TODO: make sure if the method is non-static that the type has a zero-argument constructor

            var parentClass = meth.Parent as ClassDeclarationSyntax;

            var semanticModel = compilation.GetSemanticModel(parentClass.SyntaxTree);

            var parentTypeSymbol = semanticModel.GetDeclaredSymbol(parentClass);

            var ns = parentTypeSymbol.ContainingNamespace.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat.WithGlobalNamespaceStyle(SymbolDisplayGlobalNamespaceStyle.OmittedAsContaining));
            var className = parentTypeSymbol.Name;

            var methodSymbol = semanticModel.GetDeclaredSymbol(meth) as IMethodSymbol;

            // TODO: make a key based on: assembly + declaring class + method + signature
            var key = Guid.NewGuid();

            if (ns != GlobalNamespaceValue)
            {
                sb.AppendLine($"namespace {ns}");
                sb.AppendLine("{");
            }

            sb.AppendLine($"partial class {className}");
            sb.AppendLine("{");

            // TODO: register all the methods for a single class in one initializer method.
            sb.AppendLine("    [global::System.Runtime.CompilerServices.ModuleInitializer]");
            sb.AppendLine($"    internal static void Register{methodSymbol.Name}()");
            sb.AppendLine("    {");
            sb.Append(' ', 8);
            sb.AppendLine($"global::RemoteExecutorLib.MethodRegistry.RegisterMethod(\"{key}\", {methodSymbol.Parameters.Length}, new global::System.Func<string[], int?>(args => {{");
            sb.Append(' ', 12);
            if (!methodSymbol.ReturnsVoid)
            {
                sb.Append("return ");
            }
            if (!methodSymbol.IsStatic)
            {
                sb.Append($"new {className}().");
            }
            sb.Append($"{methodSymbol.Name}(");
            for (int i = 0; i < methodSymbol.Parameters.Length; i++)
            {
                sb.Append($"args[{i}]");
                if (i != (methodSymbol.Parameters.Length - 1))
                    sb.Append(", ");
            }
            sb.AppendLine(");");
            if (methodSymbol.ReturnsVoid)
            {
                sb.Append(' ', 12);
                sb.AppendLine("return null;");
            }
            sb.Append(' ', 8);
            sb.AppendLine("}));");
            sb.AppendLine("    }");

            // TODO: if the instance is disposable, try to dispose it after invoking
            sb.Append($"    private global::RemoteExecutorLib.RemoteInvokeHandle Invoke{methodSymbol.Name}(");
            for (int i = 0; i < methodSymbol.Parameters.Length; i++)
            {
                sb.Append($"string {methodSymbol.Parameters[i].Name}, ");
            }
            sb.AppendLine("global::RemoteExecutorLib.RemoteInvokeOptions __options = null)");
            sb.Append(' ', 4);
            sb.AppendLine("{");
            sb.Append(' ', 8);
            sb.Append($"return global::RemoteExecutorLib.RemoteExecutor.Invoke(typeof({className}).Assembly, \"{key}\", new string[] {{");
            for (int i = 0; i < methodSymbol.Parameters.Length; i++)
            {
                sb.Append(methodSymbol.Parameters[i].Name);
                if (i != (methodSymbol.Parameters.Length - 1))
                    sb.Append(", ");
            }
            sb.AppendLine("}, __options);");
            sb.Append(' ', 4);
            sb.AppendLine("}");



            sb.AppendLine("}");

            if (ns != GlobalNamespaceValue)
            {
                sb.AppendLine($"}} // namespace {ns}");
            }
        }
    }
}
