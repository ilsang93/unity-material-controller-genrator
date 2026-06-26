using UnityEngine;
using UnityEngine.UI;

namespace MaterialControl
{
    /// <summary>
    /// Shared base for generated, shader-specific material controllers.
    ///
    /// Handles target resolution (Renderer or Graphic — covers SpriteRenderer,
    /// MeshRenderer, UI Image/RawImage, TextMeshProUGUI, ...), runtime material
    /// instancing so the source asset is never modified, and the per-frame push.
    ///
    /// A generated subclass declares one named serialized field per shader
    /// property and implements <see cref="Apply"/>. Because those fields are plain
    /// named members (float / Color / Vector4), Unity's Animation window lists them
    /// by name in "Add Property" and Record mode captures them.
    /// </summary>
    [ExecuteAlways]
    public abstract class MaterialControllerBase : MonoBehaviour
    {
        protected enum TargetKind { None, Renderer, Graphic }

        [Tooltip("If checked, the material is cloned at runtime so the original asset is never modified.")]
        [SerializeField] protected bool instance = true;

        [Tooltip("Optional explicit target. Leave empty to auto-detect a Renderer or Graphic on this GameObject.")]
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

            if (explicitTarget is Renderer er) { _renderer = er; _targetKind = TargetKind.Renderer; }
            else if (explicitTarget is Graphic eg) { _graphic = eg; _targetKind = TargetKind.Graphic; }
            else if (TryGetComponent(out Renderer ar)) { _renderer = ar; _targetKind = TargetKind.Renderer; }
            else if (TryGetComponent(out Graphic ag)) { _graphic = ag; _targetKind = TargetKind.Graphic; }

            Material shared = GetSharedMaterial();
            if (shared == null) { _workingMaterial = null; _resolved = true; return; }

            if (instance && Application.isPlaying)
            {
                if (_instancedMaterial == null)
                    _instancedMaterial = new Material(shared) { name = shared.name + " (Controller Instance)" };
                _workingMaterial = _instancedMaterial;
                AssignMaterial(_workingMaterial);
            }
            else
            {
                _workingMaterial = shared;
            }

            _resolved = true;
        }

        /// <summary>The original shared material on the resolved target.</summary>
        public Material GetSharedMaterial()
        {
            if (_targetKind == TargetKind.None && !_resolved)
            {
                // Allow editor tools to query before OnEnable runs.
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

        private void AssignMaterial(Material mat)
        {
            switch (_targetKind)
            {
                case TargetKind.Renderer: if (_renderer != null) _renderer.material = mat; break;
                case TargetKind.Graphic: if (_graphic != null) _graphic.material = mat; break;
            }
        }
    }
}
