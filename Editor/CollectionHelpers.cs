using System;
using System.Collections.Generic;
using Mono.Cecil;
using Mono.Collections.Generic;
using Unity.CompilationPipeline.Common.Diagnostics;

namespace ILAwake.Editor
{
    public class MethodPair
    {
        public readonly Type WhereToFind;
        
        public readonly List<DiagnosticMessage> Messages;
        public readonly string AttributeName;
        public readonly string InsertMethodName;
        public readonly string ReplaceMethodName_Simple;
        public readonly string ReplaceMethodName_Array;
        public readonly bool IsStaticCall;

        public System.Reflection.MethodInfo SimpleMethodInfo;
        public System.Reflection.MethodInfo ArrayMethodInfo;

        public MethodReference SimpleMethodReference;
        public MethodReference ArrayMethodReference;
        
        public readonly Collection<FieldDefinition> SimpleFields = new Collection<FieldDefinition>();
        public readonly Collection<FieldDefinition> ArrayFields = new Collection<FieldDefinition>();

        public MethodPair(string attributeName, string insertMethodName, string replaceMethodNameSimple, string replaceMethodNameArray, List<DiagnosticMessage> messages, 
            Type whereToFind, bool isStaticCall)
        {
            AttributeName = attributeName;
            InsertMethodName = insertMethodName;
            ReplaceMethodName_Simple = replaceMethodNameSimple;
            ReplaceMethodName_Array = replaceMethodNameArray;
            Messages = messages;
            WhereToFind = whereToFind;
            IsStaticCall = isStaticCall;
        }

        public void Clear()
        {
            SimpleFields.Clear();
            ArrayFields.Clear();
        }
        
    }
    
}