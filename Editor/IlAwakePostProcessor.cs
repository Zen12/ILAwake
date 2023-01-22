using System.Collections.Generic;
using System.IO;
using System.Linq;
using ILAwake.Runtime;
using Mono.Cecil;
using Mono.Cecil.Cil;
using Mono.Collections.Generic;
using Unity.CompilationPipeline.Common.Diagnostics;
using Unity.CompilationPipeline.Common.ILPostProcessing;
using UnityEngine;
using MethodAttributes = Mono.Cecil.MethodAttributes;

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
            var allSimpleFields = new Collection<FieldDefinition>();
            var allArrayFields = new Collection<FieldDefinition>();
            
            var getComponent = typeof(MonoBehaviour)
                .GetMethods()
                .First(m => m.IsGenericMethod && m.Name == nameof(MonoBehaviour.GetComponent));
            
            var getComponentArray = typeof(MonoBehaviour)
                .GetMethods()
                .First(m => m.IsGenericMethod && m.Name == nameof(MonoBehaviour.GetComponents) && m.GetParameters().Length == 0);

            foreach (var moduleDefinition in assemblyDefinition.Modules)
            {
                var getComponentRef = moduleDefinition.ImportReference(getComponent);
                var getComponentArrayRef = moduleDefinition.ImportReference(getComponentArray);
                
                foreach (var typeDefinition in moduleDefinition.Types)
                {
                    if (typeDefinition.BaseType == null ||
                        typeDefinition.BaseType.Name != nameof(MonoBehaviour))
                        continue;
                    
                    allSimpleFields.Clear();
                    allArrayFields.Clear();

                    foreach (var field in typeDefinition.Fields)
                    {
                        foreach (var customAttribute in field.CustomAttributes)
                        {
                            if (customAttribute.AttributeType.Name == nameof(AwakeGet))
                            {
                                if (field.FieldType.IsArray)
                                {
                                    allArrayFields.Add(field);
                                }
                                else
                                {
                                    allSimpleFields.Add(field);
                                }
                                break;
                            }
                        }
                    }

                    if (allSimpleFields.Count > 0 || allArrayFields.Count > 0)
                    {
                        MethodDefinition awakeMethod = null;
                        foreach (var methodDefinition in typeDefinition.Methods)
                        {
                            if (methodDefinition.Name == "Awake")
                            {
                                awakeMethod = methodDefinition;
                            }
                        }



                        if (awakeMethod == null)
                        {
                            awakeMethod = new MethodDefinition("Awake",
                                MethodAttributes.Private | MethodAttributes.HideBySig,
                                moduleDefinition.TypeSystem.Void);
                            awakeMethod.Body.InitLocals = true;

                            typeDefinition.Methods.Add(awakeMethod);
                            awakeMethod.Body.GetILProcessor().Emit(OpCodes.Ret);
                        }
                       
                        var ilProcessor = awakeMethod.Body.GetILProcessor();
                        var first = ilProcessor.Body.Instructions[0];

                        foreach (var awakeGetProperty in allSimpleFields)
                        {
                            var fieldType = awakeGetProperty.FieldType;
                            
                            ilProcessor.InsertBefore(first, ilProcessor.Create(OpCodes.Ldarg_0));
                            ilProcessor.InsertBefore(first, ilProcessor.Create(OpCodes.Ldarg_0));
                            
                            var genComponentMethInstance = new GenericInstanceMethod(getComponentRef);
                            
                            genComponentMethInstance.GenericArguments.Add(fieldType);
                            ilProcessor.InsertBefore(first, ilProcessor.Create(OpCodes.Call, genComponentMethInstance));
                            ilProcessor.InsertBefore(first, ilProcessor.Create(OpCodes.Stfld, awakeGetProperty));
                        }
                        
                        
                        foreach (var awakeGetProperty in allArrayFields)
                        {
                            var fieldType = awakeGetProperty.FieldType;
                            
                            ilProcessor.InsertBefore(first, ilProcessor.Create(OpCodes.Ldarg_0));
                            ilProcessor.InsertBefore(first, ilProcessor.Create(OpCodes.Ldarg_0));
                            
                            var genComponentMethInstance = new GenericInstanceMethod(getComponentArrayRef);

                            var type = moduleDefinition.ImportReference(fieldType.Resolve());

                            genComponentMethInstance.GenericArguments.Add(type);

                            ilProcessor.InsertBefore(first, ilProcessor.Create(OpCodes.Call, genComponentMethInstance));
                            ilProcessor.InsertBefore(first, ilProcessor.Create(OpCodes.Stfld, awakeGetProperty));
                        }
                    }
                }
            }

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

