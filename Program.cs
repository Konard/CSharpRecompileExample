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

namespace CSharpRecompileExample
{
    public static class Program
    {
        static void Main()
        {
            var typeName = typeof(Program).FullName;
            var executingAssembly = Assembly.GetExecutingAssembly();
            var syntaxTree = executingAssembly.Decompile(typeName);
            var code = syntaxTree.ToString();
            Console.WriteLine(code);
            var fileName = "LibraryName.dll";
            var emitResult = Compile(code, fileName);
            if (emitResult.Success)
            {
                var compiledAssembly = AssemblyLoadContext.Default.LoadFromAssemblyPath(Path.GetFullPath(fileName));
                Console.WriteLine(compiledAssembly.GetType(typeName).GetMethod("Add").Invoke(null, null));
            }
        }

        public static int Add() => 42;

        private static Microsoft.CodeAnalysis.Emit.EmitResult Compile(string code, string fileName)
        {
            var roslynTree = CSharpSyntaxTree.ParseText(code);
            var dotnetCoreDirectory = System.Runtime.InteropServices.RuntimeEnvironment.GetRuntimeDirectory(); // Path.GetDirectoryName(typeof(System.Object).GetTypeInfo().Assembly.Location);
            var references = Assembly.GetExecutingAssembly().GetAllReferencesPaths().ToList();
            references.Add(Path.Combine(dotnetCoreDirectory, "mscorlib.dll"));
            references.Add(Path.Combine(dotnetCoreDirectory, "netstandard.dll"));
            references.Add(Path.Combine(dotnetCoreDirectory, "System.Collections.Immutable.dll"));
            references.Add(Path.Combine(dotnetCoreDirectory, "System.Linq.dll"));
            references.Add(Path.Combine(dotnetCoreDirectory, "System.Private.Uri.dll"));
            references.Add(Path.Combine(dotnetCoreDirectory, "System.Private.CoreLib.dll"));
            references.Add(Path.Combine(dotnetCoreDirectory, "System.Runtime.dll"));
            references.Add(Path.Combine(dotnetCoreDirectory, "System.Runtime.Extensions.dll"));
            references.Add(Path.Combine(dotnetCoreDirectory, "System.Runtime.InteropServices.dll"));
            references.Add(Path.Combine(dotnetCoreDirectory, "System.Reflection.Metadata.dll"));
            var metadataReferences = references.Select(x => MetadataReference.CreateFromFile(x)).ToArray();
            var options = new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary);
            CSharpCompilation compilation = CSharpCompilation.Create("assemblyName", new[] { roslynTree }, metadataReferences, options);
            return compilation.Emit(fileName);
        }

        public static string GetPath(this Assembly assembly)
        {
            string codeBase = assembly.CodeBase;
            UriBuilder uri = new UriBuilder(codeBase);
            string path = Uri.UnescapeDataString(uri.Path);
            return path;
        }

        public static ICSharpCode.Decompiler.CSharp.Syntax.SyntaxTree Decompile(this Assembly assembly, string typeName)
        {
            string path = GetPath(assembly);
            var settings = new DecompilerSettings();
            var decompiler = new CSharpDecompiler(path, settings);
            var name = new FullTypeName(typeName);
            var syntaxTree = decompiler.DecompileType(name);
            return syntaxTree;
        }

        public static ICSharpCode.Decompiler.CSharp.Syntax.SyntaxTree Decompile(this Assembly assembly, string typeName, string methodName)
        {
            string path = GetPath(assembly);
            var settings = new DecompilerSettings();
            var decompiler = new CSharpDecompiler(path, settings);
            var name = new FullTypeName(typeName);
            ITypeDefinition typeInfo = decompiler.TypeSystem.MainModule.Compilation.FindType(name).GetDefinition();
            var tokenOfFirstMethod = typeInfo.Methods.Where(x => x.Name == methodName).Single().MetadataToken;
            var syntaxTree = decompiler.Decompile(tokenOfFirstMethod);
            return syntaxTree;
        }

        public static IEnumerable<string> GetAllReferencesPaths(this Assembly assembly)
        {
            foreach (var assemblyName in assembly.GetReferencedAssemblies())
            {
                yield return Assembly.Load(assemblyName).GetPath();
            }
        }
    }
}
