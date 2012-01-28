// -----------------------------------------------------------------------
// <copyright file="ExpressionMeta.cs" company="Todd Aspeotis">
//  Copyright 2012, Todd Aspeotis
// </copyright>
// -----------------------------------------------------------------------

namespace ExpressionEvaluator
{
    using System.Collections.Generic;
    using System.Collections.ObjectModel;

    /// <summary>
    /// TODO: Update summary.
    /// </summary>
    public sealed class ExpressionMeta
    {
        public readonly string Language;

        public readonly ExpressionCollection Expressions = new ExpressionCollection();

        public readonly ExtensionCollection Extensions = new ExtensionCollection();

        public readonly Collection<string> Imports = new Collection<string>();

        public readonly Collection<string> References = new Collection<string>();

        public readonly Dictionary<string, string> CustomAssemblies = new Dictionary<string,string>();

        public ExpressionMeta()
            : this("VisualBasic")
        {
        }

        public ExpressionMeta(string language)
        {
            Language = language;
        }

        public Evaluator Compile()
        {
            return ExpressionCompiler.Compile(this);
        }

        public void AddExpression(string expression)
        {
            Expressions.Add(expression);
        }

        public void AddExtensionIgnoreAssembly(Extension extension)
        {
            Extensions.Add(extension);
        }

        public void AddExtension(Extension extension)
        {
            // Add the extension
            AddExtensionIgnoreAssembly(extension);

            // And its assembly
            var extensionAssembly = extension.GetExtensionType().Assembly;
            CustomAssemblies[extensionAssembly.FullName] = extensionAssembly.ManifestModule.FullyQualifiedName;
        }
    }
}
