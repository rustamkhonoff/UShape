using System;
using System.Collections.Generic;
using UnityEngine;

namespace RustamKhonoff.UShape
{
    [Serializable]
    public struct PanelStroke : IEquatable<PanelStroke>
    {
        public StrokePattern pattern;
        [Min(.1f), Tooltip("Length of a dash in local UI units.")]
        public float dashLength;
        [Min(0), Tooltip("Gap between marks in local UI units.")]
        public float gap;
        [Tooltip("Offset in cycles. One cycle wraps back to the same position.")]
        public float phase;
        public StrokeMotion motion;
        [Tooltip("Cycles per second. Negative values reverse a marching pattern.")]
        public float speed;

        internal static PanelStroke Default => new PanelStroke { dashLength = 12, gap = 8, speed = 1 };

        public bool Equals(PanelStroke other) =>
            pattern == other.pattern && dashLength.Equals(other.dashLength) && gap.Equals(other.gap)
            && phase.Equals(other.phase) && motion == other.motion && speed.Equals(other.speed);

        public override bool Equals(object other) => other is PanelStroke stroke && Equals(stroke);
        public override int GetHashCode() => HashCode.Combine(pattern, dashLength, gap, phase, motion, speed);
    }

    [Serializable]
    public struct PanelPointer : IEquatable<PanelPointer>
    {
        public PointerSide side;
        [Range(0, 1), Tooltip("Position along the edge, from left to right or bottom to top.")]
        public float position;
        [Min(0), Tooltip("Width of the pointer base in local UI units.")]
        public float width;
        [Min(0), Tooltip("Distance the pointer extends past the edge in local UI units.")]
        public float height;

        internal bool IsVisible =>
            side >= PointerSide.Top && side <= PointerSide.Left && width > 0 && height > 0;

        public bool Equals(PanelPointer other) =>
            side == other.side && position.Equals(other.position) && width.Equals(other.width) && height.Equals(other.height);

        public override bool Equals(object other) => other is PanelPointer pointer && Equals(pointer);
        public override int GetHashCode() => HashCode.Combine(side, position, width, height);
    }

    [Serializable]
    public struct PanelBlink : IEquatable<PanelBlink>
    {
        public bool enabled;
        [Min(0), Tooltip("Width of the bright core, in local UI units.")]
        public float width;
        [Min(0), Tooltip("Soft falloff on each side, in local UI units.")]
        public float blur;
        [Tooltip("Highlight color and opacity.")]
        public Color color;
        [Tooltip("Travel direction in degrees, counter-clockwise from right.")]
        public float angle;
        [Min(.01f), Tooltip("Duration of one sweep, in seconds.")]
        public float duration;
        [Min(0), Tooltip("Pause after each sweep, in seconds.")]
        public float interval;
        [Min(0), Tooltip("One-time delay before the first sweep, in seconds.")]
        public float initialDelay;

        internal static PanelBlink Default => new PanelBlink
        {
            width = 20, blur = 16, color = new Color(1, 1, 1, .5f),
            angle = 25, duration = .65f, interval = 2, initialDelay = .35f
        };

        internal bool IsVisible => enabled && width > 0 && color.a > 0;

        public bool Equals(PanelBlink other) =>
            enabled == other.enabled && width.Equals(other.width) && blur.Equals(other.blur)
            && color.Equals(other.color) && angle.Equals(other.angle) && duration.Equals(other.duration)
            && interval.Equals(other.interval) && initialDelay.Equals(other.initialDelay);

        public override bool Equals(object other) => other is PanelBlink blink && Equals(blink);
        public override int GetHashCode() => HashCode.Combine(enabled, width, blur, color, angle, duration, interval, initialDelay);
    }

    internal interface IPanelStyle
    {
        event Action<bool> Changed;
    }

    internal static class StyleValue
    {
        public static bool Set<T>(ref T field, T value)
        {
            if (EqualityComparer<T>.Default.Equals(field, value)) return false;
            field = value;
            return true;
        }
    }

    [Serializable]
    public sealed class PanelPaint : IPanelStyle
    {
        public GradientType type = GradientType.Solid;
        public Color color = Color.white;
        public Gradient gradient = new Gradient();
        [Tooltip("Angle in degrees, counter-clockwise. Linear 0 points right.")]
        public float angle = 90;
        [Tooltip("Center in normalized panel coordinates. (0.5, 0.5) is the center.")]
        public Vector2 center = new Vector2(.5f, .5f);
        [Tooltip("Gradient size relative to the panel. Values must be positive.")]
        public Vector2 scale = Vector2.one;
        [Tooltip("Shifts the color stops along the gradient.")]
        public float offset;

        private event Action<bool> changed;
        event Action<bool> IPanelStyle.Changed { add => changed += value; remove => changed -= value; }

        public GradientType Type { get => type; set => Set(ref type, value); }
        public Color Color { get => color; set => Set(ref color, value); }
        public float Angle { get => angle; set => Set(ref angle, value); }
        public Vector2 Center { get => center; set => Set(ref center, value); }
        public Vector2 Scale { get => scale; set => Set(ref scale, value); }
        public float Offset { get => offset; set => Set(ref offset, value); }

        /// <summary>Assign a gradient or use SetGradientKeys to edit its keys with automatic refresh.</summary>
        public Gradient Gradient
        {
            get => gradient;
            set
            {
                gradient = value;
                changed?.Invoke(false);
            }
        }

        public void SetGradientKeys(GradientColorKey[] colors, GradientAlphaKey[] alphas)
        {
            gradient ??= new Gradient();
            gradient.SetKeys(colors, alphas);
            changed?.Invoke(false);
        }

        private void Set<T>(ref T field, T value)
        {
            if (StyleValue.Set(ref field, value)) changed?.Invoke(false);
        }
    }

    [Serializable]
    public sealed class PanelShadow : IPanelStyle
    {
        public bool enabled;
        public Color color = new Color(0, 0, 0, .35f);
        public Vector2 offset = new Vector2(0, -4);
        [Min(0)] public float blur = 8;
        public float spread;

        private event Action<bool> changed;
        event Action<bool> IPanelStyle.Changed { add => changed += value; remove => changed -= value; }

        public bool Enabled { get => enabled; set => Set(ref enabled, value); }
        public Color Color { get => color; set => Set(ref color, value); }
        public Vector2 Offset { get => offset; set => Set(ref offset, value); }
        public float Blur { get => blur; set => Set(ref blur, Mathf.Max(0, value)); }
        public float Spread { get => spread; set => Set(ref spread, value); }

        private void Set<T>(ref T field, T value)
        {
            if (StyleValue.Set(ref field, value)) changed?.Invoke(true);
        }
    }
}
