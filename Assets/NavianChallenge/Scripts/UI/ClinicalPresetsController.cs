using System.Collections.Generic;
using UnityEngine;

namespace NavianChallenge.UI
{
    /// <summary>
    /// One-click layer/render/camera combinations built entirely from the other
    /// controllers' public methods (SetVisible, ApplyRenderMode, ApplyVisibilityWindow,
    /// ResetInspection, ResetCamera) — no duplicated state, so the panel's toggles/sliders
    /// stay in sync with whatever a preset just changed.
    /// </summary>
    public class ClinicalPresetsController : MonoBehaviour
    {
        IReadOnlyDictionary<string, StructureVisibilityController> structures;
        Renderer volumeRenderer;
        TransferFunctionController tf;
        CrossSectionController cross;
        AtlasSceneController sceneController;

        public void Init(
            Transform sectionContent,
            IReadOnlyDictionary<string, StructureVisibilityController> structureControllers,
            Renderer volumeRend,
            TransferFunctionController transferFunctionController,
            CrossSectionController crossSectionController,
            AtlasSceneController atlasSceneController)
        {
            structures = structureControllers;
            volumeRenderer = volumeRend;
            tf = transferFunctionController;
            cross = crossSectionController;
            sceneController = atlasSceneController;

            UIFactory.MakeButton(sectionContent, "Overview", Overview);
            UIFactory.MakeButton(sectionContent, "Brain Focus", BrainFocus);
            UIFactory.MakeButton(sectionContent, "Vascular Focus", VascularFocus);
            UIFactory.MakeButton(sectionContent, "Volume Only", VolumeOnly);
            UIFactory.MakeButton(sectionContent, "Reset View", ResetView);
        }

        void Overview()
        {
            SetAllStructuresVisible(true);
            SetVolumeVisible(true);
            tf?.ApplyRenderMode(0);
            tf?.ApplyVisibilityWindow(0f, 1f);
            cross?.ResetInspection();
        }

        void BrainFocus()
        {
            SetVisible("Skin", false);
            SetVisible("Veins", false);
            SetVisible("GrayMatter", true);
            SetVisible("WhiteMatter", true);
            SetVolumeVisible(true);
            tf?.ApplyVisibilityWindow(0.28f, 1f);
        }

        void VascularFocus()
        {
            SetVisible("Skin", false);
            SetVisible("GrayMatter", false);
            SetVisible("WhiteMatter", false);
            SetVisible("Veins", true);
            SetVolumeVisible(true);
            tf?.ApplyVisibilityWindow(0.05f, 0.55f);
        }

        void VolumeOnly()
        {
            SetAllStructuresVisible(false);
            SetVolumeVisible(true);
            tf?.ApplyVisibilityWindow(0f, 1f);
        }

        void ResetView()
        {
            sceneController?.ResetCamera();
        }

        void SetAllStructuresVisible(bool visible)
        {
            if (structures == null) return;
            foreach (var kvp in structures) kvp.Value.SetVisible(visible);
        }

        void SetVisible(string name, bool visible)
        {
            if (structures != null && structures.TryGetValue(name, out var ctrl) && ctrl != null) ctrl.SetVisible(visible);
        }

        void SetVolumeVisible(bool visible)
        {
            if (volumeRenderer != null) volumeRenderer.enabled = visible;
        }
    }
}
