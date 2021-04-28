using System;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Idiom
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class IdiomAnalyzer : DiagnosticAnalyzer
    {
        public const string DiagnosticId = "WI01001";

        private const string _category = "Lang";

        private static readonly LocalizableString _title =
            GetResourceString(nameof(Resources.AnalyzerTitle));

        private static readonly LocalizableString _messageFormat =
            GetResourceString(nameof(Resources.AnalyzerMessageFormat));

        private static readonly LocalizableString _description =
            GetResourceString(nameof(Resources.AnalyzerDescription));

        private static readonly DiagnosticDescriptor _rule =
            new DiagnosticDescriptor(
                DiagnosticId,
                _title,
                _messageFormat,
                _category,
                DiagnosticSeverity.Warning,
                isEnabledByDefault: true,
                description: _description);

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics
        {
            get { return ImmutableArray.Create(_rule); }
        }

        public override void Initialize(AnalysisContext context)
        {
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
            context.EnableConcurrentExecution();
            context.RegisterSyntaxNodeAction(AnalyzeNameReference, SyntaxKind.SimpleMemberAccessExpression);
        }

        private static void AnalyzeNameReference(SyntaxNodeAnalysisContext context)
        {
            var memberAccess = context.Node as MemberAccessExpressionSyntax;
            if (memberAccess == null) return;

            var symbol = context.SemanticModel.GetSymbolInfo(memberAccess).Symbol;
            if (symbol == null) return;

            // Given a namespace `Foo.Bar` containing a class `Baz` with an inner class `Hello`
            // havimg a static method `World` there are four member accesses within the statement
            // `Foo.Bar.Baz.Hello.World()`; the access of `Bar` within `Foo`, the access of `Baz`
            // within `Foo.Bar`, the access of `Hello` within `Foo.Bar.Baz`, and the access of
            // `World` with `Foo.Bar.Baz.Hello`. All of these member accesses contain unnecessary
            // qualification but we only want to report it once. Every statement with unnecessary
            // qualification will contain precisely one member access of a type within a namespace
            // so targetting that case will ensure we catch every statement once and only once. Any
            // other member access will be of a namespace within a namespace, or of a type, field or
            // method within a type and will be ignored.
            //
            if (!IsTypeWithinNamespace(symbol)) return;

            var accessExpression = memberAccess.ToString();

            if (accessExpression != memberAccess.Name.ToString())
            {
                // todo: Verify name is not ambiguous within scope
                context.ReportDiagnostic(Diagnostic.Create(_rule, memberAccess.GetLocation(), accessExpression));
            }
        }

        private static bool IsTypeWithinNamespace(ISymbol symbol) =>
            symbol.Kind == SymbolKind.NamedType && symbol.ContainingType == null;

        private static LocalizableString GetResourceString(string name) =>
            new LocalizableResourceString(name, Resources.ResourceManager, typeof(Resources));
    }
}
