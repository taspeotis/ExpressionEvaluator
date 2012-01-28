// -----------------------------------------------------------------------
// <copyright file="Evaluator.cs" company="Todd Aspeotis">
//  Copyright 2012 Todd Aspeotis
// </copyright>
// -----------------------------------------------------------------------

namespace ExpressionEvaluator
{
    using System;
    using System.IO;

    /// <summary>
    /// Provides functionality to evaluate compiled expressions.
    /// </summary>
    public sealed class Evaluator : IDisposable
    {
        /// <summary>
        /// The path of the compiled expression assembly.
        /// </summary>
        internal readonly string AssemblyPath;

        /// <summary>
        /// The sandbox application domain.
        /// </summary>
        internal readonly AppDomain AppDomain;

        /// <summary>
        /// The remote sandbox object.
        /// </summary>
        internal readonly Sandbox Sandbox;

        /// <summary>
        /// Initializes a new instance of the Evaluator class.
        /// </summary>
        /// <param name="appDomain">The sandbox application domain.</param>
        /// <param name="sandbox">The remote sandbox object.</param>
        /// <param name="assemblyPath">The path of the compiled expression assembly.</param>
        internal Evaluator(AppDomain appDomain, Sandbox sandbox, string assemblyPath)
        {
            AppDomain = appDomain;
            Sandbox = sandbox;
            AssemblyPath = assemblyPath;
        }
        
        /// <summary>
        /// Finalizes an instance of the Evaluator class.
        /// </summary>
        ~Evaluator()
        {
            Dispose(false);
        }

        /// <summary>
        /// Gets a value indicating whether the evaluator has been disposed of.
        /// </summary>
        public bool IsDisposed { get; private set; }

        /// <summary>
        /// Disposes the evaluator.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Evaluates a previously compiled expression.
        /// </summary>
        /// <param name="expression">The expression to evaluate.</param>
        /// <returns>The result of evaluating the expression, or <see langword="null"/> if an error occurs.</returns>
        public object Evaluate(string expression)
        {
            try
            {
                var method = ExpressionCompiler.GetExpressionMethodName(expression);
                return Sandbox.Invoke(method);
            }
            catch (Exception)
            {
                return null;
            }
        }

        /// <summary>
        /// Disposes the evaluator.
        /// </summary>
        /// <param name="disposing">
        /// <see langword="true"/> if the object is being disposed.
        /// <see langword="false"/> if the object is being finalized.
        /// </param>
        private void Dispose(bool disposing)
        {
            if (IsDisposed)
                return;

            if (disposing)
            {
                // Managed
                AppDomain.Unload(AppDomain);
            }

            // Unmanaged
            File.Delete(AssemblyPath);

            IsDisposed = true;
        }
    }
}
