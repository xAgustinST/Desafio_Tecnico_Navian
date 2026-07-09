using UnityEngine;
using UnityEngine.UI;

namespace NavianChallenge.UI
{
    /// <summary>
    /// Per-structure show/hide toggle + opacity slider, added by <see cref="ChallengeHUD"/>.
    /// Exposes SetVisible/SetOpacityValue so <see cref="ClinicalPresetsController"/> can
    /// drive it programmatically while keeping the toggle/slider widgets themselves in sync
    /// (setting Toggle.isOn / Slider.value fires the same callback the user clicking would).
    /// </summary>
    public class StructureVisibilityController : MonoBehaviour
    {
        Renderer targetRenderer;
        Material materialInstance;
        Toggle visibilityToggle;
        Slider opacitySlider;

        public void Init(Transform sectionContent, string displayName, Renderer renderer, Material material, float initialOpacity)
        {
            targetRenderer = renderer;
            materialInstance = material;

            visibilityToggle = UIFactory.MakeToggle(sectionContent, displayName, true, isOn => targetRenderer.enabled = isOn);
            opacitySlider = UIFactory.MakeSlider(sectionContent, "Opacity", 0f, 1f, initialOpacity, SetOpacity);
        }

        void SetOpacity(float alpha)
        {
            if (materialInstance == null) return;
            Color c = materialInstance.color;
            c.a = alpha;
            materialInstance.color = c;
        }

        public void SetVisible(bool visible)
        {
            if (visibilityToggle != null) visibilityToggle.isOn = visible;
            else if (targetRenderer != null) targetRenderer.enabled = visible;
        }

        public void SetOpacityValue(float alpha)
        {
            if (opacitySlider != null) opacitySlider.value = alpha;
            else SetOpacity(alpha);
        }
    }
}
