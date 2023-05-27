using System.Collections.Generic;
using System.IO;
using System.Linq;
using ILAwake.Runtime;
using Mono.Cecil;
using Mono.Cecil.Cil;
using Unity.CompilationPipeline.Common.Diagnostics;
using Unity.CompilationPipeline.Common.ILPostProcessing;
using UnityEngine;

namespace ILAwake.Editor
{
    public class IlAwakePostProcessor : ILPostProcessor
    {
        public override ILPostProcessor GetInstance()
        {
            return this;
        }

        public override bool WillProcess(ICompiledAssembly compiledAssembly)
        {
            return !compiledAssembly.Name.StartsWith("Unity");
        }

        public override ILPostProcessResult Process(ICompiledAssembly compiledAssembly)
        {
            var assemblyDefinition = LoadAssemblyDefinition(compiledAssembly);
            var messages = PostProcessAssembly(assemblyDefinition);
            var pe = new MemoryStream();
            var pdb = new MemoryStream();
            var writerParameters = new WriterParameters
            {
                SymbolWriterProvider = new PortablePdbWriterProvider(),
                SymbolStream = pdb,
                WriteSymbols = true
            };

            assemblyDefinition.Write(pe, writerParameters);

            return new ILPostProcessResult(new InMemoryAssembly(pe.ToArray(), pdb.ToArray()), messages);
        }

        private static List<DiagnosticMessage> PostProcessAssembly(AssemblyDefinition assemblyDefinition)
        {
            var messages = new List<DiagnosticMessage>();
            CodeGenerator.GenerateCodeForAttributeName(assemblyDefinition,

                new MethodPair(nameof(AwakeGet), "Awake",
                    nameof(MonoBehaviour.GetComponent),
                    nameof(MonoBehaviour.GetComponents), messages, typeof(UnityEngine.MonoBehaviour),
                    false),

                 new MethodPair(nameof(AwakeFind), "Awake",
                    nameof(Object.FindObjectOfType),
                    nameof(Object.FindObjectsOfType), messages, typeof(UnityEngine.Object),
                    true),
                
                new MethodPair(nameof(AwakeGetChild), "Awake",
                    nameof(Component.GetComponentInChildren),
                    nameof(Component.GetComponentsInChildren), messages, typeof(UnityEngine.Component),
                    false)

            );

            return messages;
        }

        public static AssemblyDefinition LoadAssemblyDefinition(ICompiledAssembly compiledAssembly)
        {
            var resolver = new PostProcessorAssemblyResolver(compiledAssembly);
            var readerParameters = new ReaderParameters
            {
                SymbolStream = new MemoryStream(compiledAssembly.InMemoryAssembly.PdbData.ToArray()),
                SymbolReaderProvider = new PortablePdbReaderProvider(),
                AssemblyResolver = resolver,
                ReflectionImporterProvider = new PostProcessorReflectionImporterProvider(),
                ReadingMode = ReadingMode.Immediate
            };

            var peStream = new MemoryStream(compiledAssembly.InMemoryAssembly.PeData.ToArray());
            var assemblyDefinition = AssemblyDefinition.ReadAssembly(peStream, readerParameters);

            //apparently, it will happen that when we ask to resolve a type that lives inside Unity.Entities, and we
            //are also postprocessing Unity.Entities, type resolving will fail, because we do not actually try to resolve
            //inside the assembly we are processing. Let's make sure we do that, so that we can use postprocessor features inside
            //unity.entities itself as well.
            resolver.AddAssemblyDefinitionBeingOperatedOn(assemblyDefinition);

            return assemblyDefinition;
        }
    }
    
}

