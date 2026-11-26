using System;
using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Serialization;
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
        [Tooltip("Multiplier relative to the Shape scale. One keeps the original size.")]
        public ShapePressValue<float> scale;
        [FormerlySerializedAs("offset"), Tooltip("Additional offset of the drawn Shape, in UI units.")]
        public ShapePressValue<Vector2> visualOffset;
        [Tooltip("Additional anchored position offset of the Shape's RectTransform and its children.")]
        public ShapePressValue<Vector2> rectOffset;
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
        public ShapePressStyle hovered;
        [Tooltip("Unset fields inherit Hovered.")]
        public ShapePressStyle pressed;

        private Selectable selectable;
        private Shape activeTarget;
        private ShapePressStyle normal;
        private int? pressedPointer;
        private bool inside;
        private Shape animatedShape;
        private Coroutine motion;
        private float currentScale = 1f, goalScale = 1f;
        private Vector2 currentVisualOffset, goalVisualOffset;
        private Vector2 currentRectOffset, goalRectOffset;
        private Vector2 rectBasePosition, lastRectPosition;
        private bool restoringTransform;
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
            if (!ReferenceEquals(animatedShape, null) && (!animatedShape || animatedShape != target))
                ClearTransform();
            if (!animatedShape)
            {
                if (!interacting || !target) return;
                if (!hovered.scale.enabled && !pressed.scale.enabled &&
                    !hovered.visualOffset.enabled && !pressed.visualOffset.enabled &&
                    !hovered.rectOffset.enabled && !pressed.rectOffset.enabled) return;
                animatedShape = target;
            }

            restoringTransform = !interacting;
            bool press = interacting && pressedPointer.HasValue;
            ShapePressStyle.Resolve(hovered.scale, pressed.scale, 1f, interacting, press, out float scale);
            ShapePressStyle.Resolve(hovered.visualOffset, pressed.visualOffset, Vector2.zero, interacting, press, out goalVisualOffset);
            ShapePressStyle.Resolve(hovered.rectOffset, pressed.rectOffset, Vector2.zero, interacting, press, out goalRectOffset);
            goalScale = Mathf.Max(0f, scale);
            motionDuration = animatedShape.StyleTransitionDuration;
            if (motionDuration <= 0f || (currentScale.Equals(goalScale) &&
                currentVisualOffset.Equals(goalVisualOffset) && currentRectOffset.Equals(goalRectOffset)))
            {
                if (motion != null) StopCoroutine(motion);
                motion = null;
                ApplyGoal();
                return;
            }

            finishAt = Time.unscaledTimeAsDouble + motionDuration;
            if (motion == null) motion = StartCoroutine(AnimateTransform());
        }

        private IEnumerator AnimateTransform()
        {
            while (animatedShape)
            {
                yield return null;
                if (!animatedShape || Time.unscaledTimeAsDouble >= finishAt) break;
                float step = 1f - Mathf.Exp(-4.60517f * Time.unscaledDeltaTime / motionDuration);
                currentScale = Mathf.LerpUnclamped(currentScale, goalScale, step);
                currentVisualOffset = Vector2.LerpUnclamped(currentVisualOffset, goalVisualOffset, step);
                animatedShape.SetPressTransform(currentScale, currentVisualOffset);
                ApplyRectOffset(Vector2.LerpUnclamped(currentRectOffset, goalRectOffset, step));
            }
            motion = null;
            ApplyGoal();
        }

        private void ApplyGoal()
        {
            if (!animatedShape)
            {
                ClearTransform();
                return;
            }
            currentScale = goalScale;
            currentVisualOffset = goalVisualOffset;
            animatedShape.SetPressTransform(currentScale, currentVisualOffset);
            ApplyRectOffset(goalRectOffset);
            if (restoringTransform) animatedShape = null;
        }

        private void ApplyRectOffset(Vector2 offset)
        {
            if (currentRectOffset.Equals(offset)) return;
            var position = animatedShape.RectPosition;
            // Keep an externally repositioned RectTransform as the new resting position.
            if (currentRectOffset.Equals(Vector2.zero) || !position.Equals(lastRectPosition))
                rectBasePosition = position;
            currentRectOffset = offset;
            lastRectPosition = rectBasePosition + offset;
            if (!position.Equals(lastRectPosition)) animatedShape.RectPosition = lastRectPosition;
        }

        private void ClearTransform()
        {
            if (motion != null) StopCoroutine(motion);
            motion = null;
            if (animatedShape)
            {
                animatedShape.SetPressTransform(1f, Vector2.zero);
                ApplyRectOffset(Vector2.zero);
            }
            animatedShape = null;
            currentScale = 1f;
            currentVisualOffset = currentRectOffset = Vector2.zero;
        }

        private void Restore()
        {
            if (activeTarget) normal.Apply(activeTarget, hovered, pressed, false, false);
            activeTarget = null;
        }
    }
}
