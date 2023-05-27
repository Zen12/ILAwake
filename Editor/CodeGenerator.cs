using System.Linq;
using Mono.Cecil;
using Mono.Cecil.Cil;
using Unity.CompilationPipeline.Common.Diagnostics;
using UnityEngine;

namespace ILAwake.Editor
{
    public static class CodeGenerator
    {
        public static MethodDefinition GetOrCreateMethod(in string methodName, TypeDefinition typeDefinition,
            ModuleDefinition moduleDefinition)
        {
            foreach (var methodDefinition in typeDefinition.Methods)
            {
                if (methodDefinition.Name == methodName)
                {
                    return methodDefinition;
                }
            }

            var awakeMethod = new MethodDefinition(methodName,
                MethodAttributes.Private | MethodAttributes.HideBySig,
                moduleDefinition.TypeSystem.Void);
            awakeMethod.Body.InitLocals = true;

            typeDefinition.Methods.Add(awakeMethod);
            awakeMethod.Body.GetILProcessor().Emit(OpCodes.Ret);
            return awakeMethod;
        }

        public static void GenerateCodeForAttributeName(AssemblyDefinition assemblyDefinition,
            params MethodPair[] pairs)
        {
            
            foreach (var pair in pairs)
            {
                pair.SimpleMethodInfo = pair.SimpleMethodInfo = pair.WhereToFind
                    .GetMethods()
                    .FirstOrDefault(m => m.IsGenericMethod && m.Name == pair.ReplaceMethodName_Simple);
                
                
                pair.ArrayMethodInfo = pair.ArrayMethodInfo = pair.WhereToFind
                    .GetMethods()
                    .FirstOrDefault(m => m.IsGenericMethod && m.Name == pair.ReplaceMethodName_Array &&
                                m.GetParameters().Length == 0);
                
            }


            foreach (var moduleDefinition in assemblyDefinition.Modules)
            {
                foreach (var pair in pairs)
                {
                    pair.SimpleMethodReference = moduleDefinition.ImportReference(pair.SimpleMethodInfo);
                    pair.ArrayMethodReference = moduleDefinition.ImportReference(pair.ArrayMethodInfo);
                }

                foreach (var typeDefinition in moduleDefinition.Types)
                {
                    if (typeDefinition.BaseType != null && typeDefinition.BaseType == null &&
                        typeDefinition.BaseType.Name == nameof(MonoBehaviour))
                        continue;

                    foreach (var pair in pairs)
                    {
                        pair.Clear();
                    }


                    foreach (var field in typeDefinition.Fields)
                    {
                        foreach (var customAttribute in field.CustomAttributes)
                        {
                            foreach (var pair in pairs)
                            {
                                if (customAttribute.AttributeType.Name == pair.AttributeName)
                                {
                                    if (field.FieldType.IsArray)
                                    {
                                        pair.ArrayFields.Add(field);
                                    }
                                    else
                                    {
                                        pair.SimpleFields.Add(field);
                                    }

                                    break;
                                }
                            }

                        }
                    }

                    

                    for (int i = 0; i < pairs.Length; i++)
                    {
                        var pair = pairs[i];
                        var previousWasStatic = true;
                        
          
                        if (pair.SimpleFields.Count > 0 || pair.ArrayFields.Count > 0)
                        {
                            if (i < pairs.Length - 1)
                            {
                                previousWasStatic = pairs[i + 1].IsStaticCall;
                            }

                            GenerateCodeWithField(pair, typeDefinition, moduleDefinition);
                            
                            if (previousWasStatic == true && pair.IsStaticCall == false)
                            {
                                var awakeMethod = GetOrCreateMethod(pairs.Last().InsertMethodName, typeDefinition, moduleDefinition);
                                var ilProcessor = awakeMethod.Body.GetILProcessor();
                                ilProcessor.InsertBefore(ilProcessor.Body.Instructions[0], ilProcessor.Create(OpCodes.Ldarg_0));
                            }
                        }
                    }

                    {
                        var awakeMethod = GetOrCreateMethod(pairs.Last().InsertMethodName, typeDefinition, moduleDefinition);
                        var ilProcessor = awakeMethod.Body.GetILProcessor();
                        ilProcessor.InsertBefore(ilProcessor.Body.Instructions[0], ilProcessor.Create(OpCodes.Nop));
                    }

                }

            }
        }

        public static void GenerateCodeWithField(MethodPair pair,
            TypeDefinition typeDefinition, ModuleDefinition moduleDefinition)
        {
            var awakeMethod = GetOrCreateMethod(pair.InsertMethodName, typeDefinition, moduleDefinition);

            var ilProcessor = awakeMethod.Body.GetILProcessor();
            var first = ilProcessor.Body.Instructions[0];

            
            for (int i = 0; i < pair.SimpleFields.Count; i++)
            {
                var awakeGetProperty = pair.SimpleFields[i];
                                
                var fieldType = awakeGetProperty.FieldType;
                ilProcessor.InsertBefore(first, ilProcessor.Create(OpCodes.Ldarg_0));

                var genComponentMethInstance = new GenericInstanceMethod(pair.SimpleMethodReference);

                genComponentMethInstance.GenericArguments.Add(fieldType);
                ilProcessor.InsertBefore(first, ilProcessor.Create(OpCodes.Call, genComponentMethInstance));
                ilProcessor.InsertBefore(first, ilProcessor.Create(OpCodes.Stfld, awakeGetProperty));
            }
            

            for (int i = 0; i < pair.ArrayFields.Count; i++)
            {
                var awakeGetProperty = pair.ArrayFields[i];

                var fieldType = awakeGetProperty.FieldType;

                ilProcessor.InsertBefore(first, ilProcessor.Create(OpCodes.Ldarg_0));

                var genComponentMethInstance = new GenericInstanceMethod(pair.ArrayMethodReference);

                var type = moduleDefinition.ImportReference(fieldType.Resolve());

                genComponentMethInstance.GenericArguments.Add(type);

                ilProcessor.InsertBefore(first, ilProcessor.Create(OpCodes.Call, genComponentMethInstance));
                ilProcessor.InsertBefore(first, ilProcessor.Create(OpCodes.Stfld, awakeGetProperty));
            }
            
        }
    }
}