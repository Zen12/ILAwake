using System.Collections.Generic;
using System.IO;
using Mono.Cecil;
using UnityEditor;
using UnityEngine;

namespace ILAwake.Editor
{
    public class IlViewerEditor : EditorWindow
    {
        private string _lastSearch = "Enter class name";
        private readonly List<TypeDefinition> _types = new List<TypeDefinition>();
        private TypeDefinition _showType;
        private Vector2 _scroll;
        
        
        [MenuItem("Window/ILViewer")]
        public static void ShowWindow()
        {
            var window = GetWindow(typeof(IlViewerEditor));
            window.titleContent = new GUIContent("ILViewer");
        }

        void OnGUI()
        {
            _lastSearch = EditorGUILayout.TextArea(_lastSearch);
            if (GUILayout.Button("Search"))
            {
                _showType = null;
                _scroll = Vector2.zero;
                Search();
            }
            _scroll = EditorGUILayout.BeginScrollView(_scroll);

            foreach (var type in _types)
            {
                if (GUILayout.Button(type.Name))
                {
                    _scroll = Vector2.zero;
                    _showType = type;
                }
            }
            

            if (_showType != null)
            {
                _types.Clear();
                foreach (var field in _showType.Fields)
                {
                    foreach (var customAttribute in field.CustomAttributes)
                    {
                        EditorGUILayout.LabelField($"[{customAttribute.AttributeType.Name}]");
                    }

                    EditorGUILayout.LabelField($"{field.FieldType} {field.Name}");
                }

                foreach (var method in _showType.Methods)
                {
                    foreach (var customAttribute in method.CustomAttributes)
                    {
                        EditorGUILayout.LabelField($"[{customAttribute.AttributeType.Name}]");
                    }

                    var p = $"{method.ReturnType} {method.Name} ";
                    foreach (var parameter in method.Parameters)
                    {
                        p += parameter.Name + " ";
                    }

                    EditorGUILayout.LabelField(p);
                    if (method.HasBody)
                    {
                        foreach (var instruction in method.Body.Instructions)
                        {
                            EditorGUILayout.LabelField("  " + instruction.ToString());
                        }
                    }
                }
            }
            EditorGUILayout.EndScrollView();
        }


        private void Search()
        {
            _types.Clear();
            var allDllsPath = Path.Combine(Application.dataPath.Replace("Assets", string.Empty),
                "Library", "ScriptAssemblies");
            var files = Directory.GetFiles(allDllsPath);
            foreach (var file in files)
            {
                if (file.EndsWith(".dll") == false)
                    continue;
                
                var path = Path.Combine(allDllsPath, file);
                var module = ModuleDefinition.ReadModule(path);
                foreach (var type in module.Types)
                {
                    if (type.Name.ToLower().Contains(_lastSearch.ToLower()))
                    {
                        _types.Add(type);
                    }
                }

            }
        }
    }
}
