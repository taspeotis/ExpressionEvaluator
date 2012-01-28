// -----------------------------------------------------------------------
// <copyright file="EvaluatorCompiler.cs" company="">
//  Copyright 2012, Todd Aspeotis
// </copyright>
// -----------------------------------------------------------------------

using System.CodeDom;
using System.CodeDom.Compiler;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Security;
using System.Security.Permissions;
using System.Security.Policy;

namespace ExpressionEvaluator
{
    using System;
    using System.Collections.Generic;
    using System.Security.Cryptography;
    using System.Linq;
    using System.Text;

    /// <summary>
    /// TODO: Update summary.
    /// </summary>
    internal static class ExpressionCompiler
    {

        private static readonly IEnumerable<string> DefaultImports =
            new List<string> {"System"};

        private static readonly Dictionary<string, string[]> DefaultLanguageImports =
            new Dictionary<string, string[]>(StringComparer.InvariantCultureIgnoreCase)
                {{"VisualBasic", new[] {"Microsoft.VisualBasisc", "System.Convert", "System.Math"}}};

        // TODO: UserImports (in Meta)

        private static readonly IEnumerable<string> DefaultReferences =
            new List<string> {"System.Core.dll"};

        private static readonly Dictionary<string, string[]> DefaultLanguageReferences =
            new Dictionary<string, string[]>(StringComparer.InvariantCultureIgnoreCase)
                {
                    {"CSharp", new[] {"Microsoft.CSharp.dll"}},
                    {"VisualBasic", new[] {"Microsoft.VisualBasic.dll"}}
                };

        // TODO: UserReferences (in Meta)

        public static Evaluator Compile(ExpressionMeta meta)
        {
            if (meta == null) throw new ArgumentNullException("meta");

            string assemblyPath = null;
            AppDomain appDomain = null;

            try
            {
                assemblyPath = CreateAssembly(meta);
                appDomain = CreateAppDomain(assemblyPath);
                var sandbox = CreateSandbox(appDomain, assemblyPath, meta.Extensions);

                return new Evaluator(appDomain, sandbox, assemblyPath);
            }
            catch (Exception)
            {
                if (appDomain != null)
                    AppDomain.Unload(appDomain);

                if (assemblyPath != null)
                    File.Delete(assemblyPath);

                throw;
            }
        }

        private static string CreateAssembly(ExpressionMeta meta)
        {
            var model = CreateModel(meta);
            var options = new CompilerParameters();
            options.GenerateExecutable = false;
            options.GenerateInMemory = false;

            using(var provider = CodeDomProvider.CreateProvider(meta.Language))
            {
                // Compile the assembly
                var results = provider.CompileAssemblyFromDom(options, model);

                // ...or not
                if (results.Errors.HasErrors)
                    throw new InvalidOperationException(results.Errors[0].ToString());

                return results.PathToAssembly;
            }
        }

        private static CodeCompileUnit CreateModel(ExpressionMeta meta)
        {
            // Class
            var modelClass = new CodeTypeDeclaration();
            modelClass.Name = ClassName;
            modelClass.TypeAttributes = TypeAttributes.Public | TypeAttributes.Class;

            // Class methods
            foreach (var expression in meta.Expressions)
            {
                var modelExpression = new CodeSnippetExpression();
                modelExpression.Value = expression;

                var modelReturn = new CodeMethodReturnStatement();
                modelReturn.Expression = modelExpression;

                var modelMethod = new CodeMemberMethod();
                modelMethod.Name = GetExpressionMethodName(expression);
                modelMethod.Attributes = MemberAttributes.Public;
                modelMethod.ReturnType = new CodeTypeReference(typeof(object));
                modelMethod.Statements.Add(modelReturn);
                modelClass.Members.Add(modelMethod);
            }

            // Class fields
            foreach (var extension in meta.Extensions)
            {
                var modelField = new CodeMemberField();
                modelField.Name = extension.Name;
                modelField.Attributes = MemberAttributes.Public;
                modelField.Type = new CodeTypeReference(extension.GetExtensionType()); // Need to add this as an extra reference
                modelClass.Members.Add(modelField);
            }

            // Namespace
            var modelNamespace = new CodeNamespace();
            modelNamespace.Name = NamespaceName;
            modelNamespace.Types.Add(modelClass);

            // Namespace Imports
            foreach (var import in GetImports(meta).Distinct())
                modelNamespace.Imports.Add(new CodeNamespaceImport(import));

            // Assembly
            var modelAssembly = new CodeCompileUnit();
            modelAssembly.Namespaces.Add(modelNamespace);

            // Assembly References
            foreach (var reference in GetReferences(meta).Distinct())
                modelAssembly.ReferencedAssemblies.Add(reference);

            return modelAssembly;
        }

        private static IEnumerable<string> GetImports(ExpressionMeta meta)
        {
            foreach (var import in DefaultImports)
                yield return import;

            string[] imports;
            if (DefaultLanguageImports.TryGetValue(meta.Language, out imports))
                foreach (var import in imports)
                    yield return import;

            foreach (var import in meta.Imports)
                yield return import;
        }

        private static IEnumerable<string> GetReferences(ExpressionMeta meta)
        {
            foreach (var reference in DefaultReferences)
                yield return reference;

            string[] references;
            if (DefaultLanguageReferences.TryGetValue(meta.Language, out references))
                foreach (var reference in references)
                    yield return reference;

            foreach (var reference in meta.References)
                yield return reference;

            foreach (var type in meta.Extensions.Select(e => e.GetExtensionType()))
                yield return type.Assembly.ManifestModule.FullyQualifiedName;
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="assemblyPath"></param>
        /// <returns></returns>
        /// <remarks>http://msdn.microsoft.com/en-us/library/bb763046.aspx</remarks>
        private static AppDomain CreateAppDomain(string assemblyPath)
        {
            var appDomainSetup = new AppDomainSetup();
            appDomainSetup.ApplicationBase = Path.GetPathRoot(assemblyPath);

            var permissionSet = new PermissionSet(PermissionState.None);
            permissionSet.AddPermission(new SecurityPermission(SecurityPermissionFlag.Execution));

            var strongNameEvidence = typeof (Sandbox).Assembly.Evidence.GetHostEvidence<StrongName>();

            return AppDomain.CreateDomain("ExpressionEvaluatorAppDomain", null, appDomainSetup, permissionSet,
                                          strongNameEvidence);
        }

        private static Sandbox CreateSandbox(AppDomain appDomain, string assemblyPath, ExtensionCollection extensions)
        {
            var sandboxType = typeof(Sandbox);
            var sandboxAssemblyPath = sandboxType.Assembly.ManifestModule.FullyQualifiedName;
            var typeName = sandboxType.FullName;

            Debug.Assert(typeName != null, "typeName != null");
            var sandbox = (Sandbox) Activator.CreateInstanceFrom(appDomain, sandboxAssemblyPath, typeName).Unwrap();

            var assemblyMap = new Dictionary<string, string>();
            foreach (var type in extensions.Select(e => e.GetExtensionType()))
                assemblyMap[type.Assembly.FullName] = type.Assembly.ManifestModule.FullyQualifiedName;

            sandbox.InstallAssemblyResolver(assemblyMap);
            sandbox.CreateCompiledExpressions(assemblyPath);

            // Extensions
            foreach(var extension in extensions)
                sandbox.SetValue(extension.Name, extension.Instance);

            return sandbox;
        }

        internal static string NamespaceName = "CompiledExpressions";

        internal static string ClassName = "CompiledExpressions";

        internal static string TypeName = "CompiledExpressions.CompiledExpressions";

        private static readonly HashAlgorithm HashAlgorithm = new SHA1CryptoServiceProvider();

        public static string GetExpressionMethodName(string expression)
        {
            if (expression == null) throw new ArgumentNullException("expression");
            if (String.IsNullOrWhiteSpace(expression)) throw new ArgumentOutOfRangeException("expression");

            var input = Encoding.Default.GetBytes(expression);
            byte[] hashbytes;

            lock (HashAlgorithm)
                hashbytes = HashAlgorithm.ComputeHash(input);
            
            var hashstring = BitConverter.ToString(hashbytes).Replace("-", "");
            return String.Format("Method{0}", hashstring);
        }
    }
}
