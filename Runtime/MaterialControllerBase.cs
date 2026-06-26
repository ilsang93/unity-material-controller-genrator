using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEngine.UI;

namespace MaterialControl
{
    /// <summary>
    /// Shared base for generated, shader-specific material controllers.
    ///
    /// Two ways to choose the material to control:
    ///
    ///   - <see cref="TargetMode.Auto"/>: detect a <see cref="Renderer"/> or
    ///     <see cref="Graphic"/> on this GameObject (covers SpriteRenderer,
    ///     MeshRenderer, UI Image/RawImage, TextMeshProUGUI, ...). Convenient, but
    ///     only works for components whose material is exposed the standard way.
    ///
    ///   - <see cref="TargetMode.DirectMaterial"/>: the user assigns a Material
    ///     reference directly. The controller writes to it regardless of which
    ///     component owns it — so it works with ANY graphic component, including
    ///     ones with custom material exposure (e.g. a plain MonoBehaviour holding a
    ///     <c>public Material</c> field).
    ///
    /// A generated subclass declares one named serialized field per shader property
    /// and implements <see cref="Apply"/>. Because those fields are plain named
    /// members (float / Color / Vector4), Unity's Animation window lists them by
    /// name in "Add Property" and Record mode captures them.
    /// </summary>
    [ExecuteAlways]
    public abstract class MaterialControllerBase : MonoBehaviour
    {
        public enum TargetMode
        {
            Auto,
            DirectMaterial
        }

        protected enum TargetKind { None, Renderer, Graphic }

        [Tooltip("Auto: detect a Renderer/Graphic on this GameObject.\n" +
                 "Direct Material: control the material assigned below, whatever component owns it.")]
        [SerializeField] protected TargetMode targetMode = TargetMode.Auto;

        [Tooltip("Direct Material mode: the material to control. Works with any graphic component.")]
        [SerializeField] protected Material targetMaterial;

        [Tooltip("If checked, the material is cloned at runtime so the original asset is never modified.")]
        [SerializeField] protected bool instance = true;

        [Tooltip("Auto mode: optional explicit target. Leave empty to auto-detect a Renderer or Graphic.")]
        [SerializeField] protected Component explicitTarget;

        private Renderer _renderer;
        private Graphic _graphic;
        private TargetKind _targetKind = TargetKind.None;

        private Material _workingMaterial;
        private Material _instancedMaterial;
        private bool _resolved;

        protected virtual void OnEnable()
        {
            _resolved = false;
            Resolve();
        }

        protected virtual void OnDisable()
        {
            if (_instancedMaterial != null)
            {
                if (Application.isPlaying) Destroy(_instancedMaterial);
                else DestroyImmediate(_instancedMaterial);
                _instancedMaterial = null;
            }
            _resolved = false;
        }

        protected virtual void LateUpdate()
        {
            if (!_resolved) Resolve();
            if (_workingMaterial != null)
                Apply(_workingMaterial);
        }

        /// <summary>Generated subclasses push their fields onto the material here.</summary>
        protected abstract void Apply(Material material);

        /// <summary>Resolves the target and, at runtime with instancing on, clones the material.</summary>
        protected void Resolve()
        {
            _renderer = null;
            _graphic = null;
            _targetKind = TargetKind.None;

            Material shared = ResolveSharedMaterial();
            if (shared == null) { _workingMaterial = null; _resolved = true; return; }

            if (instance && Application.isPlaying)
            {
                if (_instancedMaterial == null)
                    _instancedMaterial = new Material(shared) { name = shared.name + " (Controller Instance)" };
                _workingMaterial = _instancedMaterial;
                AssignMaterial(shared, _workingMaterial);
            }
            else
            {
                _workingMaterial = shared;
            }

            _resolved = true;
        }

        private Material ResolveSharedMaterial()
        {
            if (targetMode == TargetMode.DirectMaterial)
                return targetMaterial;

            // Auto mode.
            if (explicitTarget is Renderer er) { _renderer = er; _targetKind = TargetKind.Renderer; }
            else if (explicitTarget is Graphic eg) { _graphic = eg; _targetKind = TargetKind.Graphic; }
            else if (TryGetComponent(out Renderer ar)) { _renderer = ar; _targetKind = TargetKind.Renderer; }
            else if (TryGetComponent(out Graphic ag)) { _graphic = ag; _targetKind = TargetKind.Graphic; }

            return _targetKind switch
            {
                TargetKind.Renderer => _renderer != null ? _renderer.sharedMaterial : null,
                TargetKind.Graphic => _graphic != null ? _graphic.material : null,
                _ => null
            };
        }

        /// <summary>The original shared material that will be controlled (for editor tools).</summary>
        public Material GetSharedMaterial()
        {
            if (targetMode == TargetMode.DirectMaterial)
                return targetMaterial;

            if (_targetKind == TargetKind.None && !_resolved)
            {
                if (explicitTarget is Renderer er) return er.sharedMaterial;
                if (explicitTarget is Graphic eg) return eg.material;
                if (TryGetComponent(out Renderer ar)) return ar.sharedMaterial;
                if (TryGetComponent(out Graphic ag)) return ag.material;
                return null;
            }
            return _targetKind switch
            {
                TargetKind.Renderer => _renderer != null ? _renderer.sharedMaterial : null,
                TargetKind.Graphic => _graphic != null ? _graphic.material : null,
                _ => null
            };
        }

        /// <summary>
        /// Assigns the working (cloned) material back to wherever the shared material
        /// was used, so the rendered result reflects the clone.
        /// </summary>
        private void AssignMaterial(Material shared, Material clone)
        {
            if (targetMode == TargetMode.Auto)
            {
                switch (_targetKind)
                {
                    case TargetKind.Renderer: if (_renderer != null) _renderer.material = clone; break;
                    case TargetKind.Graphic: if (_graphic != null) _graphic.material = clone; break;
                }
                return;
            }

            // Direct mode: we hold a material reference but don't know which component
            // uses it. Scan this GameObject's components and reassign the clone wherever
            // the shared material is referenced — standard Renderer/Graphic AND custom
            // public/serialized Material fields (e.g. VMG's `public Material Material`).
            ReassignDirectMaterial(shared, clone);
        }

        private void ReassignDirectMaterial(Material shared, Material clone)
        {
            bool reassignedAny = false;
            var components = GetComponents<Component>();
            foreach (var c in components)
            {
                if (c == null || c == this) continue;

                // Standard renderers / graphics.
                if (c is Renderer r && r.sharedMaterial == shared) { r.material = clone; reassignedAny = true; continue; }
                if (c is Graphic g && g.material == shared) { g.material = clone; reassignedAny = true; continue; }

                // Custom Material fields/properties via reflection.
                if (ReassignMaterialMembers(c, shared, clone))
                    reassignedAny = true;
            }

            // If nothing referenced it, the controller still drives the clone in place;
            // but the renderer would keep drawing the shared material. Warn once so the
            // user can switch to Auto mode or bind the right object.
            if (!reassignedAny)
            {
                Debug.LogWarning(
                    $"[MaterialController] Direct mode on '{name}': the bound material was not found on any " +
                    "component of this GameObject, so the instanced clone is controlled but may not be the one " +
                    "being rendered. Bind the controller to the same GameObject that uses the material, or turn " +
                    "off Instance.", this);
            }
        }

        private static bool ReassignMaterialMembers(Component c, Material shared, Material clone)
        {
            bool any = false;
            const BindingFlags flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;
            var type = c.GetType();

            // Public/serialized fields of type Material.
            foreach (var f in GetMaterialFields(type))
            {
                if (f.GetValue(c) as Material == shared)
                {
                    f.SetValue(c, clone);
                    any = true;
                }
            }

            // Writable properties of type Material (e.g. a custom `Material` property).
            foreach (var p in type.GetProperties(flags))
            {
                if (p.PropertyType != typeof(Material) || !p.CanRead || !p.CanWrite) continue;
                if (p.GetIndexParameters().Length != 0) continue;
                // Skip Unity's built-in material accessors to avoid unintended instantiation.
                if (p.Name == "material" || p.Name == "materials") continue;
                try
                {
                    if (p.GetValue(c) as Material == shared)
                    {
                        p.SetValue(c, clone);
                        any = true;
                    }
                }
                catch { /* property getter/setter may throw on some components; ignore. */ }
            }

            return any;
        }

        private static readonly Dictionary<System.Type, FieldInfo[]> s_MaterialFieldCache =
            new Dictionary<System.Type, FieldInfo[]>();

        private static FieldInfo[] GetMaterialFields(System.Type type)
        {
            if (s_MaterialFieldCache.TryGetValue(type, out var cached))
                return cached;

            const BindingFlags flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;
            var list = new List<FieldInfo>();
            foreach (var f in type.GetFields(flags))
            {
                if (f.FieldType != typeof(Material)) continue;
                bool serialized = f.IsPublic || f.IsDefined(typeof(SerializeField), false);
                if (serialized) list.Add(f);
            }
            var arr = list.ToArray();
            s_MaterialFieldCache[type] = arr;
            return arr;
        }
    }
}
