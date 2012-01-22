ExpressionEvaluator is a library to help developers evaluate C# and VB .NET expressions. The expressions you need to evaluate are compiled through the .NET Framework's own CodeDOM so nearly all language features are supported. The library exposes late-bound host objects to the expressions that add a scripting-like capability.

ExpressionEvaluator is written in C#.

# History and Errata

ExpressionEvaluator was written to help parse Visual Basic expressions in SQL Server Reporting Services reports. In particular, the objective was to produce a library that would produce the same output as Microsoft's own report server. To this effect the library automatically adds imports and references to some assemblies and types. The relevant section of the RDL Specification is reproduced below:

> ## Built-in References
 
> Expressions may reference types from the following assemblies: `Microsoft.VisualBasic.dll`, `System.dll`, `mscorlib.dll` and `Microsoft.SqlServer.Types.dll`.

> For convenience, the following namespaces are imported: `Microsoft.VisualBasic`, `System` and `Microsoft.SqlServer.Types`.

> In addition, the following types are imported: `System.Convert` and `System.Math`.

In the interests of producing a generic evaluator `Microsoft.SqlServer.Types` is not imported or referenced. Further, the library is also capable of evaluating any expression that has a `CodeDomProvider` available for it - not just VB .NET<sup>1</sup>.

ExpressionEvaluator's API allows for general purpose expression evaluation, but suited towards bulk evaluation<sup>2</sup>. To use ExpressionEvaluator efficiently it's required that all expressions are added first, and then compiled before being evaluated.

Currently expressions are compiled and executed within the context of the calling assembly; sandboxing expression execution is planned for a later release.

<sup>1</sup> In fact, the default language for the library is C#.

<sup>2</sup> This design was chosen because all possible expressions are known up-front in a Reporting Services report.

# Example

    class MyObject
    {
        public string Phrase = "Hello";
    }

    class Program
    {
        static IEnumerable<string> Expressions =
            new List<string>
            {
                "2 * 5",
                "Log10(50)",
                "HostObject.Phrase",
                "\"World\""
            };

        static void Main(string[] args)
        {
            using (var evaluator = new ExpressionEvaluator("VisualBasic"))
            {
                // Configure out host object for the expressions to reference.
                var hostObject = new MyObject();
                evaluator.HostObjects.Add("HostObject", hostObject);

                // Add each expression that needs to be compiled.
                foreach (var expression in Expressions)
                    evaluator.Expressions.Add(expression);

                // Compile each expression. This will also "associate" any host objects.
                evaluator.Compile();

                // Evaluate the expressions.
                foreach (var expression in Expressions)
                    Console.WriteLine(evaluator.Evaluate(expression).ToString());

                // Even though expressions are compiled, they can still be dynamic.
                hostObject.Phrase = "!";
                Console.WriteLine(evaluator.Evaluate("HostObject.Phrase"));

                // If you have simple requirements, you can also perform ad-hoc evaluation.
                Console.WriteLine(ExpressionEvaluator.EvaluateAdHoc("\"But it might be slower\""));
            }
        }
    }

Output:

    10
    1.69897000433602
    Hello
    World
    !
    But it might be slower