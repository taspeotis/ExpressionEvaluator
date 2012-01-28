// -----------------------------------------------------------------------
// <copyright file="ExpressionMeta.cs" company="Todd Aspeotis">
//  Copyright 2012 Todd Aspeotis
// </copyright>
// -----------------------------------------------------------------------

namespace ExpressionEvaluator
{
    using System.Collections.Generic;
    using System.Collections.ObjectModel;

    /// <summary>
    /// Encapsulates information about expressions and their extension objects.
    /// </summary>
    public sealed class ExpressionMeta
    {
        /// <summary>
        /// The language of the expressions.
        /// </summary>
        public readonly string Language;

        /// <summary>
        /// A collection of expressions.
        /// </summary>
        public readonly ExpressionCollection Expressions = new ExpressionCollection();

        /// <summary>
        /// A collection of extensions.
        /// </summary>
        public readonly ExtensionCollection Extensions = new ExtensionCollection();

        /// <summary>
        /// A collection of namespace imports for the extensions.
        /// </summary>
        public readonly Collection<string> Imports = new Collection<string>();

        /// <summary>
        /// A collection of assembly references for the extensions.
        /// </summary>
        public readonly Collection<string> References = new Collection<string>();

        /// <summary>
        /// A collection of non-GAC assemblies' names and paths that are used by the expressions or extensions.
        /// </summary>
        public readonly Dictionary<string, string> CustomAssemblies = new Dictionary<string, string>();

        /// <summary>
        /// Initializes a new instance of the ExpressionMeta class.
        /// </summary>
        /// <remarks>The new instance is suitable for storing VB .NET expressions.</remarks>
        public ExpressionMeta()
            : this("VisualBasic")
        {
        }

        /// <summary>
        /// Initializes a new instance of the ExpressionMeta class.
        /// </summary>
        /// <param name="language">The language of the expressions.</param>
        /// <remarks>
        /// <paramref name="language"/> is limited to the available CodeDOM providers available on the user's system.
        /// </remarks>
        public ExpressionMeta(string language)
        {
            Language = language;
        }

        /// <summary>
        /// Compiles the expressions into an expression evaluator.
        /// </summary>
        /// <returns>An expression evaluator suitable for evaluating the compiled expressions.</returns>
        public Evaluator Compile()
        {
            return ExpressionCompiler.Compile(this);
        }

        /// <summary>
        /// Adds an expression to the collection of expressions.
        /// </summary>
        /// <param name="expression">The expression to add.</param>
        /// <remarks>This is a convenience function.</remarks>
        public void AddExpression(string expression)
        {
            Expressions.Add(expression);
        }

        /// <summary>
        /// Adds an extension to the collection of extensions, and the assembly of its type to
        /// the <see cref="CustomAssemblies"/> dictionary.
        /// </summary>
        /// <param name="extension">The extension to add.</param>
        /// <remarks>This is a convenience function.</remarks>
        public void AddExtension(Extension extension)
        {
            // Add the extension
            AddExtensionIgnoreAssembly(extension);

            // And its assembly
            var extensionAssembly = extension.GetExtensionType().Assembly;
            CustomAssemblies[extensionAssembly.FullName] = extensionAssembly.ManifestModule.FullyQualifiedName;
        }

        /// <summary>
        /// Adds an extension to the collection of extension.
        /// </summary>
        /// <param name="extension">The extension to add.</param>
        /// <remarks>This is a convenience function.</remarks>
        public void AddExtensionIgnoreAssembly(Extension extension)
        {
            Extensions.Add(extension);
        }
    }
}
