// -----------------------------------------------------------------------
// <copyright file="Sandbox.cs" company="Todd Aspeotis">
//  Copyright 2012, Todd Aspeotis
// </copyright>
// -----------------------------------------------------------------------

using System.Reflection;
using System.Security;
using System.Security.Permissions;

namespace ExpressionEvaluator
{
    using System;
    using System.Collections.Generic;

    /// <summary>
    /// TODO: Update summary.
    /// </summary>
    internal sealed class Sandbox : MarshalByRefObject
    {

        private object CompiledExpressions { get; set; }

        private Dictionary<string, string> AssemblyMap { get; set; } 

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

        public object Invoke(string method)
        {
            var methodInfo = CompiledExpressions.GetType().GetMethod(method);
            return methodInfo.Invoke(CompiledExpressions, null);
        }

        public void SetValue(string field, object value)
        {
            var fieldInfo = CompiledExpressions.GetType().GetField(field);
            fieldInfo.SetValue(CompiledExpressions, value);
        }

        public void InstallAssemblyResolver(Dictionary<string, string> assemblyMap)
        {
            AssemblyMap = assemblyMap;
            AppDomain.CurrentDomain.AssemblyResolve += CurrentDomain_AssemblyResolve;
        }

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
