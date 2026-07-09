using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

namespace NavianChallenge.UI
{
    /// <summary>Click-to-select structures: highlights the hit mesh and shows an info panel.</summary>
    public class StructureSelector : MonoBehaviour
    {
        const float ClickMoveThreshold = 6f;
        const int StructureLayerMask = 1 << 0; // meshes stay on the Default layer

        static readonly Dictionary<string, (string title, string description)> Info = new Dictionary<string, (string, string)>
        {
            ["GrayMatter"] = ("Gray Matter", "Outer layer of the brain (cortex); contains neuron cell bodies and handles sensory, motor and cognitive processing."),
            ["WhiteMatter"] = ("White Matter", "Inner tissue made of myelinated axons; connects different cortical regions to each other and to the rest of the nervous system."),
            ["Veins"] = ("Veins", "Cerebral venous network visible in the MRI; drains blood from the brain tissue."),
            ["Skin"] = ("Skin", "Outer surface of the head; bounds the volume captured by the MRI scan."),
        };

        IReadOnlyDictionary<string, Renderer> structures;
        Camera cam;

        Vector2 mouseDownPos;

        string selectedName;
        Material selectedMaterial;
        Color selectedBaseEmission;

        RectTransform infoPanel;
        Text infoTitle;
        Text infoBody;
        Text toggleVisibilityLabel;

        public void Init(Transform canvasRoot, IReadOnlyDictionary<string, Renderer> structureRenderers)
        {
            structures = structureRenderers;
            cam = Camera.main;
            BuildInfoPanel(canvasRoot);
        }

        void BuildInfoPanel(Transform canvasRoot)
        {
            // Anchored bottom-right so it never overlaps the main control panel, which
            // spans the full screen height on the left.
            var go = new GameObject("SelectionInfo", typeof(RectTransform));
            infoPanel = go.GetComponent<RectTransform>();
            infoPanel.SetParent(canvasRoot, false);
            if (infoPanel.GetComponentInParent<Canvas>() == null)
            {
                Debug.LogError("[StructureSelector] 'SelectionInfo' had NO Canvas ancestor after SetParent — force-reparenting now.");
                infoPanel.SetParent(canvasRoot, false);
            }
            infoPanel.anchorMin = new Vector2(1f, 0f);
            infoPanel.anchorMax = new Vector2(1f, 0f);
            infoPanel.pivot = new Vector2(1f, 0f);
            infoPanel.anchoredPosition = new Vector2(-20, 20);
            infoPanel.sizeDelta = new Vector2(360, 10);

            var img = go.AddComponent<Image>();
            img.color = UIFactory.ColorBg;
            var vlg = go.AddComponent<VerticalLayoutGroup>();
            vlg.childControlWidth = true;
            vlg.childForceExpandWidth = true;
            vlg.childControlHeight = true;
            vlg.childForceExpandHeight = false;
            vlg.spacing = 6;
            vlg.padding = new RectOffset(16, 16, 14, 14);
            var fitter = go.AddComponent<ContentSizeFitter>();
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            infoTitle = UIFactory.Label(infoPanel, "", 17, FontStyle.Bold, TextAnchor.MiddleLeft, UIFactory.ColorText);
            infoBody = UIFactory.Label(infoPanel, "", 13, FontStyle.Normal, TextAnchor.UpperLeft, UIFactory.ColorTextDim);
            infoBody.horizontalOverflow = HorizontalWrapMode.Wrap;
            UIFactory.SetPreferredHeight(infoBody.gameObject, 64);

            var toggleBtn = UIFactory.MakeButton(infoPanel, "Hide", OnToggleVisibilityClicked);
            toggleVisibilityLabel = toggleBtn.GetComponentInChildren<Text>();

            infoPanel.gameObject.SetActive(false);
        }

        void Update()
        {
            if (cam == null) cam = Camera.main;
            if (cam == null) return;

            if (Input.GetMouseButtonDown(0))
                mouseDownPos = Input.mousePosition;

            if (Input.GetMouseButtonUp(0))
            {
                float moved = Vector2.Distance(mouseDownPos, Input.mousePosition);
                if (moved <= ClickMoveThreshold) HandleClick();
            }
        }

        void HandleClick()
        {
            if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
                return;

            Ray ray = cam.ScreenPointToRay(Input.mousePosition);
            if (Physics.Raycast(ray, out RaycastHit hit, 5000f, StructureLayerMask))
            {
                foreach (var kvp in structures)
                {
                    if (kvp.Value != null && hit.collider.gameObject == kvp.Value.gameObject)
                    {
                        Select(kvp.Key, kvp.Value);
                        return;
                    }
                }
            }
            Deselect();
        }

        void Select(string name, Renderer renderer)
        {
            if (selectedName == name) return;
            RestoreHighlight();

            selectedName = name;
            selectedMaterial = renderer.material;
            selectedMaterial.EnableKeyword("_EMISSION");
            selectedBaseEmission = selectedMaterial.HasProperty("_EmissionColor") ? selectedMaterial.GetColor("_EmissionColor") : Color.black;
            selectedMaterial.SetColor("_EmissionColor", new Color(0.35f, 0.55f, 0.95f) * 1.5f);

            infoPanel.gameObject.SetActive(true);
            if (Info.TryGetValue(name, out var entry))
            {
                infoTitle.text = entry.title;
                infoBody.text = entry.description;
            }
            else
            {
                infoTitle.text = name;
                infoBody.text = "";
            }
            RefreshVisibilityButtonLabel(renderer);
        }

        void Deselect()
        {
            RestoreHighlight();
            selectedName = null;
            infoPanel.gameObject.SetActive(false);
        }

        void RestoreHighlight()
        {
            if (selectedMaterial != null)
                selectedMaterial.SetColor("_EmissionColor", selectedBaseEmission);
            selectedMaterial = null;
        }

        void OnToggleVisibilityClicked()
        {
            if (selectedName == null) return;
            if (!structures.TryGetValue(selectedName, out Renderer renderer) || renderer == null) return;
            renderer.enabled = !renderer.enabled;
            RefreshVisibilityButtonLabel(renderer);
        }

        void RefreshVisibilityButtonLabel(Renderer renderer)
        {
            if (toggleVisibilityLabel != null)
                toggleVisibilityLabel.text = renderer.enabled ? "Hide" : "Show";
        }
    }
}
