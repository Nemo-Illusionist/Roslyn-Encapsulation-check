using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace AnalyzerPublicFields
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class AnalyzerPublicFieldsAnalyzer : DiagnosticAnalyzer
    {
        public const string DiagnosticId = "PublicField";
        private const string Title = "Filed is public";
        private const string MessageFormat = "Field '{0}' is public";
        private const string Category = "Syntax";

        private static DiagnosticDescriptor Rule = new DiagnosticDescriptor(DiagnosticId, Title, 
            MessageFormat, Category, DiagnosticSeverity.Warning, isEnabledByDefault: true);

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics
        {
            get
            {
                return ImmutableArray.Create(Rule);
            }
        }

        public override void Initialize(AnalysisContext context)
        {
            context.RegisterSymbolAction(AnalyzeSymbol, SymbolKind.Field);
        }

        private static void AnalyzeSymbol(SymbolAnalysisContext context)
        {
            var fieldSymbol = context.Symbol as IFieldSymbol;

            if (fieldSymbol != null && fieldSymbol.DeclaredAccessibility == Accessibility.Public
                && !fieldSymbol.IsConst && !fieldSymbol.IsAbstract && !fieldSymbol.IsStatic
                && !fieldSymbol.IsVirtual && !fieldSymbol.IsOverride && !fieldSymbol.IsReadOnly
                && !fieldSymbol.IsSealed && !fieldSymbol.IsExtern)
            {
                var diagnostic = Diagnostic.Create(Rule, fieldSymbol.Locations[0], fieldSymbol.Name);

                context.ReportDiagnostic(diagnostic);
            }
        }
    }
}
