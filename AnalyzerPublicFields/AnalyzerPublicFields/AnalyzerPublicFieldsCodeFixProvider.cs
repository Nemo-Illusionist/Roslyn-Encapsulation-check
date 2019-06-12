using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace AnalyzerPublicFields
{
    [ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(AnalyzerPublicFieldsCodeFixProvider)), Shared]
    public class AnalyzerPublicFieldsCodeFixProvider : CodeFixProvider
    {
        private const string title = "Encapsulate field";

        public sealed override ImmutableArray<string> FixableDiagnosticIds
        {
            get { return ImmutableArray.Create(AnalyzerPublicFieldsAnalyzer.DiagnosticId); }
        }

        public sealed override FixAllProvider GetFixAllProvider()
        {
            return WellKnownFixAllProviders.BatchFixer;
        }

        public sealed override async Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken)
                       .ConfigureAwait(false);

            var diagnostic = context.Diagnostics.First();
            var diagnosticSpan = diagnostic.Location.SourceSpan;

            var initialToken = root.FindToken(diagnosticSpan.Start);

            context.RegisterCodeFix(
                CodeAction.Create(title,
                c => EncapsulateFieldAsync(context.Document, initialToken, c),
                AnalyzerPublicFieldsAnalyzer.DiagnosticId),
                diagnostic);
        }

        private async Task<Document> EncapsulateFieldAsync(Document document, SyntaxToken declaration, CancellationToken cancellationToken)
        {
            var field = FindAncestorOfType<FieldDeclarationSyntax>(declaration.Parent);

            var fieldType = field.Declaration.Type;

            ChangeNameFieldAndNameProperty(declaration.ValueText, out string fieldName, out string propertyName);

            var fieldDeclaration = CreateFieldDecaration(fieldName, fieldType);

            var propertyDeclaration = CreatePropertyDecaration(fieldName, propertyName, fieldType);

            var root = await document.GetSyntaxRootAsync();
            var newRoot = root.ReplaceNode(field, new List<SyntaxNode> { fieldDeclaration, propertyDeclaration });
            var newDocument = document.WithSyntaxRoot(newRoot);

            return newDocument;
        }

        private void ChangeNameFieldAndNameProperty(string oldName, out string fieldName, out string propertyName)
        {
            oldName = oldName.TrimStart('_');

            fieldName = "_field";
            propertyName = "Field";

            if (oldName.Length > 0)
            {
                fieldName = Char.ToLower(oldName[0]) + oldName.Substring(1);
                fieldName = $"_{fieldName}";

                propertyName = Char.ToUpper(oldName[0]) + oldName.Substring(1);
            }
        }

        private PropertyDeclarationSyntax CreatePropertyDecaration(string fieldName, string propertyName, TypeSyntax propertyType)
        {
            var syntaxGet = SyntaxFactory.ParseStatement($"return {fieldName};");
            var syntaxSet = SyntaxFactory.ParseStatement($"{fieldName} = value;");

            return SyntaxFactory.PropertyDeclaration(propertyType, propertyName)
                .AddModifiers(SyntaxFactory.Token(SyntaxKind.PublicKeyword))
                .AddAccessorListAccessors(
                    SyntaxFactory.AccessorDeclaration(SyntaxKind.GetAccessorDeclaration).WithBody(SyntaxFactory.Block(syntaxGet)),
                    SyntaxFactory.AccessorDeclaration(SyntaxKind.SetAccessorDeclaration).WithBody(SyntaxFactory.Block(syntaxSet)));
        }

        private FieldDeclarationSyntax CreateFieldDecaration(string fieldName, TypeSyntax fieldType)
        {
            var variableDeclarationField = SyntaxFactory.VariableDeclaration(fieldType)
                .AddVariables(SyntaxFactory.VariableDeclarator(fieldName));

            return SyntaxFactory.FieldDeclaration(variableDeclarationField)
                .AddModifiers(SyntaxFactory.Token(SyntaxKind.PrivateKeyword));
        }

        private T FindAncestorOfType<T>(SyntaxNode node) where T : SyntaxNode
        {
            if (node == null)
            {
                return null;
            }

            if (node is T)
            {
                return node as T;
            }

            return FindAncestorOfType<T>(node.Parent);
        }

    }
}
