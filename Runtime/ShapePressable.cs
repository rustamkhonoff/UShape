using System;
using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace RustamKhonoff.UShape
{
    [Serializable]
    public struct ShapePressValue<T>
    {
        public bool enabled;
        public T value;
    }

    [Serializable]
    public struct ShapePressStyle
    {
        [Tooltip("Multiplier relative to the original local scale. One keeps the original size.")]
        public ShapePressValue<float> scale;
        [Tooltip("Offset from the original anchored position, in UI units.")]
        public ShapePressValue<Vector2> offset;
        public ShapePressValue<Color> fillColor;
        public ShapePressValue<float> fillAngle;
        public ShapePressValue<float> fillOffset;
        public ShapePressValue<Color> borderColor;
        public ShapePressValue<Color> outlineColor;
        public ShapePressValue<Color> outerShadowColor;
        public ShapePressValue<Vector2> outerShadowOffset;
        public ShapePressValue<float> outerShadowBlur;
        public ShapePressValue<float> outerShadowSpread;
        public ShapePressValue<Color> innerShadowColor;
        public ShapePressValue<Vector2> innerShadowOffset;
        public ShapePressValue<float> innerShadowBlur;
        public ShapePressValue<float> innerShadowSpread;
        public ShapePressValue<float> borderWidth;
        public ShapePressValue<float> outlineWidth;
        public ShapePressValue<float> borderPhase;
        public ShapePressValue<Vector4> cornerRadii;
        public ShapePressValue<float> pointerPosition;

        internal void Capture(Shape shape)
        {
            fillColor.value = shape.Fill?.Color ?? default;
            fillAngle.value = shape.Fill?.Angle ?? default;
            fillOffset.value = shape.Fill?.Offset ?? default;
            borderColor.value = shape.Border?.Color ?? default;
            outlineColor.value = shape.Outline?.Color ?? default;
            outerShadowColor.value = shape.OuterShadow?.Color ?? default;
            outerShadowOffset.value = shape.OuterShadow?.Offset ?? default;
            outerShadowBlur.value = shape.OuterShadow?.Blur ?? default;
            outerShadowSpread.value = shape.OuterShadow?.Spread ?? default;
            innerShadowColor.value = shape.InnerShadow?.Color ?? default;
            innerShadowOffset.value = shape.InnerShadow?.Offset ?? default;
            innerShadowBlur.value = shape.InnerShadow?.Blur ?? default;
            innerShadowSpread.value = shape.InnerShadow?.Spread ?? default;
            borderWidth.value = shape.BorderWidth;
            outlineWidth.value = shape.OutlineWidth;
            borderPhase.value = shape.BorderPhase;
            cornerRadii.value = shape.CornerRadii;
            pointerPosition.value = shape.PointerPosition;
        }

        internal void Apply(Shape shape, in ShapePressStyle hovered, in ShapePressStyle pressed, bool hover, bool press)
        {
            if (shape.Fill != null && Resolve(hovered.fillColor, pressed.fillColor, fillColor.value, hover, press, out var fillColorValue))
                shape.Fill.Color = fillColorValue;
            if (shape.Fill != null && Resolve(hovered.fillAngle, pressed.fillAngle, fillAngle.value, hover, press, out var fillAngleValue))
                shape.Fill.Angle = fillAngleValue;
            if (shape.Fill != null && Resolve(hovered.fillOffset, pressed.fillOffset, fillOffset.value, hover, press, out var fillOffsetValue))
                shape.Fill.Offset = fillOffsetValue;
            if (shape.Border != null && Resolve(hovered.borderColor, pressed.borderColor, borderColor.value, hover, press, out var borderColorValue))
                shape.Border.Color = borderColorValue;
            if (shape.Outline != null && Resolve(hovered.outlineColor, pressed.outlineColor, outlineColor.value, hover, press, out var outlineColorValue))
                shape.Outline.Color = outlineColorValue;
            if (shape.OuterShadow != null && Resolve(hovered.outerShadowColor, pressed.outerShadowColor, outerShadowColor.value, hover, press, out var outerShadowColorValue))
                shape.OuterShadow.Color = outerShadowColorValue;
            if (shape.OuterShadow != null && Resolve(hovered.outerShadowOffset, pressed.outerShadowOffset, outerShadowOffset.value, hover, press, out var outerShadowOffsetValue))
                shape.OuterShadow.Offset = outerShadowOffsetValue;
            if (shape.OuterShadow != null && Resolve(hovered.outerShadowBlur, pressed.outerShadowBlur, outerShadowBlur.value, hover, press, out var outerShadowBlurValue))
                shape.OuterShadow.Blur = outerShadowBlurValue;
            if (shape.OuterShadow != null && Resolve(hovered.outerShadowSpread, pressed.outerShadowSpread, outerShadowSpread.value, hover, press, out var outerShadowSpreadValue))
                shape.OuterShadow.Spread = outerShadowSpreadValue;
            if (shape.InnerShadow != null && Resolve(hovered.innerShadowColor, pressed.innerShadowColor, innerShadowColor.value, hover, press, out var innerShadowColorValue))
                shape.InnerShadow.Color = innerShadowColorValue;
            if (shape.InnerShadow != null && Resolve(hovered.innerShadowOffset, pressed.innerShadowOffset, innerShadowOffset.value, hover, press, out var innerShadowOffsetValue))
                shape.InnerShadow.Offset = innerShadowOffsetValue;
            if (shape.InnerShadow != null && Resolve(hovered.innerShadowBlur, pressed.innerShadowBlur, innerShadowBlur.value, hover, press, out var innerShadowBlurValue))
                shape.InnerShadow.Blur = innerShadowBlurValue;
            if (shape.InnerShadow != null && Resolve(hovered.innerShadowSpread, pressed.innerShadowSpread, innerShadowSpread.value, hover, press, out var innerShadowSpreadValue))
                shape.InnerShadow.Spread = innerShadowSpreadValue;
            if (Resolve(hovered.borderWidth, pressed.borderWidth, borderWidth.value, hover, press, out var borderWidthValue))
                shape.BorderWidth = borderWidthValue;
            if (Resolve(hovered.outlineWidth, pressed.outlineWidth, outlineWidth.value, hover, press, out var outlineWidthValue))
                shape.OutlineWidth = outlineWidthValue;
            if (Resolve(hovered.borderPhase, pressed.borderPhase, borderPhase.value, hover, press, out var borderPhaseValue))
                shape.BorderPhase = borderPhaseValue;
            if (Resolve(hovered.cornerRadii, pressed.cornerRadii, cornerRadii.value, hover, press, out var cornerRadiiValue))
                shape.CornerRadii = cornerRadiiValue;
            if (Resolve(hovered.pointerPosition, pressed.pointerPosition, pointerPosition.value, hover, press, out var pointerPositionValue))
                shape.PointerPosition = pointerPositionValue;
        }

        internal static bool Resolve<T>(ShapePressValue<T> hover, ShapePressValue<T> press, T normal,
            bool isHovered, bool isPressed, out T value)
        {
            value = isPressed && press.enabled ? press.value : isHovered && hover.enabled ? hover.value : normal;
            return hover.enabled || press.enabled;
        }
    }

    [DisallowMultipleComponent]
    [AddComponentMenu("UI/Shape Pressable")]
    public sealed class ShapePressable : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler,
        IPointerDownHandler, IPointerUpHandler, ICancelHandler
    {
        [Tooltip("Defaults to the Shape on this GameObject.")]
        public Shape target;
        [Tooltip("Optional visual root for scale and offset. Defaults to Target's RectTransform.")]
        public RectTransform visualRoot;
        public ShapePressStyle hovered;
        [Tooltip("Unset fields inherit Hovered.")]
        public ShapePressStyle pressed;

        private Selectable selectable;
        private Shape activeTarget;
        private ShapePressStyle normal;
        private int? pressedPointer;
        private bool inside;
        private RectTransform animatedRoot;
        private Coroutine motion;
        private Vector3 normalScale;
        private Vector2 normalPosition;
        private Vector3 goalScale;
        private Vector2 goalPosition;
        private bool controlsScale, controlsPosition, restoringTransform;
        private float motionDuration;
        private double finishAt;

        private void OnEnable()
        {
            if (!target) TryGetComponent(out target);
            TryGetComponent(out selectable);
        }

        private void OnDisable()
        {
            ResetInteraction();
            ClearTransform();
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            if (pressedPointer.HasValue && pressedPointer != eventData.pointerId) return;
            inside = true;
            Apply();
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            if (pressedPointer.HasValue && pressedPointer != eventData.pointerId) return;
            inside = false;
            Apply();
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            if (eventData.button != PointerEventData.InputButton.Left || pressedPointer.HasValue || !CanInteract()) return;
            pressedPointer = eventData.pointerId;
            inside = true;
            Apply();
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            if (eventData.button != PointerEventData.InputButton.Left || pressedPointer != eventData.pointerId) return;
            pressedPointer = null;
            Apply();
        }

        public void OnCancel(BaseEventData eventData) => ResetInteraction();

        private bool CanInteract() => isActiveAndEnabled && (!selectable || selectable.IsActive() && selectable.IsInteractable());

        private void Apply()
        {
            bool interacting = inside && target && CanInteract();
            if (activeTarget != target || !interacting) Restore();
            ApplyTransform(interacting);
            if (!interacting) return;
            if (!activeTarget)
            {
                activeTarget = target;
                normal.Capture(activeTarget);
            }
            normal.Apply(activeTarget, hovered, pressed, true, pressedPointer.HasValue);
        }

        private void ResetInteraction()
        {
            pressedPointer = null;
            inside = false;
            Restore();
            if (isActiveAndEnabled) ApplyTransform(false);
        }

        private void ApplyTransform(bool interacting)
        {
            var root = visualRoot ? visualRoot : target ? target.rectTransform : null;
            if (animatedRoot && animatedRoot != root) ClearTransform();
            if (!animatedRoot)
            {
                if (!interacting || !root) return;
                controlsScale = hovered.scale.enabled || pressed.scale.enabled;
                controlsPosition = hovered.offset.enabled || pressed.offset.enabled;
                if (!controlsScale && !controlsPosition) return;
                animatedRoot = root;
                normalScale = root.localScale;
                normalPosition = root.anchoredPosition;
            }

            restoringTransform = !interacting;
            bool press = interacting && pressedPointer.HasValue;
            ShapePressStyle.Resolve(hovered.scale, pressed.scale, 1f, interacting, press, out float scale);
            ShapePressStyle.Resolve(hovered.offset, pressed.offset, Vector2.zero, interacting, press, out Vector2 offset);
            goalScale = normalScale * Mathf.Max(0f, scale);
            goalPosition = normalPosition + offset;
            motionDuration = target ? target.StyleTransitionDuration : 0f;
            if (motionDuration <= 0f || AtGoal())
            {
                if (motion != null) StopCoroutine(motion);
                motion = null;
                ApplyGoal();
                return;
            }

            finishAt = Time.unscaledTimeAsDouble + motionDuration;
            if (motion == null) motion = StartCoroutine(AnimateTransform());
        }

        private bool AtGoal() =>
            (!controlsScale || animatedRoot.localScale == goalScale)
            && (!controlsPosition || animatedRoot.anchoredPosition == goalPosition);

        private IEnumerator AnimateTransform()
        {
            while (animatedRoot)
            {
                yield return null;
                if (!animatedRoot || Time.unscaledTimeAsDouble >= finishAt) break;
                float step = 1f - Mathf.Exp(-4.60517f * Time.unscaledDeltaTime / motionDuration);
                if (controlsScale) animatedRoot.localScale = Vector3.LerpUnclamped(animatedRoot.localScale, goalScale, step);
                if (controlsPosition) animatedRoot.anchoredPosition = Vector2.LerpUnclamped(animatedRoot.anchoredPosition, goalPosition, step);
            }
            motion = null;
            ApplyGoal();
        }

        private void ApplyGoal()
        {
            if (!animatedRoot) return;
            if (controlsScale) animatedRoot.localScale = goalScale;
            if (controlsPosition) animatedRoot.anchoredPosition = goalPosition;
            if (restoringTransform) animatedRoot = null;
        }

        private void ClearTransform()
        {
            if (motion != null) StopCoroutine(motion);
            motion = null;
            if (animatedRoot)
            {
                if (controlsScale) animatedRoot.localScale = normalScale;
                if (controlsPosition) animatedRoot.anchoredPosition = normalPosition;
            }
            animatedRoot = null;
        }

        private void Restore()
        {
            if (activeTarget) normal.Apply(activeTarget, hovered, pressed, false, false);
            activeTarget = null;
        }
    }
}
