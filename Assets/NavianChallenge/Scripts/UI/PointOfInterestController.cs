using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using UnityVolumeRendering;

namespace NavianChallenge.UI
{
    /// <summary>
    /// Lives in the "POINT OF INTEREST" section: click the 2D Slice Preview thumbnail to
    /// drop a marker (small sphere) at the matching position in the 3D view — a lightweight
    /// stand-in for "flag something that looks off" the way real neuronavigation tools let a
    /// surgeon mark a lesion on a slice and see it located in 3D space.
    ///
    /// Deliberately minimal: no editing, notes, or route planning yet — those are natural
    /// follow-ups once this proves useful, not built now. <see cref="MarkerPositions"/> is
    /// exposed (world-space) so a future "best access route to this point" analysis has
    /// something to read without needing to touch this file again.
    ///
    /// Fully self-contained and safe to delete: remove this file and the Init() call in
    /// ChallengeHUD, and the feature is gone with no side effects on anything else (it only
    /// reads from SliceExplorerController, never writes to it).
    /// </summary>
    public class PointOfInterestController : MonoBehaviour
    {
        static readonly Color MarkerColor = new Color(1f, 0.75f, 0.15f, 1f);

        SliceExplorerController slice;
        Transform markersRoot;
        Transform sectionContent;
        float markerDiameter;
        Text countLabel;

        readonly List<GameObject> markers = new List<GameObject>();
        readonly List<GameObject> markerRows = new List<GameObject>();

        /// <summary>World-space positions of all current markers — for a future route-planning feature.</summary>
        public IReadOnlyList<Vector3> MarkerPositions
        {
            get
            {
                var list = new List<Vector3>(markers.Count);
                foreach (var m in markers)
                    if (m != null) list.Add(m.transform.position);
                return list;
            }
        }

        public void Init(Transform content, VolumeRenderedObject volume, SliceExplorerController sliceExplorer, float range)
        {
            slice = sliceExplorer;
            sectionContent = content;
            markerDiameter = Mathf.Max(range * 0.05f, 0.001f);

            Transform parent = volume != null && volume.transform.parent != null ? volume.transform.parent : transform;
            var rootGo = new GameObject("PointOfInterestMarkers");
            rootGo.transform.SetParent(parent, false);
            markersRoot = rootGo.transform;

            UIFactory.Label(sectionContent, "Click the 2D slice preview above to mark a point of interest.",
                12, FontStyle.Italic, TextAnchor.MiddleLeft, UIFactory.ColorTextDim);
            countLabel = UIFactory.Label(sectionContent, "Markers: 0", 12, FontStyle.Normal, TextAnchor.MiddleLeft, UIFactory.ColorTextDim);
            UIFactory.MakeButton(sectionContent, "Clear markers", ClearMarkers);

            if (slice != null && slice.PreviewImage != null)
            {
                var forwarder = slice.PreviewImage.gameObject.AddComponent<ClickForwarder>();
                forwarder.Clicked += OnPreviewClicked;
            }
        }

        void OnPreviewClicked(PointerEventData eventData, RectTransform rect)
        {
            if (slice == null || rect == null) return;
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(rect, eventData.position, eventData.pressEventCamera, out Vector2 local))
                return;

            Rect r = rect.rect;
            float u = Mathf.InverseLerp(r.xMin, r.xMax, local.x);
            float v = Mathf.InverseLerp(r.yMin, r.yMax, local.y);

            if (slice.TryPreviewPointToWorld(new Vector2(u, v), out Vector3 worldPos))
                PlaceMarker(worldPos);
        }

        void PlaceMarker(Vector3 worldPos)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            go.name = "POI Marker";
            Destroy(go.GetComponent<Collider>()); // never block StructureSelector's raycasts

            go.transform.SetParent(markersRoot, true);
            go.transform.position = worldPos;
            go.transform.localScale = Vector3.one * markerDiameter;

            Material mat = go.GetComponent<Renderer>().material; // per-instance copy
            mat.color = MarkerColor;
            if (mat.HasProperty("_EmissionColor"))
            {
                mat.EnableKeyword("_EMISSION");
                mat.SetColor("_EmissionColor", MarkerColor * 0.6f);
            }

            markers.Add(go);

            int index = markers.Count;
            Debug.Log($"[PointOfInterest] Marker #{index} placed at world position {worldPos:F2}");

            Text row = UIFactory.Label(sectionContent, $"{index}.  ({worldPos.x:F1}, {worldPos.y:F1}, {worldPos.z:F1})",
                11, FontStyle.Normal, TextAnchor.MiddleLeft, UIFactory.ColorText);
            markerRows.Add(row.gameObject);

            RefreshCountLabel();
        }

        void ClearMarkers()
        {
            foreach (var m in markers)
                if (m != null) Destroy(m);
            markers.Clear();

            foreach (var row in markerRows)
                if (row != null) Destroy(row);
            markerRows.Clear();

            RefreshCountLabel();
        }

        void RefreshCountLabel()
        {
            if (countLabel != null) countLabel.text = $"Markers: {markers.Count}";
        }

        /// <summary>Tiny click-forwarder so SliceExplorerController's RawImage doesn't need
        /// to know markers exist — this component is added onto it from the outside.</summary>
        class ClickForwarder : MonoBehaviour, IPointerClickHandler
        {
            public event System.Action<PointerEventData, RectTransform> Clicked;
            public void OnPointerClick(PointerEventData eventData) => Clicked?.Invoke(eventData, transform as RectTransform);
        }
    }
}
