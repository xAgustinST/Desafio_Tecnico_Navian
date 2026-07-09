using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityVolumeRendering;

namespace NavianChallenge.UI
{
    /// <summary>
    /// Composition root for the "Neuro Surgical Explorer" panel: builds the Canvas/
    /// EventSystem and a fixed-width, scrollable left panel procedurally (no scene edits,
    /// no prefabs), laid out in the five sections the panel always shows in this order —
    /// Anatomy Layers, Anatomical Planes, 2D Slice Preview, Volume Inspection, Render Mode.
    /// Section headers exist immediately; the sections that depend on the async MRI volume
    /// (Planes/Preview/Inspection/Render) are populated once <see cref="OnVolumeReady"/>
    /// fires, so ordering never depends on load timing.
    /// </summary>
    public class ChallengeHUD : MonoBehaviour
    {
        const float PanelWidth = 400f;

        static readonly (string name, string displayName, Color color)[] StructureDefaults =
        {
            ("Skin",        "Skin",         new Color(0.90f, 0.76f, 0.65f, 0.35f)),
            ("GrayMatter",  "Gray Matter",  new Color(0.72f, 0.72f, 0.72f, 1f)),
            ("WhiteMatter", "White Matter", new Color(0.93f, 0.90f, 0.82f, 1f)),
            ("Veins",       "Veins",        new Color(0.75f, 0.10f, 0.12f, 1f)),
        };

        RectTransform panelContainer;
        Transform canvasRoot;
        AtlasVolumeLoader loader;
        AtlasSceneController sceneController;
        Transform meshesRoot;

        readonly Dictionary<string, Renderer> structureRenderers = new Dictionary<string, Renderer>();
        readonly Dictionary<string, StructureVisibilityController> structureControllers = new Dictionary<string, StructureVisibilityController>();

        bool panelVisible = true;

        public AtlasVolumeLoader Loader => loader;
        public IReadOnlyDictionary<string, Renderer> StructureRenderers => structureRenderers;

        void Awake()
        {
            try
            {
                BuildEventSystem();

                var (root, panel, header, content) = UIFactory.CreateCanvasAndScrollPanel(transform, PanelWidth);
                canvasRoot = root;
                panelContainer = panel;
                EnsureUnderCanvas(panelContainer.transform, "PanelContainer");
                BuildHeader(header);

                FindReferences();
                SuppressLegacyHelpOverlay();

                Transform anatomySection = UIFactory.CreateSection(content, "ANATOMY LAYERS");
                Transform planesSection = UIFactory.CreateSection(content, "ANATOMICAL PLANES");
                Transform previewSection = UIFactory.CreateSection(content, "2D SLICE PREVIEW");
                Transform poiSection = UIFactory.CreateSection(content, "POINT OF INTEREST");
                Transform inspectionSection = UIFactory.CreateSection(content, "VOLUME INSPECTION");
                Transform renderSection = UIFactory.CreateSection(content, "RENDER MODE");

                BuildStructuresSection(anatomySection);
                gameObject.AddComponent<StructureSelector>().Init(canvasRoot, structureRenderers);

                if (loader != null)
                {
                    loader.VolumeReady += v => OnVolumeReady(v, anatomySection, planesSection, inspectionSection, renderSection, previewSection, poiSection);
                    if (loader.Volume != null)
                        OnVolumeReady(loader.Volume, anatomySection, planesSection, inspectionSection, renderSection, previewSection, poiSection);
                }
                else
                {
                    Debug.LogWarning("[ChallengeHUD] No AtlasVolumeLoader found in the scene — Planes/Inspection/Render Mode/2D Preview sections will stay empty until one exists.");
                }

                Debug.Log("[ChallengeHUD] Panel built successfully.");
            }
            catch (System.Exception e)
            {
                Debug.LogError("[ChallengeHUD] Failed to build the panel — see exception below.");
                Debug.LogException(e);
            }
        }

        void Update()
        {
            if (Input.GetKeyDown(KeyCode.Tab))
            {
                panelVisible = !panelVisible;
                panelContainer.gameObject.SetActive(panelVisible);
            }
        }

        void BuildEventSystem()
        {
            if (EventSystem.current != null) return;
            var go = new GameObject("EventSystem");
            go.AddComponent<EventSystem>();
            go.AddComponent<StandaloneInputModule>();
        }

        /// <summary>
        /// Defensive check: a RectTransform with no Canvas ancestor never renders (it just
        /// silently sits invisible, even though its own Pos/Width/Height look perfectly
        /// normal) — this is a nasty failure mode to debug blind. If SetParent somehow
        /// didn't stick (stale compiled assembly, editor hiccup, etc.), force the correct
        /// parent here and log loudly either way so this is never silently wrong again.
        /// </summary>
        void EnsureUnderCanvas(Transform t, string label)
        {
            if (t.GetComponentInParent<Canvas>() != null) return;

            if (canvasRoot == null)
            {
                Debug.LogError($"[ChallengeHUD] '{label}' had no Canvas ancestor AND canvasRoot itself is null/destroyed — cannot recover.");
                return;
            }

            Debug.LogError($"[ChallengeHUD] '{label}' had NO Canvas ancestor after being built (parent was '{(t.parent != null ? t.parent.name : "NULL")}') — force-reparenting under '{canvasRoot.name}' now.");
            t.SetParent(canvasRoot, false);
        }

        void BuildHeader(Transform header)
        {
            UIFactory.Label(header, "Neuro Surgical Explorer", 22, FontStyle.Bold, TextAnchor.MiddleLeft, UIFactory.ColorText);
            UIFactory.Label(header, "MRI volume + anatomical layers", 13, FontStyle.Normal, TextAnchor.MiddleLeft, UIFactory.ColorTextDim);
            UIFactory.Label(header, "Mouse drag: orbit  |  Wheel: zoom  |  R: reset  |  H: help", 11, FontStyle.Italic, TextAnchor.MiddleLeft, UIFactory.ColorTextDim);
            UIFactory.Label(header, "Tab: show/hide this panel", 11, FontStyle.Italic, TextAnchor.MiddleLeft, UIFactory.ColorTextDim);
        }

        void FindReferences()
        {
            loader = FindAnyObjectByType<AtlasVolumeLoader>();
            sceneController = FindAnyObjectByType<AtlasSceneController>();
            Transform atlasRoot = loader != null ? loader.atlasRoot : null;
            meshesRoot = atlasRoot != null ? atlasRoot.Find("MeshesRoot") : null;
        }

        void SuppressLegacyHelpOverlay()
        {
            // AtlasSceneController draws its own on-screen OnGUI help box; our panel header
            // already shows the same shortcuts, so hide the old box to avoid overlap.
            // Camera controls themselves (orbit/zoom/R/H) are untouched.
            if (sceneController != null) sceneController.SetHelpVisible(false);
        }

        void BuildStructuresSection(Transform content)
        {
            if (meshesRoot == null) return;

            foreach (var def in StructureDefaults)
            {
                Transform t = meshesRoot.Find(def.name);
                if (t == null) continue;

                if (t.GetComponent<Collider>() == null)
                    t.gameObject.AddComponent<MeshCollider>();

                var renderer = t.GetComponent<Renderer>();
                if (renderer == null) continue;
                structureRenderers[def.name] = renderer;

                Material mat = renderer.material; // per-instance copy, safe at runtime
                SetMaterialTransparent(mat);
                mat.color = def.color;

                var controller = t.gameObject.AddComponent<StructureVisibilityController>();
                controller.Init(content, def.displayName, renderer, mat, def.color.a);
                structureControllers[def.name] = controller;
            }
        }

        const string ClipShaderName = "NavianChallenge/ClippedTransparent";

        /// <summary>
        /// Makes a structure mesh's material transparent AND clippable by the same plane
        /// CrossSectionController drives for the MRI volume (see ClippedTransparent.shader).
        /// Falls back to the old Standard-shader Fade setup (no clipping, but still
        /// transparent) if the custom shader can't be found, so this never hard-fails.
        /// </summary>
        static void SetMaterialTransparent(Material m)
        {
            Shader clipShader = Shader.Find(ClipShaderName);
            if (clipShader != null)
            {
                m.shader = clipShader;
            }
            else
            {
                Debug.LogWarning($"[ChallengeHUD] Shader '{ClipShaderName}' not found — anatomy meshes won't cut away with the cross-section plane. Falling back to plain transparency.");
                m.SetOverrideTag("RenderType", "Transparent");
                m.SetFloat("_Mode", 3f);
                m.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
                m.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                m.SetInt("_ZWrite", 0);
                m.DisableKeyword("_ALPHATEST_ON");
                m.EnableKeyword("_ALPHABLEND_ON");
                m.DisableKeyword("_ALPHAPREMULTIPLY_ON");
            }
            m.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
        }

        void OnVolumeReady(VolumeRenderedObject volume, Transform anatomySection,
            Transform planesSection, Transform inspectionSection, Transform renderSection, Transform previewSection,
            Transform poiSection)
        {
            var volumeRenderer = volume.GetComponentInChildren<MeshRenderer>();
            var volumeToggle = UIFactory.MakeToggle(anatomySection, "MRI Volume",
                volumeRenderer == null || volumeRenderer.enabled,
                isOn => { if (volumeRenderer != null) volumeRenderer.enabled = isOn; });
            volumeToggle.transform.SetSiblingIndex(0); // "MRI Volume" listed first, ahead of the mesh toggles

            var tfController = gameObject.AddComponent<TransferFunctionController>();
            tfController.Init(renderSection, volume);

            // Anatomical Planes / Volume Inspection's clip plane needs to actually sweep
            // through the anatomy meshes, not just the MRI volume's own bounds (the meshes
            // can extend beyond it) — and both controllers must agree on the exact same
            // pivot/range or their synced sliders drift apart. Compute it once, here.
            Bounds clipBounds = volumeRenderer != null
                ? volumeRenderer.bounds
                : new Bounds(volume.transform.position, Vector3.one);
            foreach (var r in structureRenderers.Values)
                if (r != null) clipBounds.Encapsulate(r.bounds);

            var crossController = gameObject.AddComponent<CrossSectionController>();
            crossController.Init(inspectionSection, volume, clipBounds);

            var sliceController = gameObject.AddComponent<SliceExplorerController>();
            sliceController.Init(planesSection, previewSection, volume, clipBounds);

            // Keep Anatomical Planes and Volume Inspection's cross-section plane in sync
            // (axis + position), so the 2D slice preview updates live as either moves.
            sliceController.LinkCrossSection(crossController);
            crossController.LinkSliceExplorer(sliceController);

            var poiController = gameObject.AddComponent<PointOfInterestController>();
            poiController.Init(poiSection, volume, sliceController, Mathf.Max(clipBounds.extents.x, clipBounds.extents.y, clipBounds.extents.z));
        }
    }
}
