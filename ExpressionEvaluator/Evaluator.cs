// -----------------------------------------------------------------------
// <copyright file="Evaluator.cs" company="Todd Aspeotis">
//  Copyright 2012, Todd Aspeotis
// </copyright>
// -----------------------------------------------------------------------

namespace ExpressionEvaluator
{
    using System;
    using System.IO;

    public sealed class Evaluator : IDisposable
    {

        internal readonly string AssemblyPath;

        internal readonly AppDomain AppDomain;

        internal readonly Sandbox Sandbox;

        internal Evaluator(AppDomain appDomain, Sandbox sandbox, string assemblyPath)
        {
            AppDomain = appDomain;
            Sandbox = sandbox;
            AssemblyPath = assemblyPath;
        }

        ~Evaluator()
        {
            Dispose(false);
        }

        public bool IsDisposed { get; set; }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

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
    }
}
