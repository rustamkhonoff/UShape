using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Scripting.APIUpdating;

namespace RustamKhonoff.UShape
{
    public enum GradientType { Solid, Linear, Radial, Angular, Diamond }
    public enum ShapeFillMode { None, Horizontal, Vertical, Radial }
    public enum PointerSide { None, Top, Right, Bottom, Left }
    public enum StrokePattern { Solid, Dashed, Dotted }
    public enum StrokeMotion { None, March, Pulse }

    /// <summary>A single uGUI graphic that draws a rounded panel and its effects.</summary>
    [AddComponentMenu("UI/Shape", 10)]
    [MovedFrom(true, "RustamKhonoff.UIElement", "RustamKhonoff.UIElement", "Element")]
    [RequireComponent(typeof(RectTransform), typeof(CanvasRenderer))]
    public sealed class Shape : MaskableGraphic
    {
        [Tooltip("Top-left, top-right, bottom-right, bottom-left, in local UI units.")]
        public Vector4 cornerRadii = new Vector4(16, 16, 16, 16);
        public PanelPaint fill = new PanelPaint();
        [SerializeField] private ShapeFillMode fillMode;
        [SerializeField, Range(0, 1)] private float fillAmount = 1f;
        public PanelPointer pointer = new PanelPointer
        {
            side = PointerSide.None,
            position = .5f,
            width = 32,
            height = 16
        };
        [SerializeField] private PanelBlink blink = PanelBlink.Default;
        [Min(0)] public float borderWidth;
        public PanelPaint border = new PanelPaint();
        public PanelStroke borderStroke = PanelStroke.Default;
        [Min(0)] public float outlineWidth;
        public PanelPaint outline = new PanelPaint();
        public PanelStroke outlineStroke = PanelStroke.Default;
        public const int MaxShadowsPerType = 4;

        public PanelShadow outerShadow = new PanelShadow();
        public PanelShadow innerShadow = new PanelShadow();
        [SerializeField] private List<PanelShadow> extraOuterShadows;
        [SerializeField] private List<PanelShadow> extraInnerShadows;
        [SerializeField, Min(0), Tooltip("Seconds to settle near a new style target in Play Mode. Zero applies changes immediately.")]
        private float styleTransitionDuration;

        /// <summary>Seconds to settle to a new continuous style target. Zero disables smoothing.</summary>
        public float StyleTransitionDuration
        {
            get => styleTransitionDuration;
            set
            {
                float duration = Mathf.Max(0, value);
                if (!StyleValue.Set(ref styleTransitionDuration, duration) || transition?.IsRunning != true) return;
                if (duration == 0) Refresh();
                else QueueStyleUpdate(false);
            }
        }

        [SerializeField, Tooltip("Scale of the drawn shape around its center.")]
        private Vector2 visualScale = Vector2.one;
        [SerializeField, Tooltip("Offset of the drawn shape, in local UI units.")]
        private Vector2 visualOffset;
        private float pressScale = 1f;
        private Vector2 pressOffset;

        /// <summary>Scale of the drawn shape around its center.</summary>
        public Vector2 Scale
        {
            get => visualScale;
            set { if (StyleValue.Set(ref visualScale, value)) SetVerticesDirty(); }
        }

        /// <summary>Anchored position of the UI object and its hierarchy.</summary>
        public Vector2 RectPosition
        {
            get => rectTransform.anchoredPosition;
            set => rectTransform.anchoredPosition = value;
        }

        /// <summary>Offset of the drawn shape, in local UI units.</summary>
        public Vector2 VisualOffset
        {
            get => visualOffset;
            set { if (StyleValue.Set(ref visualOffset, value)) SetVerticesDirty(); }
        }

        internal void SetPressTransform(float scale, Vector2 offset)
        {
            bool scaleChanged = StyleValue.Set(ref pressScale, scale);
            bool offsetChanged = StyleValue.Set(ref pressOffset, offset);
            if (scaleChanged || offsetChanged) SetVerticesDirty();
        }

        public Color BaseColor { get => color; set => color = value; }
        public Vector4 CornerRadii { get => cornerRadii; set => SetVisual(ref cornerRadii, value); }
        public PanelPaint Fill { get => fill; set => ReplaceStyle(ref fill, value); }
        public ShapeFillMode FillMode { get => fillMode; set => SetVisual(ref fillMode, value); }
        public float FillAmount { get => fillAmount; set => SetVisual(ref fillAmount, Mathf.Clamp01(value)); }
        public float BorderWidth { get => borderWidth; set => SetVisual(ref borderWidth, Mathf.Max(0, value)); }
        public PanelPaint Border { get => border; set => ReplaceStyle(ref border, value); }
        public PanelStroke BorderStroke { get => borderStroke; set => SetVisual(ref borderStroke, value); }
        public float BorderPhase { get => borderStroke.phase; set => SetVisual(ref borderStroke.phase, value); }
        public float OutlineWidth { get => outlineWidth; set => SetVisual(ref outlineWidth, Mathf.Max(0, value), true); }
        public PanelPaint Outline { get => outline; set => ReplaceStyle(ref outline, value); }
        public PanelStroke OutlineStroke { get => outlineStroke; set => SetVisual(ref outlineStroke, value); }
        public float OutlinePhase { get => outlineStroke.phase; set => SetVisual(ref outlineStroke.phase, value); }
        public PanelPointer Pointer { get => pointer; set => SetVisual(ref pointer, value, true); }
        public float PointerPosition { get => pointer.position; set => SetVisual(ref pointer.position, Mathf.Clamp01(value)); }
        public PanelBlink Blink
        {
            get => blink;
            set
            {
                if (blink.enabled != value.enabled)
                {
                    wasBlinkEnabled = value.enabled;
                    blinkEpoch = Time.unscaledTime;
                }
                SetVisual(ref blink, value);
            }
        }

        /// <summary>Restarts the Blink clock, including its initial delay.</summary>
        public void RestartBlink()
        {
            blinkEpoch = Time.unscaledTime;
            QueueStyleUpdate(false);
        }
        public PanelShadow OuterShadow { get => outerShadow; set => ReplaceStyle(ref outerShadow, value, true); }
        public PanelShadow InnerShadow { get => innerShadow; set => ReplaceStyle(ref innerShadow, value); }

        public int OuterShadowCount => ShadowCount(extraOuterShadows);
        public int InnerShadowCount => ShadowCount(extraInnerShadows);

        public PanelShadow GetOuterShadow(int index) => GetShadow(outerShadow, extraOuterShadows, index);
        public PanelShadow GetInnerShadow(int index) => GetShadow(innerShadow, extraInnerShadows, index);
        public bool AddOuterShadow(PanelShadow layer) => AddShadow(ref extraOuterShadows, layer, true);
        public bool AddInnerShadow(PanelShadow layer) => AddShadow(ref extraInnerShadows, layer, false);
        public bool RemoveOuterShadow(PanelShadow layer) => RemoveShadow(ref outerShadow, extraOuterShadows, layer, true);
        public bool RemoveInnerShadow(PanelShadow layer) => RemoveShadow(ref innerShadow, extraInnerShadows, layer, false);

        /// <summary>Layers behind outerShadow, listed from front to back.</summary>
        public List<PanelShadow> AdditionalOuterShadows => extraOuterShadows ??= new List<PanelShadow>();

        /// <summary>Layers behind innerShadow, listed from front to back.</summary>
        public List<PanelShadow> AdditionalInnerShadows => extraInnerShadows ??= new List<PanelShadow>();

        internal IReadOnlyList<PanelShadow> GetExtraShadows(bool outer) => outer ? extraOuterShadows : extraInnerShadows;

        private PanelAtlas atlas;
        private int slot = -1;
        private bool styleDirty = true;
        private bool paddingDirty;
        private bool observersNeedSync = true;
        private List<IPanelStyle> observed;
        private Action<bool> styleChanged;
        private Vector4 meshPadding;
        private bool meshWritesStencil;
        private bool hasWrittenStyle;
        private StyleTransition transition;
        private Vector4 effectPadding;
        private Vector4 pointerPadding;
        private Vector4 heldEffects;
        private Vector4 heldPointer;
        private float blinkEpoch;
        private bool blinkClockRegistered;
        private bool wasBlinkEnabled;

        internal float BlinkEpoch => blinkEpoch;
        internal bool HasLiveBlink => blinkClockRegistered;

        public override Material defaultMaterial => atlas != null ? atlas.Material : base.defaultMaterial;
        public override Texture mainTexture => atlas != null ? atlas.Ramps : Texture2D.whiteTexture;

        protected override void OnEnable()
        {
            base.OnEnable();
            // Graphic registers CanvasUpdateRegistry before the atlas subscribes to the render event.
            atlas = PanelAtlas.Acquire(out slot);
            if (atlas == null) Debug.LogError("UShape shader was not found in Resources.", this);
            EnsureCanvasChannels();
            blinkEpoch = Time.unscaledTime;
            wasBlinkEnabled = blink.enabled;
            styleDirty = true;
            observersNeedSync = true;
            if (atlas != null) SyncObservedStyles();
        }

        protected override void OnDisable()
        {
            base.OnDisable();
            PanelAtlas.CancelStyle(this);
            if (blinkClockRegistered)
            {
                PanelAtlas.ReleaseBlinkClock();
                blinkClockRegistered = false;
            }
            PanelAtlas.StopTransition(this);
            transition?.Cancel();
            hasWrittenStyle = false;
            if (observed != null)
            {
                foreach (var style in observed) style.Changed -= styleChanged;
                observed.Clear();
            }
            atlas?.Release(slot);
            atlas = null;
            slot = -1;
            paddingDirty = false;
        }

        protected override void OnCanvasHierarchyChanged()
        {
            base.OnCanvasHierarchyChanged();
            if (isActiveAndEnabled) EnsureCanvasChannels();
        }

        protected override void OnTransformParentChanged()
        {
            base.OnTransformParentChanged();
            if (isActiveAndEnabled) EnsureCanvasChannels();
        }

        private void EnsureCanvasChannels()
        {
            var target = canvas;
            if (target && (target.additionalShaderChannels & AdditionalCanvasShaderChannels.TexCoord1) == 0)
                target.additionalShaderChannels |= AdditionalCanvasShaderChannels.TexCoord1;
        }

        /// <summary>Sets the same radius on all four corners.</summary>
        public void SetCornerRadius(float radius) => CornerRadii = Vector4.one * Mathf.Max(0, radius);

        internal static bool HasShadow(PanelShadow shadow) =>
            shadow != null && shadow.enabled && shadow.color.a > 0;

        private static int ShadowCount(List<PanelShadow> extra) =>
            1 + Mathf.Min(extra?.Count ?? 0, MaxShadowsPerType - 1);

        private static PanelShadow GetShadow(PanelShadow first, List<PanelShadow> extra, int index)
        {
            if ((uint)index >= (uint)ShadowCount(extra)) throw new ArgumentOutOfRangeException(nameof(index));
            return index == 0 ? first : extra[index - 1];
        }

        private bool AddShadow(ref List<PanelShadow> extra, PanelShadow layer, bool affectsPadding)
        {
            if (layer == null || ShadowCount(extra) >= MaxShadowsPerType) return false;
            (extra ??= new List<PanelShadow>()).Add(layer);
            observersNeedSync = true;
            QueueStyleUpdate(affectsPadding);
            return true;
        }

        private bool RemoveShadow(ref PanelShadow first, List<PanelShadow> extra, PanelShadow layer, bool affectsPadding)
        {
            if (layer == null) return false;
            if (ReferenceEquals(first, layer))
            {
                if (extra != null && extra.Count > 0)
                {
                    first = extra[0];
                    extra.RemoveAt(0);
                }
                else
                {
                    first = new PanelShadow();
                }
            }
            else if (extra == null || !extra.Remove(layer))
            {
                return false;
            }

            observersNeedSync = true;
            QueueStyleUpdate(affectsPadding);
            return true;
        }

        private void SetVisual<T>(ref T field, T value, bool affectsPadding = false)
        {
            if (StyleValue.Set(ref field, value)) QueueStyleUpdate(affectsPadding);
        }

        private void ReplaceStyle<T>(ref T field, T value, bool affectsPadding = false) where T : class, IPanelStyle
        {
            if (ReferenceEquals(field, value)) return;
            field = value;
            observersNeedSync = true;
            QueueStyleUpdate(affectsPadding);
        }

        private void QueueStyleUpdate(bool affectsPadding)
        {
            styleDirty = true;
            paddingDirty |= affectsPadding;
            if (atlas != null) PanelAtlas.QueueStyle(this);
        }

        /// <summary>Applies the target style immediately, including edits to legacy fields.</summary>
        public void Refresh()
        {
            styleDirty = true;
            paddingDirty = true;
            observersNeedSync = true;
            if (atlas == null) return;

            WriteStyle(false, 0d);
            ApplyPaddingUpdate();
        }

        internal void FlushStyleUpdates(double now)
        {
            if (atlas == null) return;
            if (styleDirty) WriteStyle(true, now);
            ApplyPaddingUpdate();
        }

        internal void AdvanceStyleTransition(double now)
        {
            if (atlas == null || transition?.IsRunning != true)
            {
                PanelAtlas.StopTransition(this);
                return;
            }

            if (styleTransitionDuration <= 0 || !Application.isPlaying)
            {
                Refresh();
                return;
            }
            if (atlas.AdvanceStyle(slot, transition, now)) return;

            PanelAtlas.StopTransition(this);
            atlas.WriteStyle(slot, this);
            paddingDirty = true;
            ApplyPaddingUpdate();
        }

        private void ApplyPaddingUpdate()
        {
            if (!paddingDirty) return;
            paddingDirty = false;
            if (!meshPadding.Equals(GetRenderPadding())) SetVerticesDirty();
        }

        private void WriteStyle(bool allowTransition, double now)
        {
            SyncBlinkClock();
            if (observersNeedSync) SyncObservedStyles();
            var nextEffects = paddingDirty || !hasWrittenStyle ? GetEffectPadding() : effectPadding;
            var nextPointer = paddingDirty || !hasWrittenStyle ? GetPointerPadding() : pointerPadding;
            bool wasRunning = transition?.IsRunning == true;
            bool canTransition = allowTransition && hasWrittenStyle && styleTransitionDuration > 0 && Application.isPlaying;
            if (canTransition)
            {
                transition ??= new StyleTransition();
                if (atlas.RetargetStyle(slot, this, transition, now, styleTransitionDuration))
                {
                    // Reserve both endpoint extents. Shrinking can wait until the tween ends.
                    heldEffects = MaxPadding(wasRunning ? heldEffects : effectPadding, nextEffects);
                    heldPointer = MaxPadding(wasRunning ? heldPointer : pointerPadding, nextPointer);
                    PanelAtlas.TrackTransition(this);
                }
                else
                {
                    PanelAtlas.StopTransition(this);
                    paddingDirty |= wasRunning;
                }
            }
            else
            {
                if (wasRunning)
                {
                    PanelAtlas.StopTransition(this);
                    transition.Cancel();
                    paddingDirty = true;
                }
                atlas.WriteStyle(slot, this);
            }

            effectPadding = nextEffects;
            pointerPadding = nextPointer;
            hasWrittenStyle = true;
            styleDirty = false;
        }

        private void SyncBlinkClock()
        {
            if (wasBlinkEnabled != blink.enabled)
            {
                wasBlinkEnabled = blink.enabled;
                blinkEpoch = Time.unscaledTime;
            }

            bool needsClock = Application.isPlaying && blink.IsVisible;
            if (blinkClockRegistered == needsClock) return;
            blinkClockRegistered = needsClock;
            if (needsClock) PanelAtlas.AcquireBlinkClock();
            else PanelAtlas.ReleaseBlinkClock();
        }

        private Vector4 GetRenderPadding() => transition?.IsRunning == true
            ? heldEffects + heldPointer
            : effectPadding + pointerPadding;

        private static Vector4 MaxPadding(Vector4 a, Vector4 b) =>
            new Vector4(Mathf.Max(a.x, b.x), Mathf.Max(a.y, b.y), Mathf.Max(a.z, b.z), Mathf.Max(a.w, b.w));

        private void SyncObservedStyles()
        {
            observersNeedSync = false;
            observed ??= new List<IPanelStyle>(5);
            styleChanged ??= QueueStyleUpdate;
            for (int i = observed.Count - 1; i >= 0; i--)
            {
                var style = observed[i];
                if (UsesStyle(style)) continue;
                style.Changed -= styleChanged;
                observed.RemoveAt(i);
            }

            Observe(fill);
            Observe(border);
            Observe(outline);
            Observe(outerShadow);
            Observe(innerShadow);
            ObserveExtras(extraOuterShadows);
            ObserveExtras(extraInnerShadows);
        }

        private bool UsesStyle(IPanelStyle style) =>
            ReferenceEquals(style, fill) || ReferenceEquals(style, border) || ReferenceEquals(style, outline)
            || ReferenceEquals(style, outerShadow) || ReferenceEquals(style, innerShadow)
            || ExtraContains(extraOuterShadows, style) || ExtraContains(extraInnerShadows, style);

        private static bool ExtraContains(List<PanelShadow> extra, IPanelStyle style)
        {
            int count = ShadowCount(extra) - 1;
            for (int i = 0; i < count; i++)
                if (ReferenceEquals(extra[i], style)) return true;
            return false;
        }

        private void ObserveExtras(List<PanelShadow> extra)
        {
            int count = ShadowCount(extra) - 1;
            for (int i = 0; i < count; i++) Observe(extra[i]);
        }

        private void Observe(IPanelStyle style)
        {
            if (style == null || observed.Contains(style)) return;
            style.Changed += styleChanged;
            observed.Add(style);
        }

        protected override void OnDidApplyAnimationProperties()
        {
            base.OnDidApplyAnimationProperties();
            Refresh();
        }

        private Vector4 GetPointerPadding()
        {
            var shape = Vector4.zero;
            if (!pointer.IsVisible) return shape;

            switch (pointer.side)
            {
                case PointerSide.Left: shape.x = pointer.height; break;
                case PointerSide.Right: shape.y = pointer.height; break;
                case PointerSide.Bottom: shape.z = pointer.height; break;
                case PointerSide.Top: shape.w = pointer.height; break;
            }
            return shape;
        }

        private Vector4 GetEffectPadding()
        {
            float outline = Mathf.Max(0, outlineWidth) + 2;
            var padding = Vector4.one * outline;
            ExpandForShadow(ref padding, outerShadow);
            if (extraOuterShadows != null)
            {
                int limit = Mathf.Min(extraOuterShadows.Count, MaxShadowsPerType - 1);
                for (int i = 0; i < limit; i++) ExpandForShadow(ref padding, extraOuterShadows[i]);
            }
            return padding;
        }

        private static void ExpandForShadow(ref Vector4 padding, PanelShadow layer)
        {
            if (!HasShadow(layer)) return;

            float shadow = layer.spread + Mathf.Max(0, layer.blur) + 2;
            Vector2 offset = layer.offset;
            padding.x = Mathf.Max(padding.x, shadow - offset.x);
            padding.y = Mathf.Max(padding.y, shadow + offset.x);
            padding.z = Mathf.Max(padding.z, shadow - offset.y);
            padding.w = Mathf.Max(padding.w, shadow + offset.y);
        }

        private bool WritesStencil => isMaskingGraphic && atlas != null && material.shader == atlas.Material.shader;

        protected override void UpdateMaterial()
        {
            // uGUI Mask toggles material dirtiness without requesting a mesh rebuild.
            if (meshWritesStencil != WritesStencil) UpdateGeometry();
            base.UpdateMaterial();
        }

        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();
            meshWritesStencil = WritesStencil;
            if (atlas == null) return;
            if (styleDirty) WriteStyle(true, Time.unscaledTimeAsDouble);

            Rect r = GetPixelAdjustedRect();
            if (r.width <= 0 || r.height <= 0) return;
            meshPadding = GetRenderPadding();
            paddingDirty = false;
            var instance = new Vector4(slot, r.width * .5f, r.height * .5f, meshWritesStencil ? 1 : 0);
            float left = -instance.y - meshPadding.x;
            float right = instance.y + meshPadding.y;
            float bottom = -instance.z - meshPadding.z;
            float top = instance.z + meshPadding.w;
            Vector2 origin = r.center + visualOffset + pressOffset;
            Vector2 scale = visualScale * pressScale;
            AddVertex(vh, new Vector2(left, bottom), origin, scale, instance);
            AddVertex(vh, new Vector2(left, top), origin, scale, instance);
            AddVertex(vh, new Vector2(right, top), origin, scale, instance);
            AddVertex(vh, new Vector2(right, bottom), origin, scale, instance);
            vh.AddTriangle(0, 1, 2);
            vh.AddTriangle(2, 3, 0);
            if (meshWritesStencil)
            {
                // Back faces write the silhouette, front faces draw the full style.
                vh.AddTriangle(0, 2, 1);
                vh.AddTriangle(2, 0, 3);
            }
        }

        private void AddVertex(VertexHelper vh, Vector2 local, Vector2 origin, Vector2 scale, Vector4 instance)
        {
            UIVertex vertex = UIVertex.simpleVert;
            vertex.position = origin + Vector2.Scale(local, scale);
            vertex.color = color;
            vertex.uv0 = local;
            vertex.uv1 = instance;
            vh.AddVert(vertex);
        }

#if UNITY_EDITOR
        protected override void OnValidate()
        {
            styleDirty = true;
            paddingDirty = true;
            observersNeedSync = true;
            base.OnValidate();
        }
#endif
    }
}
