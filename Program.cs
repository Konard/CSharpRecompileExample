using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Loader;
using ICSharpCode.Decompiler;
using ICSharpCode.Decompiler.CSharp;
using ICSharpCode.Decompiler.TypeSystem;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace CecilExample
{
    class Program
    {
        static void Main(string[] args)
        {
            GetAllReferencesPaths();

            ICSharpCode.Decompiler.CSharp.Syntax.SyntaxTree syntaxTree = GetSyntaxTreeOfThisClass();

            //var astBuilder = new AstBuilder

            // AstBuilder.AddMethod()

            //ICSharpCode.Decompiler.

            var code = syntaxTree;

            Console.WriteLine(syntaxTree);

            var roslynTree = CSharpSyntaxTree.ParseText(code.ToString());

            var dotnetCoreDirectory = System.Runtime.InteropServices.RuntimeEnvironment.GetRuntimeDirectory(); // Path.GetDirectoryName(typeof(System.Object).GetTypeInfo().Assembly.Location);

            CSharpCompilation compilation = CSharpCompilation.Create(
                "assemblyName",
                new[] { roslynTree },
                //GetAllReferencesPaths().Select(x => MetadataReference.CreateFromFile(x)).ToArray(),
                new[]
                {
                    MetadataReference.CreateFromFile(typeof(object).Assembly.Location),
                    MetadataReference.CreateFromFile(typeof(Console).Assembly.Location),
                    MetadataReference.CreateFromFile(typeof(MetadataReference).Assembly.Location),
                    MetadataReference.CreateFromFile(Path.Combine(dotnetCoreDirectory, "mscorlib.dll")),
                    MetadataReference.CreateFromFile(Path.Combine(dotnetCoreDirectory, "netstandard.dll")),
                    MetadataReference.CreateFromFile(Path.Combine(dotnetCoreDirectory, "System.Collections.Immutable.dll")),
                    MetadataReference.CreateFromFile(Path.Combine(dotnetCoreDirectory, "System.Runtime.dll")),
                    MetadataReference.CreateFromFile(Path.Combine(dotnetCoreDirectory, "System.Runtime.Extensions.dll")),
                    MetadataReference.CreateFromFile(Path.Combine(dotnetCoreDirectory, "System.Reflection.Metadata.dll")),
                    MetadataReference.CreateFromFile(Path.Combine(dotnetCoreDirectory, "System.Private.Uri.dll")),
                    MetadataReference.CreateFromFile(Path.Combine(dotnetCoreDirectory, "System.Linq.dll")),
                    MetadataReference.CreateFromFile(Path.Combine(dotnetCoreDirectory, "System.Runtime.InteropServices.dll")),
                    MetadataReference.CreateFromFile(typeof(ICSharpCode.Decompiler.CSharp.Syntax.SyntaxTree).Assembly.Location),
                    MetadataReference.CreateFromFile(typeof(CSharpSyntaxTree).Assembly.Location),
                },
                new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

            var fileName = "LibraryName.dll";
            var emitResult = compilation.Emit(fileName);
            if (emitResult.Success)
            {
                var assembly = AssemblyLoadContext.Default.LoadFromAssemblyPath(Path.GetFullPath(fileName));

                Console.WriteLine(assembly.GetType(GetThisTypeName()).GetMethod("Add").Invoke(null, null));
            }
        }

        public static int Add()
        {
            return 42;
        }

        private static ICSharpCode.Decompiler.CSharp.Syntax.SyntaxTree GetSyntaxTreeOfThisClass()
        {
            string path = GetPath(Assembly.GetExecutingAssembly());

            //var assemblyDefinition = AssemblyDefinition.ReadAssembly(path);

            //var type = assemblyDefinition.MainModule.GetType($"{nameof(CecilExample)}.{nameof(Program)}");

            //var method = type.Methods.Where(x => x.Name == nameof(Main)).Single();

            var settings = new DecompilerSettings();

            var decompiler = new CSharpDecompiler(path, settings);

            var name = new FullTypeName(GetThisTypeName());
            var syntaxTree = decompiler.DecompileType(name);
            return syntaxTree;
        }

        private static string GetThisTypeName()
        {
            return $"{nameof(CecilExample)}.{nameof(Program)}";
        }

        private static ICSharpCode.Decompiler.CSharp.Syntax.SyntaxTree GetSyntaxTreeOfThisMethod()
        {
            string path = GetPath(Assembly.GetExecutingAssembly());

            //var assemblyDefinition = AssemblyDefinition.ReadAssembly(path);

            //var type = assemblyDefinition.MainModule.GetType($"{nameof(CecilExample)}.{nameof(Program)}");

            //var method = type.Methods.Where(x => x.Name == nameof(Main)).Single();

            var settings = new DecompilerSettings();

            var decompiler = new CSharpDecompiler(path, settings);

            var name = new FullTypeName($"{nameof(CecilExample)}.{nameof(Program)}");
            ITypeDefinition typeInfo = decompiler.TypeSystem.MainModule.Compilation.FindType(name).GetDefinition();
            var tokenOfFirstMethod = typeInfo.Methods.Where(x => x.Name == nameof(Main)).Single().MetadataToken;

            var syntaxTree = decompiler.Decompile(tokenOfFirstMethod);
            return syntaxTree;
        }

        private static string GetPath(Assembly assembly)
        {
            string codeBase = assembly.CodeBase;
            UriBuilder uri = new UriBuilder(codeBase);
            string path = Uri.UnescapeDataString(uri.Path);
            return path;
        }

        private static IEnumerable<string> GetAllReferencesPaths()
        {
            foreach (var assemblyName in Assembly.GetExecutingAssembly().GetReferencedAssemblies())
            {
                yield return GetPath(Assembly.Load(assemblyName));
            }
        }
    }
}
