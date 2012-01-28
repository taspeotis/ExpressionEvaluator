// -----------------------------------------------------------------------
// <copyright file="ExpressionCompiler.cs" company="Todd Aspeotis">
//  Copyright 2012 Todd Aspeotis
// </copyright>
// -----------------------------------------------------------------------

namespace ExpressionEvaluator
{
    using System;
    using System.CodeDom;
    using System.CodeDom.Compiler;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.IO;
    using System.Linq;
    using System.Reflection;
    using System.Security;
    using System.Security.Cryptography;
    using System.Security.Permissions;
    using System.Security.Policy;
    using System.Text;

    /// <summary>
    /// Provides functionality to compile expression metadata into an assembly.
    /// </summary>
    internal static class ExpressionCompiler
    {
        /// <summary>
        /// The name of the compiled assembly's compiled expression namespace.
        /// </summary>
        internal static readonly string NamespaceName = "CompiledExpressions";

        /// <summary>
        /// The name of the compiled assembly's compiled expression class.
        /// </summary>
        internal static readonly string ClassName = "CompiledExpressions";

        /// <summary>
        /// The name of the compiled assembly's compiled expression type.
        /// </summary>
        internal static readonly string TypeName = String.Format("{0}.{1}", NamespaceName, ClassName);

        /// <summary>
        /// The hash algorithm used in converting expressions to method names.
        /// </summary>
        /// <seealso cref="GetExpressionMethodName"/>
        private static readonly HashAlgorithm HashAlgorithm = new SHA1CryptoServiceProvider();

        /// <summary>
        /// A list of default namespace imports for the output assembly.
        /// </summary>
        private static readonly IEnumerable<string> DefaultImports =
            new List<string> {"System"};

        /// <summary>
        /// A list of default namespace imports for the output assembly for a given language.
        /// </summary>
        private static readonly Dictionary<string, string[]> DefaultLanguageImports =
            new Dictionary<string, string[]>(StringComparer.InvariantCultureIgnoreCase)
                {
                    {"VisualBasic", new[] {"Microsoft.VisualBasic", "System.Convert", "System.Math"}}
                };

        /// <summary>
        /// A list of default assembly references for the output assembly.
        /// </summary>
        private static readonly IEnumerable<string> DefaultReferences =
            new List<string> {"System.Core.dll"};

        /// <summary>
        /// A list of default assembly references for the output assembly for a given language.
        /// </summary>
        private static readonly Dictionary<string, string[]> DefaultLanguageReferences =
            new Dictionary<string, string[]>(StringComparer.InvariantCultureIgnoreCase)
                {
                    {"CSharp", new[] {"Microsoft.CSharp.dll"}},
                    {"VisualBasic", new[] {"Microsoft.VisualBasic.dll"}}
                };

        /// <summary>
        /// Converts an expression into a method name.
        /// This value can then be used to create or invoke a method that represents the compiled expression.
        /// </summary>
        /// <param name="expression">The expression to convert.</param>
        /// <returns>A string representating the method name of the expression.</returns>
        public static string GetExpressionMethodName(string expression)
        {
            if (expression == null) throw new ArgumentNullException("expression");
            if (String.IsNullOrWhiteSpace(expression)) throw new ArgumentOutOfRangeException("expression");

            var input = Encoding.Default.GetBytes(expression);
            byte[] hashbytes;

            lock (HashAlgorithm)
                hashbytes = HashAlgorithm.ComputeHash(input);

            var hashstring = BitConverter.ToString(hashbytes).Replace("-", String.Empty);
            return String.Format("Method{0}", hashstring);
        }

        /// <summary>
        /// Compiles expression metadata into an assembly. The assembly is then wrapped by an <see cref="Evaluator"/>.
        /// </summary>
        /// <param name="meta">The expression metadata to compile.</param>
        /// <returns>An <see cref="Evaluator"/> capable of evaluating the expressions.</returns>
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

        /// <summary>
        /// Creates the compiled expressions assembly.
        /// </summary>
        /// <param name="meta">The expression metadata to compile into an assembly.</param>
        /// <returns>The path of the compiled expression assembly.</returns>
        /// <remarks>The caller is responsible for deleting the compiled assembly.</remarks>
        private static string CreateAssembly(ExpressionMeta meta)
        {
            var model = CreateModel(meta);
            var options = new CompilerParameters();
            options.GenerateExecutable = false;
            options.GenerateInMemory = false;

            using (var provider = CodeDomProvider.CreateProvider(meta.Language))
            {
                // Compile the assembly
                var results = provider.CompileAssemblyFromDom(options, model);

                // ...or not
                if (results.Errors.HasErrors)
                    throw new InvalidOperationException(results.Errors[0].ToString());

                return results.PathToAssembly;
            }
        }

        /// <summary>
        /// Creates an abstract model of the compiled expression assembly.
        /// </summary>
        /// <param name="meta">The expression metadata for which to create an abstract model for.</param>
        /// <returns>An abstract model, suitable for compiling with CodeDom.</returns>
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

        /// <summary>
        /// Gets namespace imports for the expression metadata based on its language.
        /// </summary>
        /// <param name="meta">The expression metadata for which to get namespace imports.</param>
        /// <returns>All relevant namespace imports.</returns>
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

        /// <summary>
        /// Gets assembly references for the expression metadata based on its language.
        /// </summary>
        /// <param name="meta">The expression metadata for which to get references.</param>
        /// <returns>All relevant assembly references.</returns>
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
        /// Creates a sandbox application domain, with minimum permissions.
        /// </summary>
        /// <param name="assemblyPath">
        /// Path to the compiled expression assembly.
        /// This will be used to calculate an application base for the app domain.
        /// </param>
        /// <returns>An application domain with minimum permissions.</returns>
        /// <remarks>
        /// See http://msdn.microsoft.com/en-us/library/bb763046.aspx for more information about this implementation.
        /// The caller is responsible for unloading the application domain in advance of deleting the compiled expressions assembly.
        /// </remarks>
        private static AppDomain CreateAppDomain(string assemblyPath)
        {
            var appDomainSetup = new AppDomainSetup();
            appDomainSetup.ApplicationBase = Path.GetPathRoot(assemblyPath);

            var permissionSet = new PermissionSet(PermissionState.None);
            permissionSet.AddPermission(new SecurityPermission(SecurityPermissionFlag.Execution));

            var strongNameEvidence = typeof(Sandbox).Assembly.Evidence.GetHostEvidence<StrongName>();

            return AppDomain.CreateDomain("ExpressionEvaluatorAppDomain", null, appDomainSetup, permissionSet, strongNameEvidence);
        }

        /// <summary>
        /// Creates a <see cref="Sandbox"/> object, compiled expressions and configures extensions inside the sandbox application domain.
        /// </summary>
        /// <param name="appDomain">The sandbox application domain.</param>
        /// <param name="assemblyPath">The compiled expressions assembly path.</param>
        /// <param name="extensions">The extensions to initialize the compiled expression object with.</param>
        /// <returns>A remote <see cref="Sandbox"/> object.</returns>
        private static Sandbox CreateSandbox(AppDomain appDomain, string assemblyPath, ExtensionCollection extensions)
        {
            var sandboxType = typeof(Sandbox);
            var sandboxAssemblyPath = sandboxType.Assembly.ManifestModule.FullyQualifiedName;
            var typeName = sandboxType.FullName;

            Debug.Assert(typeName != null, "typeName != null");
            var sandbox = (Sandbox)Activator.CreateInstanceFrom(appDomain, sandboxAssemblyPath, typeName).Unwrap();

            var assemblyMap = new Dictionary<string, string>();
            foreach (var type in extensions.Select(e => e.GetExtensionType()))
                assemblyMap[type.Assembly.FullName] = type.Assembly.ManifestModule.FullyQualifiedName;

            sandbox.InstallAssemblyResolver(assemblyMap);
            sandbox.CreateCompiledExpressions(assemblyPath);

            // Extensions
            foreach (var extension in extensions)
                sandbox.SetValue(extension.Name, extension.Instance);

            return sandbox;
        }
    }
}
