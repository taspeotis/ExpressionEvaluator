namespace ExpressionEvaluator
{
    using System;
    using System.CodeDom;
    using System.CodeDom.Compiler;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.IO;
    using System.Reflection;
    using System.Security.Cryptography;
    using System.Text;

    public sealed class ExpressionEvaluator : IDisposable
    {
        private static readonly Dictionary<string, string> DefaultLateBindingObjectTypeNames =
            new Dictionary<string, string>(StringComparer.InvariantCultureIgnoreCase)
                {
                    {"CSharp", "dynamic"}
                };

        private static readonly IEnumerable<string> DefaultImports =
            new List<string> { "System" };

        // TODO: Make this a property that automatically filters based on language
        private static readonly Dictionary<string, string[]> DefaultLanguageImports =
            new Dictionary<string, string[]>(StringComparer.InvariantCultureIgnoreCase)
                {{"VisualBasic", new[] {"Microsoft.VisualBasic", "System.Convert", "System.Math"}}};

        // TODO: UserImports

        private static readonly IEnumerable<string> DefaultReferences =
            new List<string> {"System.Core.dll"};

        // TODO: Make this a property that automatically filters based on language
        private static readonly Dictionary<string, string[]> DefaultLanguageReferences =
            new Dictionary<string, string[]>(StringComparer.InvariantCultureIgnoreCase)
                {{"CSharp", new[] {"Microsoft.CSharp.dll"}}};


        // TODO: UserReferences

        public ExpressionEvaluator(string language = "CSharp", string lateBindingObjectTypeName = null)
        {
            if (language == null)
                throw new ArgumentNullException("language");

            Language = language;

            if (lateBindingObjectTypeName == null)
            {
                if (!DefaultLateBindingObjectTypeNames.TryGetValue(language, out lateBindingObjectTypeName))
                    lateBindingObjectTypeName = "System.Object";
            }

            LateBindingObjectTypeName = lateBindingObjectTypeName;

            Provider = CodeDomProvider.CreateProvider(language);
            Expressions = new Collection<string>();
            HostObjects = new Dictionary<string, dynamic>(ProviderIdentifierStringComparer);
            IsDisposed = false;
            Hasher = new SHA1CryptoServiceProvider();
        }

        ~ExpressionEvaluator()
        {
            Dispose(false);
        }

        public ICollection<string> Expressions { get; private set; }

        public IDictionary<string, dynamic> HostObjects { get; private set; }

        public bool IsDisposed { get; private set; }

        public string Language { get; private set; }

        private CodeDomProvider Provider { get; set; }

        private object ExpressionObject { get; set; }

        public StringComparer ProviderIdentifierStringComparer
        {
            get
            {
                return Provider.LanguageOptions.HasFlag(LanguageOptions.CaseInsensitive)
                    ? StringComparer.InvariantCultureIgnoreCase : StringComparer.InvariantCulture;
            }
        }

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
                Provider.Dispose();
            }

            IsDisposed = true;
        }

        private HashAlgorithm Hasher { get; set; }

        private string GetExpressionMethodName(string expression)
        {
            var inputbytes = Encoding.Default.GetBytes(expression);
            var hashbytes = Hasher.ComputeHash(inputbytes);
            var hashstring = BitConverter.ToString(hashbytes);
            return String.Format("Method{0}", hashstring.Replace("-", ""));
        }

        public void Compile()
        {
            // Create assembly
            Assembly expressionAssembly = CreateAssembly();
            
            // Create object
            ExpressionObject = expressionAssembly.CreateInstance(TypeName);

            if (ExpressionObject == null)
                throw new Exception("Unable to instantiate compiled expressions.");

            // Assign host objects
            foreach(var kvp in HostObjects)
            {
                var fieldInfo = ExpressionObject.GetType().GetField(kvp.Key);
                if (fieldInfo != null)
                    fieldInfo.SetValue(ExpressionObject, kvp.Value);
            }
        }

        public object Evaluate(string expression)
        {
            if (ExpressionObject == null)
                throw new InvalidOperationException("Compile has not been called successfully.");

            var methodName = GetExpressionMethodName(expression);
            var methodInfo = ExpressionObject.GetType().GetMethod(methodName);

            if (methodInfo == null)
                throw new InvalidOperationException("This expression has not been compiled.");

            return methodInfo.Invoke(ExpressionObject, null);
        }

        private Assembly CreateAssembly()
        {
            // Get DOM
            var model = CreateModel();

            // Compile
            using (var sw = new StreamWriter(@"C:\Temp\Output.vb"))
            {
                var cgo = new CodeGeneratorOptions();                
                var itw = new IndentedTextWriter(sw, "    ");
                Provider.GenerateCodeFromCompileUnit(model, itw, cgo);
            }

            var options = new CompilerParameters {GenerateInMemory = true, GenerateExecutable = false};        
            var results = Provider.CompileAssemblyFromDom(options, model);

            // ...or don't compile
            if (results.Errors.HasErrors)
                throw new Exception(results.Errors[0].ToString());

            // Assembly
            return results.CompiledAssembly;
        }

        private CodeCompileUnit CreateModel()
        {
            // Class
            var modelClass = new CodeTypeDeclaration();
            modelClass.Name = ClassName;
            modelClass.TypeAttributes = TypeAttributes.Class | TypeAttributes.Public;

            // Class Methods
            foreach(var expression in Expressions)
            {
                if (String.IsNullOrWhiteSpace(expression))
                    continue;

                var modelExpression = new CodeSnippetExpression(expression);
                var modelReturnStatement = new CodeMethodReturnStatement(modelExpression);
                var modelMethod = new CodeMemberMethod();
                modelMethod.Name = GetExpressionMethodName(expression);
                modelMethod.Attributes = MemberAttributes.Public;
                modelMethod.ReturnType = new CodeTypeReference("System.Object");
                modelMethod.Statements.Add(modelReturnStatement);
                modelClass.Members.Add(modelMethod);
            }

            // Class Members
            foreach(var kvp in HostObjects)
            {
                if (String.IsNullOrWhiteSpace(kvp.Key))
                    continue;

                var modelField = new CodeMemberField();
                modelField.Name = kvp.Key;
                modelField.Type = new CodeTypeReference(LateBindingObjectTypeName);
                modelField.Attributes = MemberAttributes.Public;
                modelClass.Members.Add(modelField);
            }

            // Namespace
            var modelNamespace = new CodeNamespace();
            modelNamespace.Name = NamespaceName;

            foreach(var import in DefaultImports)
                modelNamespace.Imports.Add(new CodeNamespaceImport(import));

            foreach(var kvp in DefaultLanguageImports)
                if (StringComparer.InvariantCultureIgnoreCase.Equals(kvp.Key, Language))
                    foreach(var import in kvp.Value)
                        modelNamespace.Imports.Add(new CodeNamespaceImport(import));

            modelNamespace.Types.Add(modelClass);

            // Model
            var model = new CodeCompileUnit();

            foreach (var reference in DefaultReferences)
                model.ReferencedAssemblies.Add(reference);

            foreach (var kvp in DefaultLanguageReferences)
                if (StringComparer.InvariantCultureIgnoreCase.Equals(kvp.Key, Language))
                    foreach (var reference in kvp.Value)
                        model.ReferencedAssemblies.Add(reference);
            
            model.Namespaces.Add(modelNamespace);
            return model;
        }

        private const string NamespaceName = "CompiledExpressions";

        private const string ClassName = "CompiledExpressions";

        private static string TypeName
        {
            get { return String.Format("{0}.{1}", NamespaceName, ClassName); }
        }

        public static object EvaluateAdHoc(string expression, string language = "CSharp")
        {
            if (String.IsNullOrWhiteSpace(expression))
                return null;

            var expressionEvaluator = new ExpressionEvaluator(language);
            expressionEvaluator.Expressions.Add(expression);
            expressionEvaluator.Compile();
            return expressionEvaluator.Evaluate(expression);
        }

        public string LateBindingObjectTypeName { get; private set; }
    }
}
