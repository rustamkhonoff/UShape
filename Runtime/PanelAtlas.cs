using System.Collections.Generic;
using Unity.Collections;
using UnityEngine;

namespace RustamKhonoff.UShape
{
    // A page owns immutable material bindings. StencilMaterial can safely cache copies.
    internal sealed class PanelAtlas
    {
        private const int Capacity = 64;
        private const int StyleWidth = 32;
        private const int OuterShadowStart = 11;
        private const int InnerShadowStart = OuterShadowStart + Shape.MaxShadowsPerType * 2;
        private const int RampWidth = 256;
        private const int PointerEntry = InnerShadowStart + Shape.MaxShadowsPerType * 2;
        private const int InnerReachEntry = PointerEntry + 1;
        private const int BorderStrokeEntry = InnerReachEntry + 1;
        private const int OutlineStrokeEntry = BorderStrokeEntry + 1;
        private const int PaintCount = 3;

        private static readonly List<PanelAtlas> Pages = new List<PanelAtlas>();
        private static readonly HashSet<PanelAtlas> DirtyPages = new HashSet<PanelAtlas>();
        private static HashSet<Shape> pendingStyles = new HashSet<Shape>();
        private static HashSet<Shape> processingStyles = new HashSet<Shape>();
        private static bool flushingStyles;
        private static Shader shader;

        private readonly Stack<int> freeSlots = new Stack<int>(Capacity);
        private readonly Texture2D styles;
        private readonly Texture2D ramps;
        private readonly Gradient[] rampSnapshots = new Gradient[Capacity * PaintCount];
        private bool stylesDirty;
        private bool rampsDirty;

        public readonly Material Material;
        public Texture Ramps => ramps;

        private PanelAtlas()
        {
            for (int slot = Capacity - 1; slot >= 0; slot--) freeSlots.Push(slot);

            styles = new Texture2D(StyleWidth, Capacity, TextureFormat.RGBAFloat, false, true)
            {
                name = "UShape Styles",
                hideFlags = HideFlags.HideAndDontSave,
                filterMode = FilterMode.Point,
                wrapMode = TextureWrapMode.Clamp
            };
            ramps = new Texture2D(RampWidth, Capacity * PaintCount, TextureFormat.RGBA32, false)
            {
                name = "UShape Gradients",
                hideFlags = HideFlags.HideAndDontSave,
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp
            };
            Material = new Material(shader) { name = "UShape Batch", hideFlags = HideFlags.HideAndDontSave };
            Material.SetTexture("_StyleTex", styles);
            Material.SetTexture("_MainTex", ramps);
        }

        public static PanelAtlas Acquire(out int slot)
        {
            slot = -1;
            if (!shader) shader = Resources.Load<Shader>("RustamKhonoffUShape");
            if (!shader) return null;

            foreach (var page in Pages)
            {
                if (page.freeSlots.Count == 0) continue;
                slot = page.freeSlots.Pop();
                return page;
            }

            if (Pages.Count == 0)
            {
                Canvas.preWillRenderCanvases += FlushStyles;
                Canvas.willRenderCanvases += UploadDirtyPages;
            }
            var newPage = new PanelAtlas();
            Pages.Add(newPage);
            slot = newPage.freeSlots.Pop();
            return newPage;
        }

        public void Release(int slot)
        {
            freeSlots.Push(slot);
            if (freeSlots.Count != Capacity) return;

            Pages.Remove(this);
            DirtyPages.Remove(this);
            DestroyResource(Material);
            DestroyResource(styles);
            DestroyResource(ramps);
            if (Pages.Count == 0)
            {
                Canvas.preWillRenderCanvases -= FlushStyles;
                Canvas.willRenderCanvases -= UploadDirtyPages;
            }
        }

        public static void QueueStyle(Shape element) => pendingStyles.Add(element);
        public static void CancelStyle(Shape element) => pendingStyles.Remove(element);

        private static void FlushStyles()
        {
            if (flushingStyles || pendingStyles.Count == 0) return;
            // Padding changes must reach Graphic before the canvas rebuilds. A dirty callback
            // can schedule another element, so flush a separate set.
            var batch = pendingStyles;
            pendingStyles = processingStyles;
            processingStyles = batch;
            flushingStyles = true;
            try
            {
                foreach (var element in batch)
                    if (element) element.FlushStyleUpdates();
            }
            finally
            {
                batch.Clear();
                flushingStyles = false;
            }
        }

        public void WriteStyle(int slot, Shape element)
        {
            var data = styles.GetRawTextureData<Color>();
            int start = slot * StyleWidth;
            var r = element.cornerRadii;
            var radii = new Color(Mathf.Max(0, r.x), Mathf.Max(0, r.y), Mathf.Max(0, r.z), Mathf.Max(0, r.w));
            var fillGradient = GetGradient(element.fill);
            bool hasPointer = element.pointer.IsVisible;
            int flags = (fillGradient != null ? 4 : 0)
                | (radii.r == radii.g && radii.r == radii.b && radii.r == radii.a ? 8 : 0)
                | (hasPointer ? 16 : 0);

            // Four bits per stroke: two for pattern, two for motion.
            flags |= EncodeStroke(element.borderWidth, element.borderStroke, data, start + BorderStrokeEntry) << 5;
            flags |= EncodeStroke(element.outlineWidth, element.outlineStroke, data, start + OutlineStrokeEntry) << 9;

            int outerCount = WriteShadows(data, start + OuterShadowStart, element.outerShadow, element.GetExtraShadows(true), false, out _);
            int innerCount = WriteShadows(data, start + InnerShadowStart, element.innerShadow, element.GetExtraShadows(false), true, out Vector2 innerReach);
            if (innerCount > 0)
                SetStyle(data, start + InnerReachEntry, new Color(innerReach.x, innerReach.y, 0, 0));

            if (hasPointer)
            {
                var pointer = element.pointer;
                SetStyle(data, start + PointerEntry, new Color((float)pointer.side, Mathf.Clamp01(pointer.position), pointer.width, pointer.height));
            }
            // Each count fits in three bits. The shader only visits enabled and visible layers.
            int shadowCounts = outerCount | (innerCount << 3);

            SetStyle(data, start, radii);
            SetStyle(data, start + 1, new Color(Mathf.Max(0, element.borderWidth), Mathf.Max(0, element.outlineWidth), flags, shadowCounts));
            WritePaint(data, start + 2, slot, 0, element.fill, fillGradient);
            WritePaint(data, start + 5, slot, 1, element.border, GetGradient(element.border));
            WritePaint(data, start + 8, slot, 2, element.outline, GetGradient(element.outline));
            if (stylesDirty || rampsDirty) DirtyPages.Add(this);
        }

        private int EncodeStroke(float width, PanelStroke stroke, NativeArray<Color> data, int entry)
        {
            if (width <= 0) return 0;
            var motion = stroke.motion == StrokeMotion.March && stroke.pattern == StrokePattern.Solid
                ? StrokeMotion.None
                : stroke.motion;
            int mode = (int)stroke.pattern | ((int)motion << 2);
            if (mode != 0)
                SetStyle(data, entry, new Color(Mathf.Max(.1f, stroke.dashLength), Mathf.Max(0, stroke.gap), stroke.phase, stroke.speed));
            return mode;
        }

        private static Gradient GetGradient(PanelPaint paint) =>
            paint != null && paint.type != GradientType.Solid ? paint.gradient : null;

        private void SetStyle(NativeArray<Color> data, int offset, Color value)
        {
            if (data[offset].Equals(value)) return;
            data[offset] = value;
            stylesDirty = true;
        }

        private void WritePaint(NativeArray<Color> data, int index, int slot, int layer,
            PanelPaint paint, Gradient gradient)
        {
            SetStyle(data, index, ToWorkingColorSpace(paint?.color ?? Color.white));
            if (gradient == null)
            {
                SetStyle(data, index + 1, Color.clear);
                return;
            }

            float angle = paint.angle * Mathf.Deg2Rad;
            float type = (float)paint.type + (gradient.mode == GradientMode.Fixed ? 8 : 0);
            SetStyle(data, index + 1, new Color(type, Mathf.Cos(angle), Mathf.Sin(angle), paint.offset));
            SetStyle(data, index + 2, new Color(paint.center.x, paint.center.y,
                Mathf.Max(.001f, Mathf.Abs(paint.scale.x)), Mathf.Max(.001f, Mathf.Abs(paint.scale.y))));

            int row = slot * PaintCount + layer;
            var snapshot = rampSnapshots[row];
            if (snapshot != null && snapshot.Equals(gradient)) return;

            var pixels = ramps.GetRawTextureData<Color32>();
            int firstPixel = row * RampWidth;
            bool linearKeys = gradient.colorSpace == ColorSpace.Linear;
            for (int x = 0; x < RampWidth; x++)
            {
                Color value = gradient.Evaluate(x / (RampWidth - 1f));
                // The ramp stores sRGB so the GPU can decode it in linear projects.
                pixels[firstPixel + x] = linearKeys ? value.gamma : value;
            }

            if (snapshot == null) rampSnapshots[row] = snapshot = new Gradient();
            snapshot.SetKeys(gradient.colorKeys, gradient.alphaKeys);
            snapshot.mode = gradient.mode;
            snapshot.colorSpace = gradient.colorSpace;
            rampsDirty = true;
        }

        private int WriteShadows(NativeArray<Color> data, int start, PanelShadow first, IReadOnlyList<PanelShadow> extra,
            bool trackInnerReach, out Vector2 reach)
        {
            int count = 0;
            reach = Vector2.zero;
            if (Shape.HasShadow(first))
            {
                WriteShadow(data, start, first);
                if (trackInnerReach) IncludeInnerReach(ref reach, first);
                count++;
            }

            if (extra == null) return count;

            int limit = Mathf.Min(extra.Count, Shape.MaxShadowsPerType - 1);
            for (int i = 0; i < limit; i++)
            {
                var layer = extra[i];
                if (!Shape.HasShadow(layer)) continue;
                WriteShadow(data, start + count * 2, layer);
                if (trackInnerReach) IncludeInnerReach(ref reach, layer);
                count++;
            }
            return count;
        }

        private static void IncludeInnerReach(ref Vector2 reach, PanelShadow shadow)
        {
            float softnessAndSpread = Mathf.Max(0, shadow.blur) + Mathf.Max(0, shadow.spread);
            reach.x = Mathf.Max(reach.x, Mathf.Abs(shadow.offset.x) + softnessAndSpread);
            reach.y = Mathf.Max(reach.y, Mathf.Abs(shadow.offset.y) + softnessAndSpread);
        }

        private void WriteShadow(NativeArray<Color> data, int index, PanelShadow shadow)
        {
            SetStyle(data, index, ToWorkingColorSpace(shadow.color));
            SetStyle(data, index + 1, new Color(shadow.offset.x, shadow.offset.y, Mathf.Max(0, shadow.blur), shadow.spread));
        }

        private static Color ToWorkingColorSpace(Color color) =>
            QualitySettings.activeColorSpace == ColorSpace.Linear ? color.linear : color;

        private static void UploadDirtyPages()
        {
            foreach (var page in DirtyPages)
            {
                if (page.stylesDirty) page.styles.Apply(false, false);
                if (page.rampsDirty) page.ramps.Apply(false, false);
                page.stylesDirty = false;
                page.rampsDirty = false;
            }
            DirtyPages.Clear();
        }

        private static void DestroyResource(Object resource)
        {
            if (Application.isPlaying) Object.Destroy(resource);
            else Object.DestroyImmediate(resource);
        }
    }
}
