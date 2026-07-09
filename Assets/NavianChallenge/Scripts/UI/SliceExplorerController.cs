using UnityEngine;
using UnityEngine.UI;
using UnityVolumeRendering;

namespace NavianChallenge.UI
{
    /// <summary>
    /// Splits across two sections: "ANATOMICAL PLANES" (axis buttons + position slider +
    /// current-plane label) and "2D SLICE PREVIEW" (a small live thumbnail). A single
    /// <see cref="SlicingPlane"/> is reused across axes — switching the axis re-rotates it
    /// and re-aims a dedicated orthographic camera that renders into a RenderTexture shown
    /// in the preview thumbnail. This is a real GPU-sampled cut of the loaded MRI (not a
    /// synthetic mockup), shown next to the main volumetric view for a live
    /// volumetric-vs-2D-slice comparison.
    ///
    /// Axis-to-rotation mapping: the vendor's SlicingPlane prefab is a Unity primitive
    /// Plane, which rests flat in the local XZ plane with its face normal along local
    /// +Y — NOT a Quad (XY plane, +Z normal). Confirmed against the vendor's own
    /// SliceRenderingEditorWindow, which uses transform.up as the normal throughout.
    /// So the normal/depth axis here is always transform.up, and reorienting the plane
    /// to point along a different world axis means rotating it AWAY from Y — rotating
    /// around Y itself (the normal) is a no-op (a plane spun around its own normal is
    /// still facing the same way), which is why the previous Euler(0,90,0) "Sagittal"
    /// entry never actually changed the view.
    /// </summary>
    public class SliceExplorerController : MonoBehaviour
    {
        const int SliceLayer = 28; // free runtime-only layer, not defined in TagManager
        const int PreviewSize = 260;

        static readonly Quaternion[] AxisRotations =
        {
            Quaternion.identity,          // normal = +Y (Axial)
            Quaternion.Euler(90f, 0f, 0f),  // normal = +Z (Sagital)
            Quaternion.Euler(0f, 0f, 90f),  // normal = +X (Coronal)
        };
        static readonly string[] AxisLabels = { "Axial", "Sagital", "Coronal" };

        // Maps this controller's axis index to CrossSectionController's X/Y/Z axis index,
        // per the requested pairing: Axial<->X, Coronal<->Y, Sagital<->Z.
        static readonly int[] SliceToCrossAxis = { 0, 2, 1 };

        VolumeRenderedObject volume;
        Vector3 volumeCenter;
        float range;

        SlicingPlane plane;
        Camera previewCam;
        RenderTexture previewRT;
        Button[] axisButtons;
        Slider positionSlider;
        Text currentPlaneLabel;
        int activeAxis;

        CrossSectionController cross;
        bool syncingFromCross;

        /// <summary>Links this controller to the Volume Inspection cross-section plane so
        /// their axis selection and position sliders move together.</summary>
        public void LinkCrossSection(CrossSectionController crossSection)
        {
            cross = crossSection;
        }

        public void Init(Transform planesSectionContent, Transform previewSectionContent, VolumeRenderedObject vol, Bounds bounds)
        {
            volume = vol;
            volumeCenter = bounds.center;
            range = Mathf.Max(bounds.extents.x, bounds.extents.y, bounds.extents.z);

            axisButtons = UIFactory.ButtonGroup(planesSectionContent, AxisLabels, 0, SetAxis);
            positionSlider = UIFactory.MakeSlider(planesSectionContent, "Plane position", -0.5f, 0.5f, 0f, SetPosition);
            currentPlaneLabel = UIFactory.Label(planesSectionContent, "Current plane: Axial", 12, FontStyle.Italic, TextAnchor.MiddleLeft, UIFactory.ColorTextDim);

            BuildPlaneAndCamera();
            BuildPreview(previewSectionContent);

            SetAxis(0);
        }

        void BuildPlaneAndCamera()
        {
            plane = volume.CreateSlicingPlane();
            plane.gameObject.layer = SliceLayer;

            var camGo = new GameObject("SlicePreviewCam");
            camGo.transform.SetParent(transform, false);
            previewCam = camGo.AddComponent<Camera>();
            previewCam.clearFlags = CameraClearFlags.SolidColor;
            previewCam.backgroundColor = new Color(0.02f, 0.02f, 0.03f, 1f);
            previewCam.orthographic = true;
            previewCam.orthographicSize = range * 1.1f;
            previewCam.cullingMask = 1 << SliceLayer;
            previewCam.nearClipPlane = 0.05f;
            previewCam.farClipPlane = range * 6f;

            previewRT = new RenderTexture(PreviewSize, PreviewSize, 16) { name = "SlicePreviewRT" };
            previewCam.targetTexture = previewRT;

            Camera mainCam = Camera.main;
            if (mainCam != null) mainCam.cullingMask &= ~(1 << SliceLayer);
        }

        RawImage previewImage;

        /// <summary>The 2D preview's RawImage — exposed so PointOfInterestController can
        /// attach a click handler without SliceExplorerController needing to know markers
        /// exist at all.</summary>
        public RawImage PreviewImage => previewImage;

        void BuildPreview(Transform previewSectionContent)
        {
            previewImage = UIFactory.CreateThumbnail(previewSectionContent, PreviewSize);
            previewImage.texture = previewRT;

            Text caption = UIFactory.Label(previewSectionContent,
                "Live GPU slice of the loaded MRI volume — not a diagnostic-grade render.",
                10, FontStyle.Italic, TextAnchor.MiddleLeft, UIFactory.ColorTextDim);
            caption.horizontalOverflow = HorizontalWrapMode.Wrap;
            UIFactory.SetPreferredHeight(caption.gameObject, 32);
        }

        /// <summary>
        /// Converts a normalized point within the 2D slice preview (0,0 = bottom-left,
        /// 1,1 = top-right — matches RectTransformUtility's convention) into a world-space
        /// position on the currently active slice plane. Used by PointOfInterestController
        /// to turn a click on the 2D image into a 3D marker; harmless to call even if that
        /// feature is removed later.
        /// </summary>
        public bool TryPreviewPointToWorld(Vector2 normalizedPoint, out Vector3 worldPos)
        {
            worldPos = default;
            if (previewCam == null || plane == null) return false;

            float halfSize = previewCam.orthographicSize;
            float offsetX = (normalizedPoint.x - 0.5f) * 2f * halfSize;
            float offsetY = (normalizedPoint.y - 0.5f) * 2f * halfSize;

            Transform camT = previewCam.transform;
            worldPos = plane.transform.position + camT.right * offsetX + camT.up * offsetY;
            return true;
        }

        void SetAxis(int index)
        {
            activeAxis = index;
            if (axisButtons != null) UIFactory.HighlightButtonGroup(axisButtons, index);
            if (currentPlaneLabel != null) currentPlaneLabel.text = "Current plane: " + AxisLabels[index];

            plane.transform.localRotation = AxisRotations[index];
            plane.transform.localPosition = Vector3.zero;
            if (positionSlider != null) positionSlider.value = 0f;

            Vector3 normal = plane.transform.up;
            Vector3 up = Mathf.Abs(Vector3.Dot(normal, Vector3.up)) > 0.95f ? Vector3.forward : Vector3.up;
            previewCam.transform.position = volumeCenter - normal * (range * 3f);
            previewCam.transform.rotation = Quaternion.LookRotation(normal, up);

            if (!syncingFromCross) cross?.SyncAxisFromSlice(SliceToCrossAxis[index]);
        }

        void SetPosition(float value)
        {
            if (plane == null) return;
            Vector3 localNormal = AxisRotations[activeAxis] * Vector3.up;
            plane.transform.localPosition = localNormal * value;

            // Slice position is a fraction of the volume's local unit cube (-0.5..0.5);
            // CrossSectionController's clipping position is a world-space offset
            // (-range..range) — 2*range converts between the two linearly.
            if (!syncingFromCross) cross?.SyncPositionFromSlice(value * 2f * range);
        }

        /// <summary>Called by CrossSectionController when its own axis selection changes.</summary>
        public void SyncAxisFromCross(int sliceAxisIndex)
        {
            syncingFromCross = true;
            SetAxis(sliceAxisIndex);
            syncingFromCross = false;
        }

        /// <summary>Called by CrossSectionController when its own clipping-position slider changes.</summary>
        public void SyncPositionFromCross(float sliceValue)
        {
            syncingFromCross = true;
            if (positionSlider != null) positionSlider.value = Mathf.Clamp(sliceValue, -0.5f, 0.5f);
            syncingFromCross = false;
        }

        void OnDestroy()
        {
            if (previewRT != null) previewRT.Release();
        }
    }
}
