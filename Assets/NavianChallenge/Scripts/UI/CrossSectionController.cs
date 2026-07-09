using UnityEngine;
using UnityEngine.UI;
using UnityVolumeRendering;

namespace NavianChallenge.UI
{
    /// <summary>
    /// Lives in the "VOLUME INSPECTION" section: cross-section plane over the MRI volume,
    /// plus a Reset Inspection button that clears it back to its default (disabled) state.
    /// Enabling the plane also cuts away the anatomy meshes (Skin/GrayMatter/WhiteMatter/
    /// Veins) at the same position — see <see cref="PushMeshClipState"/>. The crop (cutout)
    /// box UI is intentionally not built here — not useful enough yet to justify the panel
    /// space — but the underlying fields/methods are left in place so it's a one-line
    /// change (re-add the UIFactory calls in Init) to bring it back.
    /// </summary>
    public class CrossSectionController : MonoBehaviour
    {
        VolumeRenderedObject volume;
        Vector3 volumeCenter;
        float range;

        CrossSectionPlane plane;
        Toggle planeToggle;
        Slider planeSlider;
        Button[] axisButtons;
        static readonly Quaternion[] AxisRotations =
        {
            Quaternion.Euler(270f, 0f, 0f), // X — presets, fine-tuned visually in Play mode
            Quaternion.Euler(0f, 0f, 90f),  // Y
            Quaternion.Euler(0f, 90f, 0f),  // Z
        };

        CutoutBox box;
        Toggle boxToggle;
        Slider boxX, boxY, boxZ;
        bool boxExclusive = true;

        int activeAxis;

        // Maps this controller's X/Y/Z axis index to SliceExplorerController's axis index,
        // per the requested pairing: X<->Axial, Y<->Coronal, Z<->Sagital.
        static readonly int[] CrossToSliceAxis = { 0, 2, 1 };

        SliceExplorerController slice;
        bool syncingFromSlice;

        // Global shader properties read by NavianChallenge/ClippedTransparent (the anatomy
        // meshes' material) so Skin/GrayMatter/WhiteMatter/Veins cut away in sync with this
        // same plane — no per-material wiring needed, every material using that shader picks
        // these up automatically.
        static readonly int ClipPlaneID = Shader.PropertyToID("_NavianClipPlane");
        static readonly int ClipEnabledID = Shader.PropertyToID("_NavianClipEnabled");

        /// <summary>Links this controller to the Anatomical Planes slice explorer so their
        /// axis selection and position sliders move together.</summary>
        public void LinkSliceExplorer(SliceExplorerController sliceExplorer)
        {
            slice = sliceExplorer;
        }

        public void Init(Transform sectionContent, VolumeRenderedObject vol, Bounds bounds)
        {
            volume = vol;
            volumeCenter = bounds.center;
            range = Mathf.Max(bounds.extents.x, bounds.extents.y, bounds.extents.z);

            UIFactory.SubHeader(sectionContent, "Cross-Section Plane");
            planeToggle = UIFactory.MakeToggle(sectionContent, "Enable plane", false, OnPlaneToggle);
            axisButtons = UIFactory.ButtonGroup(sectionContent, new[] { "X", "Y", "Z" }, 0, SetPlaneAxis);
            planeSlider = UIFactory.MakeSlider(sectionContent, "Clipping position", -range, range, 0f, SetPlanePosition);

            UIFactory.MakeButton(sectionContent, "Reset Inspection", ResetInspection);

            PushMeshClipState(); // meshes start uncut, matching the plane's default-off state
        }

        /// <summary>Disables the cross-section plane and restores its controls to defaults
        /// (also clears the crop box's, if that UI is ever re-added).</summary>
        public void ResetInspection()
        {
            if (planeToggle != null) planeToggle.isOn = false;
            if (boxToggle != null) boxToggle.isOn = false;
            if (planeSlider != null) planeSlider.value = 0f;
            if (boxX != null) boxX.value = 1f;
            if (boxY != null) boxY.value = 1f;
            if (boxZ != null) boxZ.value = 1f;
            activeAxis = 0;
            if (axisButtons != null) UIFactory.HighlightButtonGroup(axisButtons, 0);
        }

        // --- Cross-section plane ---

        void OnPlaneToggle(bool on)
        {
            if (on)
            {
                if (plane != null) return;
                var go = Instantiate((GameObject)Resources.Load("CrossSectionPlane"));
                go.name = "CrossSectionPlane (runtime)";
                go.transform.position = volumeCenter;
                go.transform.rotation = AxisRotations[activeAxis]; // match whatever axis is currently selected (kept in sync with the slice explorer)
                plane = go.GetComponent<CrossSectionPlane>();
                plane.SetTargetObject(volume);
                if (planeSlider != null) planeSlider.value = 0f;
            }
            else if (plane != null)
            {
                Destroy(plane.gameObject);
                plane = null;
            }
            PushMeshClipState();
        }

        void SetPlaneAxis(int axisIndex)
        {
            activeAxis = axisIndex;
            if (axisButtons != null) UIFactory.HighlightButtonGroup(axisButtons, axisIndex);
            if (!syncingFromSlice) slice?.SyncAxisFromCross(CrossToSliceAxis[axisIndex]);

            if (plane == null) { PushMeshClipState(); return; }
            plane.transform.position = volumeCenter;
            plane.transform.rotation = AxisRotations[axisIndex];
            if (planeSlider != null) planeSlider.value = 0f;
            PushMeshClipState();
        }

        void SetPlanePosition(float offset)
        {
            if (plane != null)
                plane.transform.position = volumeCenter + plane.transform.forward * offset;

            // Clipping position is a world-space offset (-range..range); the slice explorer's
            // plane position is a fraction of the volume's local unit cube (-0.5..0.5).
            // Sync regardless of whether the cross-section plane itself is enabled — the
            // slider should still drive the always-visible 2D slice preview.
            if (!syncingFromSlice && range > 0.0001f) slice?.SyncPositionFromCross(offset / (2f * range));
            PushMeshClipState();
        }

        /// <summary>
        /// Pushes this plane's current state to the anatomy meshes' shader (global
        /// properties, see ClippedTransparent.shader) so Skin/GrayMatter/WhiteMatter/Veins
        /// cut away in sync with the same plane cutting the MRI volume. Sign convention
        /// matches the vendor DVR shader's own cross-section clip (VolumeCutout.cginc):
        /// the half-space the plane's forward axis points into gets clipped away.
        /// </summary>
        void PushMeshClipState()
        {
            if (plane == null)
            {
                Shader.SetGlobalFloat(ClipEnabledID, 0f);
                return;
            }
            Vector3 n = plane.transform.forward;
            Shader.SetGlobalVector(ClipPlaneID, new Vector4(n.x, n.y, n.z, Vector3.Dot(plane.transform.position, n)));
            Shader.SetGlobalFloat(ClipEnabledID, 1f);
        }

        /// <summary>Called by SliceExplorerController when its own axis selection changes.</summary>
        public void SyncAxisFromSlice(int crossAxisIndex)
        {
            syncingFromSlice = true;
            SetPlaneAxis(crossAxisIndex);
            syncingFromSlice = false;
        }

        /// <summary>Called by SliceExplorerController when its own plane-position slider changes.</summary>
        public void SyncPositionFromSlice(float crossValue)
        {
            syncingFromSlice = true;
            if (planeSlider != null) planeSlider.value = Mathf.Clamp(crossValue, -range, range);
            syncingFromSlice = false;
        }

        // --- Cutout box ---

        void OnBoxToggle(bool on)
        {
            if (on)
            {
                if (box != null) return;
                var go = Instantiate((GameObject)Resources.Load("CutoutBox"));
                go.name = "CutoutBox (runtime)";
                go.transform.position = volumeCenter;
                go.transform.rotation = Quaternion.Euler(270f, 0f, 0f);
                go.transform.localScale = new Vector3(boxX.value, boxY.value, boxZ.value);
                box = go.GetComponent<CutoutBox>();
                box.cutoutType = boxExclusive ? CutoutType.Exclusive : CutoutType.Inclusive;
                box.SetTargetObject(volume);
            }
            else if (box != null)
            {
                Destroy(box.gameObject);
                box = null;
            }
        }

        void SetBoxScale(int axis, float value)
        {
            if (box == null) return;
            Vector3 s = box.transform.localScale;
            s[axis] = value;
            box.transform.localScale = s;
        }
    }
}
