using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using IllusionScript.Runtime;
using IllusionScript.Runtime.Compiling;
using IllusionScript.Runtime.Binding;
using IllusionScript.Runtime.Interpreting.Memory.Symbols;

namespace IllusionScript.Compiler.PHP8
{
    public sealed class Compiler : CompilerConnector
    {
        public override string name => "php8";
        private string outDir;

        public override bool BuildOutput()
        {
            string baseOutput = Path.Combine(baseDir, "out", name);

            if (!Directory.Exists(baseOutput))
            {
                Directory.CreateDirectory(baseOutput);
            }

            outDir = baseOutput;

            return true;
        }

        public override bool BuildCore()
        {
            writer.WriteLine("Build Core ...");
            writer.WriteLine("Bind Syscalls");
            string syscall = Path.Combine(outDir, "syscall.php");
            File.WriteAllText(syscall, SyscallBinder());

            return true;
        }

        public override bool Build(Compilation compilation, BoundProgram program)
        {
            Dictionary<string, List<FunctionSymbol>> files = SortFunctions(compilation);

            string entryFunctionFile = "";

            foreach (KeyValuePair<string, List<FunctionSymbol>> pair in files)
            {
                if (pair.Value.Count == 0)
                {
                    continue;
                }

                writer.WriteLine($"Compile: {Path.GetFullPath(pair.Value.First().declaration.location.text.filename)}");
                string path = pair.Key;
                List<FunctionSymbol> functions = pair.Value;

                if (File.Exists(path))
                {
                    File.Delete(path);
                }

                CompilerFile file = new CompilerFile(File.Open(path, FileMode.Create));
                file.WriteHeader(files.Keys);

                foreach (FunctionSymbol function in functions)
                {
                    if (function == compilation.mainFunction || function == compilation.scriptFunction)
                    {
                        entryFunctionFile = path;
                    }

                    writer.WriteLine($"    Write: {function.name}");
                    file.Write(function, program.functionBodies,
                        function == program.mainFunction || function == program.scriptFunction);
                }

                file.Close();
            }

            string entryFilePath = Path.Combine(outDir, "index.php");
            if (File.Exists(entryFilePath))
            {
                File.Delete(entryFilePath);
            }

            entryFunctionFile = Path.GetFileName(entryFunctionFile);
            File.WriteAllText(entryFilePath, $"<?php\ninclude_once \"./{entryFunctionFile}\";");

            return true;
        }

        private Dictionary<string, List<FunctionSymbol>> SortFunctions(Compilation compilation)
        {
            Dictionary<string, List<FunctionSymbol>> sourceFiles = new Dictionary<string, List<FunctionSymbol>>();

            foreach (FunctionSymbol function in compilation.functions)
            {
                string path = Path.GetFullPath(function.declaration.location.text.filename);
                if (sourceFiles.ContainsKey(path))
                {
                    sourceFiles[path].Add(function);
                }
                else
                {
                    List<FunctionSymbol> functions = new List<FunctionSymbol> { function };
                    sourceFiles.Add(path, functions);
                }
            }

            int index = 0;

            Dictionary<string, List<FunctionSymbol>> files = new Dictionary<string, List<FunctionSymbol>>();

            foreach (KeyValuePair<string, List<FunctionSymbol>> pair in sourceFiles)
            {
                string name = $"{index:0000}_{Path.GetFileNameWithoutExtension(pair.Key)}.php";
                string path = Path.Combine(outDir, name);
                files.Add(path, new List<FunctionSymbol>(pair.Value));
                index++;
            }

            return files;
        }

        public override bool CleanUp()
        {
            return true;
        }

        public override string SyscallBinder()
        {
            Assembly assembly = Assembly.GetExecutingAssembly();
            using Stream stream = assembly.GetManifestResourceStream("IllusionScript.Compiler.PHP8.syscall.php");
            using StreamReader reader = new StreamReader(stream);
            return reader.ReadToEnd();
        }
    }
}