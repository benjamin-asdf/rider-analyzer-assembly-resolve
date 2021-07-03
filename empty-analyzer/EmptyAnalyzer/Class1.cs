using System;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;

namespace EmptyAnalyzer {
    
    

    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class BestAnalyzer : DiagnosticAnalyzer {

        string id = "TEST1337";
        string title = "Test empty analzyer";
        string description = "This is a class declaration!" ;
        string category = "EmptyTest";
        DiagnosticSeverity severity = DiagnosticSeverity.Error;

        DiagnosticDescriptor m_diagnostic ;
        protected  DiagnosticDescriptor diagnostic => m_diagnostic ?? (m_diagnostic = new DiagnosticDescriptor(id,title,description,category,severity,true));

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(diagnostic);


        public override void Initialize(AnalysisContext context) {

            context.RegisterSyntaxNodeAction((ctx => {
                ctx.ReportDiagnostic(Diagnostic.Create(diagnostic,ctx.Node.GetLocation()));
            }), SyntaxKind.ClassDeclaration);


        }


    }

    
}

// context.RegisterSyntaxNodeAction((analysisContext => {
//     //
//     //var node
//     // call the get block method here
// }), SyntaxKind.ParenthesizedLambdaExpression);
