using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using UnityEngine.UIElements;
using Button = UnityEngine.UIElements.Button;

namespace RustamKhonoff.UShape.Editor
{
    [CustomEditor(typeof(Shape)), CanEditMultipleObjects]
    public sealed class ShapeEditor : UnityEditor.Editor
    {
        private const string StylePath = "Packages/com.rustamkhonoff.ushape/Editor/ShapeInspector.uss";
        private const string SelectedGroupKey = "com.rustamkhonoff.ushape.SelectedGroup";
        private const string ActiveTab = "ushape-tab--active";
        private const string Leaving = "ushape-pane--leaving";
        private const int PaneFadeOutMs = 85;

        private static readonly string[] Groups = { "Appearance", "Edges", "Shadows", "Transform", "Effects" };
        private static readonly GUIContent BaseColor = new GUIContent("Base Color", "Multiplies all layers and effects.");
        private static readonly GUIContent AllCorners = new GUIContent("All Corners", "Set the same radius on all four corners.");
        private static readonly GUIContent TopLeft = new GUIContent("Top Left");
        private static readonly GUIContent TopRight = new GUIContent("Top Right");
        private static readonly GUIContent BottomRight = new GUIContent("Bottom Right");
        private static readonly GUIContent BottomLeft = new GUIContent("Bottom Left");
        private int group;
        private bool showInteraction;
        private static readonly GUIContent ScaleLabel = new GUIContent("Scale", "Scale of the drawn shape around its center.");
        private static readonly GUIContent OffsetLabel = new GUIContent("Offset", "Offset of the drawn shape, in local UI units.");

        private void OnEnable()
        {
            group = Mathf.Clamp(SessionState.GetInt(SelectedGroupKey, 0), 0, Groups.Length - 1);
        }

        private void SetGroup(int index)
        {
            group = index;
            SessionState.SetInt(SelectedGroupKey, index);
        }

        public override VisualElement CreateInspectorGUI()
        {
            var stylesheet = AssetDatabase.LoadAssetAtPath<StyleSheet>(StylePath);
            if (!stylesheet) return new IMGUIContainer(OnInspectorGUI);

            var root = new VisualElement();
            root.styleSheets.Add(stylesheet);
            root.AddToClassList("ushape-inspector");
            root.AddToClassList(EditorGUIUtility.isProSkin ? "ushape-dark" : "ushape-light");

            var tabs = new VisualElement { name = "shape-tabs" };
            tabs.AddToClassList("ushape-tabs");
            root.Add(tabs);

            var rail = new VisualElement { pickingMode = PickingMode.Ignore };
            rail.AddToClassList("ushape-tab-rail");
            tabs.Add(rail);

            var indicator = new VisualElement { name = "shape-tab-indicator", pickingMode = PickingMode.Ignore };
            indicator.AddToClassList("ushape-tab-indicator");
            indicator.style.width = Length.Percent(100f / Groups.Length);
            rail.Add(indicator);

            var pane = new VisualElement { name = "shape-pane" };
            pane.AddToClassList("ushape-pane");
            root.Add(pane);

            var buttons = new Button[Groups.Length];
            IVisualElementScheduledItem pending = null;
            int displayedGroup = group;
            for (int i = 0; i < buttons.Length; i++)
            {
                int index = i;
                var button = new Button(() => SelectGroup(index)) { text = Groups[i], name = "shape-tab-" + i };
                button.AddToClassList("ushape-tab");
                buttons[i] = button;
                tabs.Add(button);
            }

            UpdateTabs();
            ShowGroup(group);

            var interaction = new Foldout { text = "Interaction", value = showInteraction };
            interaction.AddToClassList("ushape-interaction");
            interaction.RegisterValueChangedCallback(evt => showInteraction = evt.newValue);
            interaction.Add(new IMGUIContainer(DrawInteraction));
            root.Add(interaction);
            return root;

            void SelectGroup(int index)
            {
                if (group == index) return;
                SetGroup(index);
                UpdateTabs();
                pending?.Pause();

                if (displayedGroup == index)
                {
                    pane.RemoveFromClassList(Leaving);
                    return;
                }

                pane.AddToClassList(Leaving);
                pending = pane.schedule.Execute(() =>
                {
                    ShowGroup(index);
                    pane.RemoveFromClassList(Leaving);
                }).StartingIn(PaneFadeOutMs);
            }

            void UpdateTabs()
            {
                indicator.style.translate = new Translate(Length.Percent(100f * group), 0);
                for (int i = 0; i < buttons.Length; i++)
                    buttons[i].EnableInClassList(ActiveTab, group == i);
            }

            void ShowGroup(int index)
            {
                displayedGroup = index;
                pane.Clear();
                pane.Add(new IMGUIContainer(() => DrawGroup(index)) { name = "shape-properties-" + index });
            }
        }

        public override void OnInspectorGUI()
        {
            int selected = GUILayout.Toolbar(group, Groups);
            if (selected != group) SetGroup(selected);
            DrawGroup(group);
            showInteraction = EditorGUILayout.Foldout(showInteraction, "Interaction", true);
            if (showInteraction) DrawInteraction();
        }

        private void DrawGroup(int index)
        {
            serializedObject.Update();
            switch (index)
            {
                case 0:
                    DrawAppearance();
                    break;
                case 1:
                    DrawStroke("Border", "borderWidth", "border", "borderStroke");
                    DrawStroke("Outline", "outlineWidth", "outline", "outlineStroke");
                    break;
                case 2:
                    DrawShadowStack("Outer Shadows", "outerShadow", "extraOuterShadows");
                    DrawShadowStack("Inner Shadows", "innerShadow", "extraInnerShadows");
                    break;
                case 3:
                    DrawTransform();
                    break;
                case 4:
                    DrawEffects();
                    break;
            }
            serializedObject.ApplyModifiedProperties();
        }

        private void DrawTransform()
        {
            EditorGUILayout.PropertyField(serializedObject.FindProperty("visualScale"), ScaleLabel);
            EditorGUILayout.PropertyField(serializedObject.FindProperty("visualOffset"), OffsetLabel);
        }

        private void DrawInteraction()
        {
            serializedObject.Update();
            EditorGUILayout.PropertyField(serializedObject.FindProperty("m_RaycastTarget"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("m_Maskable"));
            serializedObject.ApplyModifiedProperties();
        }

        private static void DrawSection(string label)
        {
            EditorGUILayout.Space(4);
            var heading = EditorGUILayout.GetControlRect(false, 22);
            var line = new Rect(heading.x, heading.yMax - 1, heading.width, 1);
            Color divider = EditorGUIUtility.isProSkin ? new Color(1, 1, 1, .10f) : new Color(0, 0, 0, .13f);
            EditorGUI.DrawRect(line, divider);
            EditorGUI.LabelField(heading, label, EditorStyles.boldLabel);
            EditorGUILayout.Space(4);
        }

        private void DrawAppearance()
        {
            DrawSection("Fill");
            EditorGUILayout.PropertyField(serializedObject.FindProperty("m_Color"), BaseColor);
            DrawPaint(serializedObject.FindProperty("fill"));
            var fillMode = serializedObject.FindProperty("fillMode");
            EditorGUILayout.PropertyField(fillMode, new GUIContent("Fill Mode",
                "Horizontal: left to right. Vertical: bottom to top. Radial: clockwise from the top."));
            if (fillMode.hasMultipleDifferentValues || fillMode.intValue != (int)ShapeFillMode.None)
                EditorGUILayout.PropertyField(serializedObject.FindProperty("fillAmount"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("styleTransitionDuration"),
                new GUIContent("Style Transition (s)", "Smooths runtime property changes. New targets continue from the visible value."));

            DrawSection("Corner Radius");
            var radii = serializedObject.FindProperty("cornerRadii");
            var value = radii.vector4Value;
            bool mixed = radii.hasMultipleDifferentValues || value.x != value.y || value.x != value.z || value.x != value.w;
            var field = EditorGUILayout.GetControlRect();
            EditorGUI.BeginProperty(field, AllCorners, radii);
            bool previousMixed = EditorGUI.showMixedValue;
            EditorGUI.showMixedValue = mixed;
            EditorGUI.BeginChangeCheck();
            float common = EditorGUI.FloatField(field, AllCorners, value.x);
            if (EditorGUI.EndChangeCheck()) radii.vector4Value = Vector4.one * Mathf.Max(0, common);
            EditorGUI.showMixedValue = previousMixed;
            EditorGUI.EndProperty();

            using (new EditorGUI.IndentLevelScope())
            {
                DrawRadius(radii.FindPropertyRelative("x"), TopLeft);
                DrawRadius(radii.FindPropertyRelative("y"), TopRight);
                DrawRadius(radii.FindPropertyRelative("z"), BottomRight);
                DrawRadius(radii.FindPropertyRelative("w"), BottomLeft);
            }
        }

        private void DrawEffects()
        {
            DrawSection("Blink");
            var blink = serializedObject.FindProperty("blink");
            var enabled = blink.FindPropertyRelative("enabled");
            EditorGUILayout.PropertyField(enabled);
            if (enabled.boolValue || enabled.hasMultipleDifferentValues)
            {
                EditorGUILayout.PropertyField(blink.FindPropertyRelative("width"));
                EditorGUILayout.PropertyField(blink.FindPropertyRelative("blur"));
                EditorGUILayout.PropertyField(blink.FindPropertyRelative("color"));
                EditorGUILayout.PropertyField(blink.FindPropertyRelative("angle"));
                EditorGUILayout.PropertyField(blink.FindPropertyRelative("duration"), new GUIContent("Duration (s)"));
                EditorGUILayout.PropertyField(blink.FindPropertyRelative("interval"), new GUIContent("Pause (s)"));
                EditorGUILayout.PropertyField(blink.FindPropertyRelative("initialDelay"), new GUIContent("Initial Delay (s)"));
            }

            DrawSection("Pointer");
            var pointer = serializedObject.FindProperty("pointer");
            var side = pointer.FindPropertyRelative("side");
            EditorGUILayout.PropertyField(side);
            if (!side.hasMultipleDifferentValues && side.enumValueIndex == (int)PointerSide.None) return;

            EditorGUILayout.PropertyField(pointer.FindPropertyRelative("position"));
            EditorGUILayout.PropertyField(pointer.FindPropertyRelative("width"));
            EditorGUILayout.PropertyField(pointer.FindPropertyRelative("height"));
        }

        private static void DrawRadius(SerializedProperty radius, GUIContent label)
        {
            EditorGUI.BeginChangeCheck();
            EditorGUILayout.PropertyField(radius, label);
            if (EditorGUI.EndChangeCheck() && radius.floatValue < 0) radius.floatValue = 0;
        }

        private void DrawStroke(string label, string widthProperty, string paintProperty, string strokeProperty)
        {
            DrawSection(label);
            EditorGUILayout.PropertyField(serializedObject.FindProperty(widthProperty));
            DrawPaint(serializedObject.FindProperty(paintProperty));

            var stroke = serializedObject.FindProperty(strokeProperty);
            var pattern = stroke.FindPropertyRelative("pattern");
            var motion = stroke.FindPropertyRelative("motion");
            EditorGUILayout.Space(3);
            EditorGUILayout.PropertyField(pattern);
            EditorGUILayout.PropertyField(motion, new GUIContent("Animation", "March advances the pattern. Pulse fades the stroke in and out."));
            bool mixedPattern = pattern.hasMultipleDifferentValues;
            bool patterned = mixedPattern || pattern.enumValueIndex != (int)StrokePattern.Solid;
            bool animated = motion.hasMultipleDifferentValues || motion.enumValueIndex != (int)StrokeMotion.None;
            if (mixedPattern || pattern.enumValueIndex == (int)StrokePattern.Dashed)
                EditorGUILayout.PropertyField(stroke.FindPropertyRelative("dashLength"));
            if (patterned) EditorGUILayout.PropertyField(stroke.FindPropertyRelative("gap"));
            if (patterned || animated) EditorGUILayout.PropertyField(stroke.FindPropertyRelative("phase"));
            if (animated) EditorGUILayout.PropertyField(stroke.FindPropertyRelative("speed"));
            if (!patterned && !motion.hasMultipleDifferentValues && motion.enumValueIndex == (int)StrokeMotion.March)
                EditorGUILayout.HelpBox("March needs a dashed or dotted pattern.", MessageType.None);
        }

        private void DrawShadowStack(string label, string firstProperty, string extraProperty)
        {
            DrawSection(label);
            var first = serializedObject.FindProperty(firstProperty);
            var extra = serializedObject.FindProperty(extraProperty);
            int count = Mathf.Min(1 + extra.arraySize, Shape.MaxShadowsPerType);

            for (int i = 0; i < count; i++)
            {
                var layer = GetLayer(first, extra, i);
                ShadowAction action = DrawShadowLayer(layer, i, count);
                if (action == ShadowAction.Remove)
                {
                    RemoveShadow(first, extra, i);
                    break;
                }
                if (action == ShadowAction.Up || action == ShadowAction.Down)
                {
                    MoveShadow(first, extra, i, i + (action == ShadowAction.Up ? -1 : 1));
                    break;
                }
            }

            using (new EditorGUI.DisabledScope(serializedObject.isEditingMultipleObjects || count >= Shape.MaxShadowsPerType))
            {
                if (GUILayout.Button("+ Add Shadow", EditorStyles.miniButton))
                    AddShadow(first, extra);
            }
        }

        private enum ShadowAction { None, Up, Down, Remove }

        private ShadowAction DrawShadowLayer(SerializedProperty layer, int index, int count)
        {
            var enabled = layer.FindPropertyRelative("enabled");
            ShadowAction action = ShadowAction.None;
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                using (new EditorGUILayout.HorizontalScope())
                {
                    EditorGUILayout.PropertyField(enabled, GUIContent.none, GUILayout.Width(16));
                    layer.isExpanded = GUILayout.Toggle(layer.isExpanded, "Shadow " + (index + 1),
                        EditorStyles.foldout, GUILayout.ExpandWidth(true));

                    using (new EditorGUI.DisabledScope(serializedObject.isEditingMultipleObjects))
                    {
                        if (ShadowButton("↑", "Move toward the front", index > 0)) action = ShadowAction.Up;
                        if (ShadowButton("↓", "Move toward the back", index < count - 1)) action = ShadowAction.Down;
                        if (ShadowButton("−", "Remove shadow", index > 0 || count > 1 || enabled.boolValue))
                            action = ShadowAction.Remove;
                    }
                }

                if (layer.isExpanded)
                {
                    EditorGUILayout.PropertyField(layer.FindPropertyRelative("color"));
                    EditorGUILayout.PropertyField(layer.FindPropertyRelative("offset"));
                    EditorGUILayout.PropertyField(layer.FindPropertyRelative("blur"));
                    EditorGUILayout.PropertyField(layer.FindPropertyRelative("spread"));
                }
            }
            return action;
        }

        private static bool ShadowButton(string label, string tooltip, bool available)
        {
            using (new EditorGUI.DisabledScope(!available))
                return GUILayout.Button(new GUIContent(label, tooltip), EditorStyles.miniButton, GUILayout.Width(22));
        }

        private static SerializedProperty GetLayer(SerializedProperty first, SerializedProperty extra, int index) =>
            index == 0 ? first : extra.GetArrayElementAtIndex(index - 1);

        private static void AddShadow(SerializedProperty first, SerializedProperty extra)
        {
            var enabled = first.FindPropertyRelative("enabled");
            if (!enabled.boolValue && extra.arraySize == 0)
            {
                enabled.boolValue = true;
                first.isExpanded = true;
                return;
            }

            int index = extra.arraySize;
            extra.InsertArrayElementAtIndex(index);
            var layer = extra.GetArrayElementAtIndex(index);
            SetShadow(layer, new PanelShadow { enabled = true });
            layer.isExpanded = true;
        }

        private static void RemoveShadow(SerializedProperty first, SerializedProperty extra, int index)
        {
            if (index > 0)
            {
                extra.DeleteArrayElementAtIndex(index - 1);
                return;
            }

            if (extra.arraySize == 0)
            {
                first.FindPropertyRelative("enabled").boolValue = false;
                first.isExpanded = false;
                return;
            }

            SetShadow(first, ReadShadow(extra.GetArrayElementAtIndex(0)));
            extra.DeleteArrayElementAtIndex(0);
        }

        private static void MoveShadow(SerializedProperty first, SerializedProperty extra, int from, int to)
        {
            if (from > 0 && to > 0)
            {
                extra.MoveArrayElement(from - 1, to - 1);
                return;
            }

            // The first layer keeps its original serialized property path.
            var second = extra.GetArrayElementAtIndex(0);
            var firstValue = ReadShadow(first);
            SetShadow(first, ReadShadow(second));
            SetShadow(second, firstValue);
        }

        private static PanelShadow ReadShadow(SerializedProperty layer) => new PanelShadow
        {
            enabled = layer.FindPropertyRelative("enabled").boolValue,
            color = layer.FindPropertyRelative("color").colorValue,
            offset = layer.FindPropertyRelative("offset").vector2Value,
            blur = layer.FindPropertyRelative("blur").floatValue,
            spread = layer.FindPropertyRelative("spread").floatValue
        };

        private static void SetShadow(SerializedProperty layer, PanelShadow value)
        {
            layer.FindPropertyRelative("enabled").boolValue = value.enabled;
            layer.FindPropertyRelative("color").colorValue = value.color;
            layer.FindPropertyRelative("offset").vector2Value = value.offset;
            layer.FindPropertyRelative("blur").floatValue = value.blur;
            layer.FindPropertyRelative("spread").floatValue = value.spread;
        }

        private static void DrawPaint(SerializedProperty paint)
        {
            var type = paint.FindPropertyRelative("type");
            EditorGUILayout.PropertyField(type);
            EditorGUILayout.PropertyField(paint.FindPropertyRelative("color"));
            if (!type.hasMultipleDifferentValues && type.enumValueIndex == (int)GradientType.Solid) return;

            EditorGUILayout.PropertyField(paint.FindPropertyRelative("gradient"));
            EditorGUILayout.PropertyField(paint.FindPropertyRelative("angle"));
            EditorGUILayout.PropertyField(paint.FindPropertyRelative("center"));
            EditorGUILayout.PropertyField(paint.FindPropertyRelative("scale"));
            EditorGUILayout.PropertyField(paint.FindPropertyRelative("offset"));
        }

        [MenuItem("GameObject/UI/Shape Example", false, 2031)]
        private static void CreateExample(MenuCommand command)
        {
            var panel = CreateShapeObject(command);
            Undo.RecordObject(panel, "Style Shape");
            panel.SetCornerRadius(22);
            panel.fill.type = GradientType.Linear;
            panel.fill.angle = 90;
            panel.fill.gradient.SetKeys(
                new[] { new GradientColorKey(new Color(.08f, .48f, .02f), 0), new GradientColorKey(new Color(.56f, .9f, .22f), 1) },
                new[] { new GradientAlphaKey(1, 0), new GradientAlphaKey(1, 1) });
            panel.borderWidth = 4;
            panel.border.color = new Color(.72f, .98f, .4f);
            panel.outerShadow.enabled = true;
            panel.outerShadow.offset = new Vector2(0, -6);
            panel.outerShadow.color = new Color32(20, 95, 8, 255);
            panel.outerShadow.blur = 0;
            panel.AdditionalOuterShadows.Add(new PanelShadow
            {
                enabled = true,
                offset = new Vector2(0, -12),
                blur = 20
            });
            panel.Refresh();
            EditorUtility.SetDirty(panel);
        }

        [MenuItem("GameObject/UI/Shape Tooltip Example", false, 2032)]
        private static void CreateTooltipExample(MenuCommand command)
        {
            var panel = CreateShapeObject(command);
            Undo.RecordObject(panel, "Style Shape Tooltip");
            panel.gameObject.name = "Shape Tooltip";
            panel.cornerRadii = Vector4.one * 24;
            panel.pointer = new PanelPointer
            {
                side = PointerSide.Top,
                position = .5f,
                width = 36,
                height = 18
            };
            panel.fill.type = GradientType.Linear;
            panel.fill.angle = 90;
            panel.fill.gradient.SetKeys(
                new[] { new GradientColorKey(new Color32(89, 31, 171, 255), 0),
                        new GradientColorKey(new Color32(155, 87, 238, 255), 1) },
                new[] { new GradientAlphaKey(1, 0), new GradientAlphaKey(1, 1) });
            panel.borderWidth = 3;
            panel.border.color = new Color32(255, 212, 68, 255);
            panel.outerShadow = new PanelShadow
            {
                enabled = true,
                color = new Color(0, 0, 0, .35f),
                offset = new Vector2(0, -7),
                blur = 14
            };
            panel.Refresh();
            EditorUtility.SetDirty(panel);
        }

        [MenuItem("GameObject/UI/Shape Dashed Example", false, 2033)]
        private static void CreateDashedExample(MenuCommand command)
        {
            var panel = CreateShapeObject(command);
            Undo.RecordObject(panel, "Style Dashed Shape");
            panel.gameObject.name = "Shape Dashed";
            panel.rectTransform.sizeDelta = new Vector2(320, 112);
            panel.cornerRadii = Vector4.one * 20;
            panel.fill.color = new Color32(39, 16, 87, 255);
            panel.borderWidth = 2;
            panel.border.color = new Color32(235, 163, 48, 255);
            panel.borderStroke = new PanelStroke
            {
                pattern = StrokePattern.Dashed,
                dashLength = 12,
                gap = 8,
                motion = StrokeMotion.March,
                speed = .75f
            };
            panel.Refresh();
            EditorUtility.SetDirty(panel);
        }

        [MenuItem("GameObject/UI/Shape", false, 2030)]
        private static void CreateShape(MenuCommand command) => CreateShapeObject(command);

        private static Shape CreateShapeObject(MenuCommand command)
        {
            var parent = command.context as GameObject;
            var canvas = parent ? parent.GetComponentInParent<Canvas>() : Object.FindAnyObjectByType<Canvas>();
            if (!canvas)
            {
                var root = new GameObject("Canvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
                root.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
                Undo.RegisterCreatedObjectUndo(root, "Create Canvas");
                canvas = root.GetComponent<Canvas>();
                if (!Object.FindAnyObjectByType<EventSystem>())
                {
                    var events = new GameObject("EventSystem", typeof(EventSystem));
#if ENABLE_INPUT_SYSTEM
                    var inputModule = System.Type.GetType("UnityEngine.InputSystem.UI.InputSystemUIInputModule, Unity.InputSystem");
                    if (inputModule != null) events.AddComponent(inputModule);
#elif ENABLE_LEGACY_INPUT_MANAGER
                    events.AddComponent<StandaloneInputModule>();
#endif
                    Undo.RegisterCreatedObjectUndo(events, "Create EventSystem");
                }
            }
            var go = new GameObject("Shape", typeof(RectTransform), typeof(CanvasRenderer), typeof(Shape));
            Undo.RegisterCreatedObjectUndo(go, "Create Shape");
            GameObjectUtility.SetParentAndAlign(go, parent && parent.GetComponentInParent<Canvas>() ? parent : canvas.gameObject);
            go.GetComponent<RectTransform>().sizeDelta = new Vector2(240, 100);
            Selection.activeGameObject = go;
            return go.GetComponent<Shape>();
        }
    }
}
