// -----------------------------------------------------------------------
// <copyright file="Sandbox.cs" company="Todd Aspeotis">
//  Copyright 2012 Todd Aspeotis
// </copyright>
// -----------------------------------------------------------------------

namespace ExpressionEvaluator
{
    using System;
    using System.Collections.Generic;
    using System.Reflection;
    using System.Security;
    using System.Security.Permissions;

    /// <summary>
    /// Provides functionality for sandboxing compiled expressions.
    /// </summary>
    internal sealed class Sandbox : MarshalByRefObject
    {
        /// <summary>
        /// Gets or sets an instance of the compiled expressions object.
        /// </summary>
        private object CompiledExpressions { get; set; }

        /// <summary>
        /// Gets or sets a map of assemblies' names and their associated paths.
        /// </summary>
        private Dictionary<string, string> AssemblyMap { get; set; } 

        /// <summary>
        /// Creates an instance of the compiled expression class.
        /// </summary>
        /// <param name="assemblyPath">The path to the assembly containing the class.</param>
        public void CreateCompiledExpressions(string assemblyPath)
        {
            try
            {
                (new PermissionSet(PermissionState.Unrestricted)).Assert();
                CompiledExpressions = Assembly.LoadFrom(assemblyPath).CreateInstance(ExpressionCompiler.TypeName);
            }
            finally
            {
                CodeAccessPermission.RevertAssert();
            }
        }

        /// <summary>
        /// Invokes the compiled expression instance's method.
        /// </summary>
        /// <param name="method">The name of the method.</param>
        /// <returns>The result of the method's invocation.</returns>
        public object Invoke(string method)
        {
            var methodInfo = CompiledExpressions.GetType().GetMethod(method);
            return methodInfo.Invoke(CompiledExpressions, null);
        }

        /// <summary>
        /// Sets the value of the compiled expression instance's field.
        /// </summary>
        /// <param name="field">The name of the field.</param>
        /// <param name="value">The value to be set.</param>
        public void SetValue(string field, object value)
        {
            var fieldInfo = CompiledExpressions.GetType().GetField(field);
            fieldInfo.SetValue(CompiledExpressions, value);
        }

        /// <summary>
        /// Subscribes to the assembly resolve event, to allow for loading custom assemblies within the sandbox.
        /// </summary>
        /// <param name="assemblyMap">A map of assemblies' names and paths to be used when resolving assemblies.</param>
        public void InstallAssemblyResolver(Dictionary<string, string> assemblyMap)
        {
            AssemblyMap = assemblyMap;
            AppDomain.CurrentDomain.AssemblyResolve += CurrentDomain_AssemblyResolve;
        }

        /// <summary>
        /// Loads assemblies from the <see cref="AssemblyMap"/>
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="args">The assembly to resolve.</param>
        /// <returns>The assembly that resolves the assembly, or null if the assembly cannot be resolved.</returns>
        private Assembly CurrentDomain_AssemblyResolve(object sender, ResolveEventArgs args)
        {
            try
            {
                (new PermissionSet(PermissionState.Unrestricted)).Assert();

                string assemblyPath;
                if (!AssemblyMap.TryGetValue(args.Name, out assemblyPath))
                    return null;

                return Assembly.LoadFrom(assemblyPath);
            }
            finally
            {
                CodeAccessPermission.RevertAssert();
            }
        }
    }
}
