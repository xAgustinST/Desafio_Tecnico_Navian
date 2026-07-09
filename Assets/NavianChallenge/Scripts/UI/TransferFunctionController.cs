using UnityEngine;
using UnityEngine.UI;
using UnityVolumeRendering;

namespace NavianChallenge.UI
{
    /// <summary>Lives in the "RENDER MODE" section: DVR/MIP/Isosurface buttons plus a few advanced sliders.</summary>
    public class TransferFunctionController : MonoBehaviour
    {
        VolumeRenderedObject volume;
        float visMin;
        float visMax;
        float[] baseAlphas;
        Button[] renderModeButtons;
        Slider minSlider, maxSlider;

        public void Init(Transform sectionContent, VolumeRenderedObject vol)
        {
            volume = vol;

            Vector2 window = volume.GetVisibilityWindow();
            visMin = window.x;
            visMax = window.y;

            if (volume.transferFunction != null)
            {
                var points = volume.transferFunction.alphaControlPoints;
                baseAlphas = new float[points.Count];
                for (int i = 0; i < points.Count; i++)
                    baseAlphas[i] = points[i].alphaValue;
            }

            renderModeButtons = UIFactory.ButtonGroup(sectionContent, new[] { "DVR", "MIP", "Isosurface" }, 0, OnRenderModeChanged);

            UIFactory.SubHeader(sectionContent, "Advanced");
            minSlider = UIFactory.MakeSlider(sectionContent, "Visibility window (min)", 0f, 1f, visMin, v => { visMin = v; ApplyWindow(); });
            maxSlider = UIFactory.MakeSlider(sectionContent, "Visibility window (max)", 0f, 1f, visMax, v => { visMax = v; ApplyWindow(); });
            // "Transfer function opacity" intentionally not built here: the vendor shader
            // forces alpha=1 on every Isosurface hit (DirectVolumeRenderingShader.shader,
            // frag_surf), so this slider only ever did anything in DVR — confusing more than
            // useful. SetOpacityMultiplier/baseAlphas are left in place below in case it's
            // worth re-adding as a DVR-only control later.
            UIFactory.MakeToggle(sectionContent, "Lighting", volume.GetLightingEnabled(), volume.SetLightingEnabled);
        }

        /// <summary>Used by ClinicalPresetsController to switch render mode and keep the button highlight in sync.</summary>
        public void ApplyRenderMode(int index) => OnRenderModeChanged(index);

        /// <summary>Used by ClinicalPresetsController; also keeps the min/max sliders in sync.</summary>
        public void ApplyVisibilityWindow(float min, float max)
        {
            visMin = min;
            visMax = max;
            if (minSlider != null) minSlider.value = min;
            if (maxSlider != null) maxSlider.value = max; // triggers ApplyWindow via its own callback
            else ApplyWindow();
        }

        void OnRenderModeChanged(int index)
        {
            switch (index)
            {
                case 0: volume.SetRenderMode(UnityVolumeRendering.RenderMode.DirectVolumeRendering); break;
                case 1: volume.SetRenderMode(UnityVolumeRendering.RenderMode.MaximumIntensityProjectipon); break;
                case 2: volume.SetRenderMode(UnityVolumeRendering.RenderMode.IsosurfaceRendering); break;
            }
            if (renderModeButtons != null) UIFactory.HighlightButtonGroup(renderModeButtons, index);
        }

        void ApplyWindow()
        {
            if (visMin > visMax)
            {
                float tmp = visMin;
                visMin = visMax;
                visMax = tmp;
            }
            volume.SetVisibilityWindow(visMin, visMax);
        }

        void SetOpacityMultiplier(float mult)
        {
            if (volume.transferFunction == null || baseAlphas == null) return;
            var points = volume.transferFunction.alphaControlPoints;
            for (int i = 0; i < points.Count && i < baseAlphas.Length; i++)
            {
                var p = points[i];
                p.alphaValue = Mathf.Clamp01(baseAlphas[i] * mult);
                points[i] = p;
            }
            volume.transferFunction.GenerateTexture();
            volume.SetTransferFunction(volume.transferFunction);
        }
    }
}
