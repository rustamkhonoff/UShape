using Unity.Collections;
using UnityEngine;

namespace RustamKhonoff.UShape
{
    // Holds one target, regardless of how often the caller changes properties.
    internal sealed class StyleTransition
    {
        internal const int RowWidth = 32;
        private const int Widths = 1;
        private const int OuterStart = 11;
        private const int InnerStart = 19;
        private const int Pointer = 27;
        private const int InnerReach = 28;
        private const int BorderStroke = 29;
        private const int OutlineStroke = 30;
        private const float SettleRate = 4.6051702f; // 99% of the distance over the configured duration.

        internal readonly Color[] Target = new Color[RowWidth];
        private uint moving;
        private double lastTick;
        private double settleAt;
        private float seconds;

        internal bool IsRunning => moving != 0;

        internal bool Retarget(NativeArray<Color> data, int start, double now, float duration)
        {
            var live = data[start + Widths];
            var desired = Target[Widths];
            int liveFlags = (int)live.b, targetFlags = (int)desired.b;
            int liveShadows = (int)live.a, targetShadows = (int)desired.a;

            uint blend = Bit(0) | Bit(Widths) | Bit(2) | Bit(5) | Bit(8);
            for (int paint = 3; paint <= 9; paint += 3)
                if (Target[paint].r > 0 && Target[paint].r == data[start + paint].r)
                    blend |= Bit(paint) | Bit(paint + 1);

            if ((liveShadows & 7) == (targetShadows & 7))
                blend |= ShadowMask(OuterStart, targetShadows & 7);
            if ((targetShadows >> 3) != 0 && (liveShadows >> 3) == (targetShadows >> 3))
                blend |= ShadowMask(InnerStart, targetShadows >> 3) | Bit(InnerReach);

            if ((liveFlags & targetFlags & 16) != 0 && data[start + Pointer].r == Target[Pointer].r)
                blend |= Bit(Pointer);
            if ((liveFlags & (15 << 5)) == (targetFlags & (15 << 5))) blend |= Bit(BorderStroke);
            if ((liveFlags & (15 << 9)) == (targetFlags & (15 << 9))) blend |= Bit(OutlineStroke);

            bool dirty = false;
            // Flags and shadow counts are discrete. Preserve only the two live widths.
            dirty |= Set(data, start + Widths, new Color(live.r, live.g, desired.b, desired.a));
            // Stroke speed is a discrete shader clock setting.
            dirty |= SetStrokeSpeed(data, start, BorderStroke);
            dirty |= SetStrokeSpeed(data, start, OutlineStroke);

            uint next = 0;
            for (int i = 0; i < RowWidth; i++)
            {
                int index = start + i;
                if ((blend & Bit(i)) == 0)
                    dirty |= Set(data, index, Target[i]);
                else if (!data[index].Equals(Target[i]))
                    next |= Bit(i);
            }

            if (!IsRunning) lastTick = now;
            moving = next;
            seconds = duration;
            settleAt = now + duration;
            return dirty;
        }

        internal bool Step(NativeArray<Color> data, int start, double now)
        {
            if (!IsRunning || now <= lastTick) return false;
            float weight = now >= settleAt ? 1f : 1f - Mathf.Exp(-SettleRate * (float)(now - lastTick) / seconds);
            lastTick = now;
            bool finished = weight >= 1f;
            bool dirty = false;
            for (int i = 0; i < RowWidth; i++)
            {
                if ((moving & Bit(i)) == 0) continue;
                int index = start + i;
                Color value = finished ? Target[i] : Interpolate(i, data[index], Target[i], weight);
                dirty |= Set(data, index, value);
            }
            if (finished) moving = 0;
            return dirty;
        }

        internal void Cancel() => moving = 0;

        private bool SetStrokeSpeed(NativeArray<Color> data, int start, int entry)
        {
            var live = data[start + entry];
            live.a = Target[entry].a;
            return Set(data, start + entry, live);
        }

        private static Color Interpolate(int entry, Color current, Color target, float amount)
        {
            Color value = Color.LerpUnclamped(current, target, amount);
            if (entry == 3 || entry == 6 || entry == 9)
            {
                if (!current.g.Equals(target.g) || !current.b.Equals(target.b))
                {
                    float from = Mathf.Atan2(current.b, current.g) * Mathf.Rad2Deg;
                    float to = Mathf.Atan2(target.b, target.g) * Mathf.Rad2Deg;
                    float angle = Mathf.LerpAngle(from, to, amount) * Mathf.Deg2Rad;
                    value.g = Mathf.Cos(angle);
                    value.b = Mathf.Sin(angle);
                }
            }
            else if (entry == BorderStroke || entry == OutlineStroke)
            {
                value.b = Mathf.LerpAngle(current.b * 360f, target.b * 360f, amount) / 360f;
            }
            return value;
        }

        private static uint Bit(int index) => 1u << index;
        private static uint ShadowMask(int start, int count) => ((1u << (count * 2)) - 1u) << start;

        private static bool Set(NativeArray<Color> data, int index, Color value)
        {
            if (data[index].Equals(value)) return false;
            data[index] = value;
            return true;
        }
    }
}
