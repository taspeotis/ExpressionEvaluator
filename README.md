Expression Evaluator is a library to help developers evaluate C# and VB .NET expressions. The expressions you need to evaluate are compiled through the .NET Framework's own CodeDOM so nearly all language features are supported. The library can expose remotable objects to the expressions for a scripting-like capability. All expression evaluation is sandboxed.

Expression Evaluator is written in C#.

# History and Errata

Expression Evaluator was written to help parse Visual Basic expressions in SQL Server Reporting Services reports. In particular, the objective was to produce a library that would produce the same output as Microsoft's own report server. To this effect the library automatically adds imports and references to some assemblies and types. The relevant section of the RDL Specification is reproduced below:

> ## Built-in References
 
> Expressions may reference types from the following assemblies: `Microsoft.VisualBasic.dll`, `System.dll`, `mscorlib.dll` and `Microsoft.SqlServer.Types.dll`.

> For convenience, the following namespaces are imported: `Microsoft.VisualBasic`, `System` and `Microsoft.SqlServer.Types`.

> In addition, the following types are imported: `System.Convert` and `System.Math`.

In the interests of producing a generic evaluator `Microsoft.SqlServer.Types` is neither imported nor referenced. Further, the library is also capable of evaluating any expression that has a `CodeDomProvider` available for it - not just VB .NET.

Expression Evaluator's API allows for general purpose expression evaluation, but suited towards bulk evaluation<sup>1</sup>. To use Expression Evaluator efficiently it's required that all expressions are added first, and then compiled before being evaluated.

<sup>1</sup> This design was chosen because all possible expressions are known up-front in a Reporting Services report.

# Example

```csharp
static void Main(string[] args)
{
    var expressions = new List<string>
                            {
                                "3 * 5",
                                "Log10(50)",
                                "Parameters!Greeting + \" World!\""
                            };

    // An ExpressionMeta contains the expressions and extensions to be compiled.
    var meta = new ExpressionMeta("VisualBasic");

    // Add the expressions to be compiled.
    foreach(var expression in expressions)
        meta.AddExpression(expression);

    // Add the extensions to be compiled.
    var extension = new Dictionary<string, string> {{"Greeting", "Hello"}};
    meta.AddExtensionIgnoreAssembly(new Extension("Parameters", extension));

    // Compile the expressions
    using(var evaluator = meta.Compile())
    {
        // Evaluate the expression
        foreach(var expression in expressions)
            Console.WriteLine("{0}", evaluator.Evaluate(expression));
    }
}
```

Output:

```
15
1.69897000433602
Hello World!
```