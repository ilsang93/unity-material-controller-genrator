using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEditor.Callbacks;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UI;

namespace MaterialControl.Editor
{
    /// <summary>
    /// Generates a shader-specific MaterialController component for a target
    /// GameObject and attaches it. Invokable from:
    ///   - the GameObject context/hierarchy menu, and
    ///   - the context menu (header "...") of a Renderer or Graphic component.
    ///
    /// The generated class declares one named serialized field per shader property,
    /// so Unity's Animation window lists and records each property by name.
    ///
    /// Generated scripts are written to the user's Assets (not the package), since
    /// they are project-specific and the package folder is distributable.
    /// </summary>
    public static class MaterialControllerGenerator
    {
        private const string OutputDir = "Assets/MaterialControllerGenerated";
        private const string ClassPrefix = "MatCtrl_";

        // Queued attachment that survives the domain reload after generation.
        private const string PendingInstanceIdKey = "MaterialControl.Pending.InstanceId";
        private const string PendingClassNameKey = "MaterialControl.Pending.ClassName";

        // ----- Menu entry points --------------------------------------------

        // GameObject right-click / hierarchy menu.
        [MenuItem("GameObject/Material Controller/Generate Controller", false, 49)]
        private static void GenerateFromGameObjectMenu(MenuCommand command)
        {
            var go = command.context as GameObject ?? Selection.activeGameObject;
            if (go == null)
            {
                EditorUtility.DisplayDialog("Material Controller", "No GameObject selected.", "OK");
                return;
            }
            Generate(go);
        }

        [MenuItem("GameObject/Material Controller/Generate Controller", true)]
        private static bool ValidateFromGameObjectMenu()
        {
            var go = Selection.activeGameObject;
            return go != null && FindMaterial(go) != null;
        }

        // Component header "..." menu on any Renderer (SpriteRenderer, MeshRenderer, ...).
        [MenuItem("CONTEXT/Renderer/Generate Material Controller", false, 1000)]
        private static void GenerateFromRendererContext(MenuCommand command)
        {
            GenerateFromComponent(command.context as Component);
        }

        [MenuItem("CONTEXT/Renderer/Generate Material Controller", true)]
        private static bool ValidateFromRendererContext(MenuCommand command)
        {
            return command.context is Renderer r && r.sharedMaterial != null;
        }

        // Component header "..." menu on any Graphic (Image, RawImage, TextMeshProUGUI, ...).
        [MenuItem("CONTEXT/Graphic/Generate Material Controller", false, 1000)]
        private static void GenerateFromGraphicContext(MenuCommand command)
        {
            GenerateFromComponent(command.context as Component);
        }

        [MenuItem("CONTEXT/Graphic/Generate Material Controller", true)]
        private static bool ValidateFromGraphicContext(MenuCommand command)
        {
            return command.context is Graphic g && g.material != null;
        }

        private static void GenerateFromComponent(Component component)
        {
            if (component == null) return;
            Generate(component.gameObject);
        }

        // ----- Core ---------------------------------------------------------

        private static void Generate(GameObject go)
        {
            Material material = FindMaterial(go);
            if (material == null)
            {
                EditorUtility.DisplayDialog("Material Controller",
                    $"'{go.name}' has no Renderer or Graphic with a material.", "OK");
                return;
            }

            Shader shader = material.shader;
            string className = ClassPrefix + Sanitize(shader.name);
            Type existing = FindType(className);

            if (existing != null)
            {
                // Reuse: attach the existing controller if not already present.
                AttachIfMissing(go, existing);
                Debug.Log($"[MaterialController] Reused existing '{className}' for '{go.name}'.");
                return;
            }

            string path = $"{OutputDir}/{className}.cs";
            if (AssetDatabase.LoadAssetAtPath<MonoScript>(path) != null)
            {
                EditorUtility.DisplayDialog("Material Controller",
                    $"Script '{path}' already exists but its type is not available.\n" +
                    "Resolve any compile errors, then add the component manually.", "OK");
                return;
            }

            string source = BuildSource(className, shader, material);
            EnsureDir(OutputDir);
            System.IO.File.WriteAllText(path, source);
            AssetDatabase.ImportAsset(path);

            // Queue attachment for after the domain reload completes.
            SessionState.SetInt(PendingInstanceIdKey, go.GetInstanceID());
            SessionState.SetString(PendingClassNameKey, className);

            Debug.Log($"[MaterialController] Generated '{path}'. It will be attached to '{go.name}' after compilation.");
            AssetDatabase.Refresh();
        }

        [DidReloadScripts]
        private static void OnScriptsReloaded()
        {
            string className = SessionState.GetString(PendingClassNameKey, null);
            int instanceId = SessionState.GetInt(PendingInstanceIdKey, 0);
            if (string.IsNullOrEmpty(className) || instanceId == 0)
                return;

            SessionState.EraseString(PendingClassNameKey);
            SessionState.EraseInt(PendingInstanceIdKey);

            Type type = FindType(className);
            if (type == null)
            {
                Debug.LogWarning($"[MaterialController] Generated type '{className}' not found after reload " +
                                 "(possible compile error). Add the component manually once it compiles.");
                return;
            }

            var go = EditorUtility.InstanceIDToObject(instanceId) as GameObject;
            if (go == null)
            {
                Debug.LogWarning("[MaterialController] Target GameObject no longer exists; skipping auto-attach.");
                return;
            }

            AttachIfMissing(go, type);
            Debug.Log($"[MaterialController] Attached '{className}' to '{go.name}'.");
        }

        private static void AttachIfMissing(GameObject go, Type type)
        {
            if (go.GetComponent(type) != null)
                return;
            Undo.AddComponent(go, type);
            EditorUtility.SetDirty(go);
        }

        // ----- Material discovery -------------------------------------------

        private static Material FindMaterial(GameObject go)
        {
            if (go.TryGetComponent(out Renderer r) && r.sharedMaterial != null)
                return r.sharedMaterial;
            if (go.TryGetComponent(out Graphic g) && g.material != null)
                return g.material;
            return null;
        }

        // ----- Source generation --------------------------------------------

        private static string BuildSource(string className, Shader shader, Material material)
        {
            var fields = new StringBuilder();
            var ids = new StringBuilder();
            var apply = new StringBuilder();

            var usedFieldNames = new HashSet<string>();
            int count = shader.GetPropertyCount();

            for (int i = 0; i < count; i++)
            {
                var flags = shader.GetPropertyFlags(i);
                if ((flags & ShaderPropertyFlags.HideInInspector) != 0)
                    continue;

                string propName = shader.GetPropertyName(i);
                ShaderPropertyType pType = shader.GetPropertyType(i);

                string fieldName = MakeFieldName(propName, usedFieldNames);
                string idName = fieldName + "ID";
                string description = Escape(shader.GetPropertyDescription(i));

                fields.AppendLine($"        [Tooltip(\"{description}  ({propName})\")]");

                switch (pType)
                {
                    case ShaderPropertyType.Color:
                    {
                        Color c = material.HasProperty(propName) ? material.GetColor(propName) : Color.white;
                        fields.AppendLine($"        public Color {fieldName} = new Color({F(c.r)}, {F(c.g)}, {F(c.b)}, {F(c.a)});");
                        apply.AppendLine($"            material.SetColor({idName}, {fieldName});");
                        break;
                    }
                    case ShaderPropertyType.Float:
                    case ShaderPropertyType.Range:
                    {
                        float v = material.HasProperty(propName) ? material.GetFloat(propName) : 0f;
                        if (pType == ShaderPropertyType.Range)
                        {
                            Vector2 range = shader.GetPropertyRangeLimits(i);
                            fields.AppendLine($"        [Range({F(range.x)}, {F(range.y)})]");
                        }
                        fields.AppendLine($"        public float {fieldName} = {F(v)};");
                        apply.AppendLine($"            material.SetFloat({idName}, {fieldName});");
                        break;
                    }
                    case ShaderPropertyType.Vector:
                    {
                        Vector4 v = material.HasProperty(propName) ? material.GetVector(propName) : Vector4.zero;
                        fields.AppendLine($"        public Vector4 {fieldName} = new Vector4({F(v.x)}, {F(v.y)}, {F(v.z)}, {F(v.w)});");
                        apply.AppendLine($"            material.SetVector({idName}, {fieldName});");
                        break;
                    }
                    case ShaderPropertyType.Texture:
                    {
                        // Texture is not animatable; exposed for inspector control only.
                        fields.AppendLine($"        public Texture {fieldName};");
                        apply.AppendLine($"            if ({fieldName} != null) material.SetTexture({idName}, {fieldName});");
                        break;
                    }
                    case ShaderPropertyType.Int:
                    {
                        int v = material.HasProperty(propName) ? material.GetInt(propName) : 0;
                        fields.AppendLine($"        public int {fieldName} = {v};");
                        apply.AppendLine($"            material.SetInt({idName}, {fieldName});");
                        break;
                    }
                    default:
                        continue;
                }

                ids.AppendLine($"        private static readonly int {idName} = Shader.PropertyToID(\"{propName}\");");
                fields.AppendLine();
            }

            var sb = new StringBuilder();
            sb.AppendLine("// <auto-generated>");
            sb.AppendLine("// Generated by MaterialControllerGenerator. Safe to edit, but");
            sb.AppendLine("// regenerating requires deleting this file first.");
            sb.AppendLine($"// Shader: {shader.name}");
            sb.AppendLine("// </auto-generated>");
            sb.AppendLine("using UnityEngine;");
            sb.AppendLine("using MaterialControl;");
            sb.AppendLine();
            sb.AppendLine("namespace MaterialControl.Generated");
            sb.AppendLine("{");
            sb.AppendLine($"    [AddComponentMenu(\"Rendering/Material Controllers/{className}\")]");
            sb.AppendLine($"    public class {className} : MaterialControllerBase");
            sb.AppendLine("    {");
            sb.Append(fields);
            sb.Append(ids);
            sb.AppendLine();
            sb.AppendLine("        protected override void Apply(Material material)");
            sb.AppendLine("        {");
            sb.Append(apply);
            sb.AppendLine("        }");
            sb.AppendLine("    }");
            sb.AppendLine("}");
            return sb.ToString();
        }

        // ----- Helpers ------------------------------------------------------

        private static string MakeFieldName(string propName, HashSet<string> used)
        {
            string n = propName.StartsWith("_") ? propName.Substring(1) : propName;
            n = Regex.Replace(n, "[^A-Za-z0-9_]", "_");
            if (n.Length == 0 || char.IsDigit(n[0])) n = "_" + n;

            string candidate = n;
            int suffix = 1;
            while (used.Contains(candidate))
                candidate = n + "_" + (++suffix);
            used.Add(candidate);
            return candidate;
        }

        private static string Sanitize(string shaderName)
        {
            string n = Regex.Replace(shaderName, "[^A-Za-z0-9]", "_");
            n = Regex.Replace(n, "_+", "_").Trim('_');
            if (n.Length == 0) n = "Shader";
            if (char.IsDigit(n[0])) n = "_" + n;
            return n;
        }

        private static string Escape(string s) =>
            string.IsNullOrEmpty(s) ? "" : s.Replace("\\", "\\\\").Replace("\"", "\\\"");

        private static string F(float v) =>
            v.ToString("R", System.Globalization.CultureInfo.InvariantCulture) + "f";

        private static void EnsureDir(string assetDir)
        {
            string full = System.IO.Path.Combine(
                System.IO.Path.GetDirectoryName(Application.dataPath) ?? "", assetDir);
            if (!System.IO.Directory.Exists(full))
            {
                System.IO.Directory.CreateDirectory(full);
                AssetDatabase.Refresh();
            }
        }

        private static Type FindType(string className)
        {
            string full = "MaterialControl.Generated." + className;
            foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
            {
                var t = asm.GetType(full);
                if (t != null) return t;
            }
            return null;
        }
    }
}
