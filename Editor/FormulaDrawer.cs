using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using Cwcbb.Tools.CwcFormula;

namespace Cwcbb.Tools.CwcFormula.Editor
{
    /// <summary>
    /// Formula 通用数值公式容器的自定义属性绘制器。
    /// 遵循 CwcCombatEntity 紧凑卡片架构：
    /// 1. 整体卡片包裹 (Card Container with Accent Bar)；
    /// 2. 紧凑单行 Header（折叠、标题、策略切换下拉、更多选项菜单按钮）；
    /// 3. 策略菜单内聚：从配置读取、保存为配置、工程模板秒选、打开脚本、剪贴板复制/粘贴与重置；
    /// 4. 极致紧凑的内部字段排布与微型实时采样测试行。
    /// </summary>
    [CustomPropertyDrawer(typeof(Formula))]
    public class FormulaDrawer : PropertyDrawer
    {
        #region Private Static Fields
        private const int FORMULA_TEMPLATE_PICKER_CONTROL_ID = 202690;
        private static string _pickingPropertyPath;
        private static UnityEngine.Object _pickingTargetObject;

        private static string _clipboardFormulaJson;
        private static Type _clipboardFormulaType;
        private static List<FormulaTypeInfo> _cachedStrategies;

        private class FormulaTypeInfo
        {
            public Type Type;
            public string ShortName;
            public string DisplayName;
            public int Order;
        }

        private static readonly Color _cardAccentColor = new Color(0.28f, 0.65f, 0.95f, 0.95f);
        #endregion

        #region Public Methods
        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            float lineHeight = EditorGUIUtility.singleLineHeight;
            float spacing = EditorGUIUtility.standardVerticalSpacing;

            // 卡片上下边距: 各 3px
            float cardPadding = 6f;

            // Header 行高度: 22px
            float headerHeight = 22f;

            if (!property.isExpanded)
            {
                return headerHeight + cardPadding;
            }

            float totalHeight = headerHeight + cardPadding;

            // 动态累加 _evaluator 内部所有可见字段高度
            SerializedProperty evaluatorProp = property.FindPropertyRelative("_evaluator");
            if (evaluatorProp != null && evaluatorProp.managedReferenceValue != null)
            {
                SerializedProperty copy = evaluatorProp.Copy();
                SerializedProperty end = copy.GetEndProperty();
                bool enterChildren = true;

                while (copy.NextVisible(enterChildren) && !SerializedProperty.EqualContents(copy, end))
                {
                    enterChildren = false;
                    totalHeight += EditorGUI.GetPropertyHeight(copy, true) + spacing;
                }
            }
            else
            {
                // 未指定公式策略时展开，计入 HelpBox 警告提示框的高度与间隙
                float warningHeight = Mathf.Max(26f, lineHeight * 1.5f);
                totalHeight += warningHeight + spacing;
            }

            return totalHeight + 2f;
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            // 0. 监听 ObjectPicker 选择事件 (用于 Apply Preset Template...)
            Event evt = Event.current;
            if (evt.type == EventType.ExecuteCommand && (evt.commandName == "ObjectSelectorUpdated" || evt.commandName == "ObjectSelectorClosed"))
            {
                if (EditorGUIUtility.GetObjectPickerControlID() == FORMULA_TEMPLATE_PICKER_CONTROL_ID
                    && _pickingPropertyPath == property.propertyPath
                    && _pickingTargetObject == property.serializedObject.targetObject)
                {
                    var pickedTemplate = EditorGUIUtility.GetObjectPickerObject() as FormulaTemplateSO;
                    if (pickedTemplate != null)
                    {
                        ApplyTemplateToProperty(property, pickedTemplate);
                    }

                    if (evt.commandName == "ObjectSelectorClosed")
                    {
                        _pickingPropertyPath = null;
                        _pickingTargetObject = null;
                    }
                    evt.Use();
                    GUI.changed = true;
                }
            }

            EditorGUI.BeginProperty(position, label, property);

            float lineHeight = EditorGUIUtility.singleLineHeight;
            float spacing = EditorGUIUtility.standardVerticalSpacing;

            // 1. 绘制外部卡片底板 (Card Box) 与左侧重音线 (Accent Bar)
            GUI.Box(position, GUIContent.none, EditorStyles.helpBox);
            Rect accentRect = new Rect(position.x + 1f, position.y + 1f, 3.5f, position.height - 2f);
            EditorGUI.DrawRect(accentRect, _cardAccentColor);

            // 卡片内部可用区域
            Rect contentRect = new Rect(position.x + 6f, position.y + 3f, position.width - 12f, position.height - 6f);

            SerializedProperty evaluatorProp = property.FindPropertyRelative("_evaluator");

            Type currentType = evaluatorProp?.managedReferenceValue?.GetType();
            string shortTypeName = GetShortTypeName(currentType);

            // 2. 绘制 Header 行 (22px，集成策略选框、数值测试框与操作菜单)
            Rect headerRect = new Rect(contentRect.x, contentRect.y, contentRect.width, 22f);
            DrawCardHeader(headerRect, property, label, shortTypeName, currentType, evaluatorProp);

            if (!property.isExpanded)
            {
                EditorGUI.EndProperty();
                return;
            }

            float currentY = headerRect.yMax + spacing;

            // 3. 绘制公式具体参数字段
            if (evaluatorProp != null && evaluatorProp.managedReferenceValue != null)
            {
                SerializedProperty copy = evaluatorProp.Copy();
                SerializedProperty end = copy.GetEndProperty();
                bool enterChildren = true;

                EditorGUI.indentLevel++;

                while (copy.NextVisible(enterChildren) && !SerializedProperty.EqualContents(copy, end))
                {
                    enterChildren = false;
                    float childHeight = EditorGUI.GetPropertyHeight(copy, true);
                    Rect childRect = new Rect(contentRect.x, currentY, contentRect.width, childHeight);

                    EditorGUI.PropertyField(childRect, copy, true);
                    currentY += childHeight + spacing;
                }

                EditorGUI.indentLevel--;
            }
            else
            {
                float warningHeight = Mathf.Max(26f, lineHeight * 1.5f);
                Rect warningRect = new Rect(contentRect.x + 3f, currentY, contentRect.width - 6f, warningHeight);
                EditorGUI.HelpBox(warningRect, "No formula strategy assigned. Use the dropdown above to choose one.", MessageType.Warning);
            }

            EditorGUI.EndProperty();
        }
        #endregion

        #region Private Methods
        private void DrawCardHeader(
            Rect headerRect,
            SerializedProperty property,
            GUIContent label,
            string shortTypeName,
            Type currentType,
            SerializedProperty evaluatorProp)
        {
            Event evt = Event.current;

            // 1. 最右侧菜单按钮紧贴卡片内部右边界（与卡片外边框保持匀称呼吸边距）
            float menuBtnWidth = 18f;
            Rect menuBtnRect = new Rect(headerRect.xMax - menuBtnWidth - 1f, headerRect.y + 1f, menuBtnWidth, 20f);

            // 2. 右侧操作区紧凑布局规划 (菜单 18px + 预览小按钮 54px + 策略选择 76px)
            float previewBtnWidth = 54f;
            Rect previewBtnRect = new Rect(menuBtnRect.x - 3f - previewBtnWidth, headerRect.y + 1f, previewBtnWidth, 20f);

            float strategyBtnWidth = 76f;
            Rect strategyBtnRect = new Rect(previewBtnRect.x - 3f - strategyBtnWidth, headerRect.y + 1f, strategyBtnWidth, 20f);

            // 3. 左侧折叠箭头与字段标题 (自适应剩余更充裕宽度)
            Rect foldoutRect = new Rect(headerRect.x + 2f, headerRect.y + 3f, 14f, 16f);
            float titleWidth = Mathf.Max(20f, strategyBtnRect.x - foldoutRect.xMax - 4f);
            Rect titleRect = new Rect(foldoutRect.xMax + 2f, headerRect.y + 1f, titleWidth, 20f);

            // 折叠切换
            property.isExpanded = GUI.Toggle(foldoutRect, property.isExpanded, GUIContent.none, EditorStyles.foldout);

            // 标题显示与点击整行切换折叠
            GUIStyle titleStyle = new GUIStyle(EditorStyles.boldLabel)
            {
                clipping = TextClipping.Ellipsis
            };
            GUI.Label(titleRect, new GUIContent(label.text, $"Property Field: {label.text}"), titleStyle);

            if (evt.type == EventType.MouseDown && evt.button == 0 && titleRect.Contains(evt.mousePosition))
            {
                property.isExpanded = !property.isExpanded;
                evt.Use();
                GUI.changed = true;
            }

            // 4. 绘制策略切换按钮 (分层设计：右侧固定箭头，左侧文字打点省略)
            if (GUI.Button(strategyBtnRect, GUIContent.none, EditorStyles.miniButton))
            {
                ShowStrategyDropdown(strategyBtnRect, evaluatorProp);
            }

            // 4.1 右侧固定箭头
            Rect arrowPartRect = new Rect(strategyBtnRect.xMax - 13f, strategyBtnRect.y, 11f, strategyBtnRect.height);
            GUIStyle arrowPartStyle = new GUIStyle(EditorStyles.miniLabel)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = 9,
                normal = { textColor = EditorGUIUtility.isProSkin ? new Color(0.65f, 0.65f, 0.65f) : new Color(0.35f, 0.35f, 0.35f) }
            };
            GUI.Label(arrowPartRect, "v", arrowPartStyle);

            // 4.2 左侧文字 (超出自动打点省略)
            Rect labelPartRect = new Rect(strategyBtnRect.x + 4f, strategyBtnRect.y, strategyBtnRect.width - 17f, strategyBtnRect.height);
            GUIStyle strategyLabelStyle = new GUIStyle(EditorStyles.label)
            {
                alignment = TextAnchor.MiddleLeft,
                clipping = TextClipping.Ellipsis,
                fontSize = 11,
                normal = { textColor = EditorGUIUtility.isProSkin ? new Color(0.85f, 0.85f, 0.85f) : new Color(0.15f, 0.15f, 0.15f) }
            };
            GUI.Label(labelPartRect, shortTypeName, strategyLabelStyle);

            // 5. 绘制曲线浮动预览小区域 (鼠标悬停即弹出无污染浮动曲线窗口，离开即自动隐藏)
            FormulaBase currentFormula = evaluatorProp?.managedReferenceValue as FormulaBase;
            bool hasFormula = currentFormula != null;

            bool isBoundToThis = FormulaPreviewPopup.IsBoundTo(property);
            bool isPinnedThis = isBoundToThis && FormulaPreviewPopup.IsPinned;

            string btnText = isPinnedThis ? "Pinned" : "Curve";
            GUIContent previewBtnContent = new GUIContent(btnText, "Hover to preview curve\nClick to pin");

            EditorGUI.BeginDisabledGroup(!hasFormula);
            Color prevBg = GUI.backgroundColor;
            if (isPinnedThis)
            {
                GUI.backgroundColor = new Color(0.35f, 0.75f, 1f, 1f);
            }

            if (GUI.Button(previewBtnRect, previewBtnContent, EditorStyles.miniButton))
            {
                FormulaPreviewPopup.TogglePin(previewBtnRect, currentFormula, shortTypeName, property);
            }
            GUI.backgroundColor = prevBg;
            EditorGUI.EndDisabledGroup();

            // 悬停感应：鼠标移入小区域立即激活浮动预览窗口，移开后由系统自动平滑关闭
            if (hasFormula && (evt.type == EventType.Repaint || evt.type == EventType.MouseMove))
            {
                if (previewBtnRect.Contains(evt.mousePosition))
                {
                    FormulaPreviewPopup.RequestHover(previewBtnRect, currentFormula, shortTypeName, property);
                }
            }

            // 数据变更通知：当预览弹窗打开且绑定的是本字段时，若策划在下方面板调整了参数或切换了策略，按需触发重算
            if (FormulaPreviewPopup.IsOpen && isBoundToThis && hasFormula)
            {
                FormulaPreviewPopup.NotifyFormula(currentFormula, shortTypeName, property);
            }

            // 9. 更多选项菜单按钮 (图标 ...)
            GUIContent moreIcon = EditorGUIUtility.IconContent(EditorGUIUtility.isProSkin ? "d_more" : "more");
            if (moreIcon == null || moreIcon.image == null)
            {
                moreIcon = new GUIContent("...", "Formula Options");
            }
            else
            {
                moreIcon.tooltip = "Formula Options";
            }

            if (GUI.Button(menuBtnRect, moreIcon, EditorStyles.iconButton))
            {
                ShowFormulaMenu(menuBtnRect, property, evaluatorProp, currentType, shortTypeName, isContextMenu: false);
            }

            // 整行支持右键上下文菜单
            if (evt.type == EventType.ContextClick && headerRect.Contains(evt.mousePosition))
            {
                ShowFormulaMenu(headerRect, property, evaluatorProp, currentType, shortTypeName, isContextMenu: true);
                evt.Use();
            }
        }

        private void ShowStrategyDropdown(Rect positionRect, SerializedProperty evaluatorProp)
        {
            var menu = new GenericMenu();
            Type currentType = evaluatorProp?.managedReferenceValue?.GetType();
            var list = GetStrategies();

            for (int i = 0; i < list.Count; i++)
            {
                var info = list[i];
                bool isSelected = currentType == info.Type;

                menu.AddItem(new GUIContent(info.DisplayName), isSelected, () =>
                {
                    Undo.RecordObject(evaluatorProp.serializedObject.targetObject, $"Change Strategy to {info.ShortName}");
                    evaluatorProp.managedReferenceValue = Activator.CreateInstance(info.Type);
                    evaluatorProp.serializedObject.ApplyModifiedProperties();
                });
            }

            menu.DropDown(positionRect);
        }

        private void ShowFormulaMenu(
            Rect positionRect,
            SerializedProperty property,
            SerializedProperty evaluatorProp,
            Type currentType,
            string shortTypeName,
            bool isContextMenu)
        {
            var menu = new GenericMenu();
            var targetObject = property.serializedObject.targetObject;

            // 1. 最常用的基础操作组（平铺在最顶层，不嵌套折叠）
            if (evaluatorProp?.managedReferenceValue != null)
            {
                menu.AddItem(new GUIContent("Copy"), false, () =>
                {
                    _clipboardFormulaType = evaluatorProp.managedReferenceValue.GetType();
                    _clipboardFormulaJson = EditorJsonUtility.ToJson(evaluatorProp.managedReferenceValue);
                    Debug.Log($"[FormulaDrawer] 已复制 {_clipboardFormulaType.Name} 设置至剪贴板。");
                });
            }
            else
            {
                menu.AddDisabledItem(new GUIContent("Copy"));
            }

            bool canPaste = _clipboardFormulaType != null && !string.IsNullOrEmpty(_clipboardFormulaJson);
            if (canPaste)
            {
                string pasteLabel = $"Paste ({GetShortTypeName(_clipboardFormulaType)})";
                menu.AddItem(new GUIContent(pasteLabel), false, () =>
                {
                    Undo.RecordObject(targetObject, "Paste Formula Settings");
                    object newInstance = Activator.CreateInstance(_clipboardFormulaType);
                    EditorJsonUtility.FromJsonOverwrite(_clipboardFormulaJson, newInstance);
                    evaluatorProp.managedReferenceValue = newInstance;
                    property.serializedObject.ApplyModifiedProperties();
                    Debug.Log($"[FormulaDrawer] 已粘贴 {_clipboardFormulaType.Name} 设置。");
                });
            }
            else
            {
                menu.AddDisabledItem(new GUIContent("Paste"));
            }

            if (currentType != null)
            {
                menu.AddItem(new GUIContent($"Reset ({shortTypeName})"), false, () =>
                {
                    Undo.RecordObject(targetObject, $"Reset {currentType.Name}");
                    evaluatorProp.managedReferenceValue = Activator.CreateInstance(currentType);
                    property.serializedObject.ApplyModifiedProperties();
                });
            }
            else
            {
                menu.AddDisabledItem(new GUIContent("Reset"));
            }

            menu.AddSeparator("");

            // 2. 预设操作组（印章模式：应用预设覆盖参数，但不绑定资产引用）
            menu.AddItem(new GUIContent("Apply Preset Template..."), false, () =>
            {
                _pickingPropertyPath = property.propertyPath;
                _pickingTargetObject = targetObject;
                EditorGUIUtility.ShowObjectPicker<FormulaTemplateSO>(
                    null,
                    false,
                    "",
                    FORMULA_TEMPLATE_PICKER_CONTROL_ID);
            });

            if (evaluatorProp?.managedReferenceValue is FormulaBase)
            {
                menu.AddItem(new GUIContent("Save as New Preset..."), false, () =>
                {
                    SavePropertyAsTemplate(property);
                });
            }
            else
            {
                menu.AddDisabledItem(new GUIContent("Save as New Preset..."));
            }

            menu.AddSeparator("");

            // 3. 开发者操作（打开脚本置底）
            if (currentType != null)
            {
                menu.AddItem(new GUIContent("Open Script"), false, () =>
                {
                    OpenScriptForType(currentType);
                });
            }
            else
            {
                menu.AddDisabledItem(new GUIContent("Open Script"));
            }

            if (isContextMenu)
            {
                menu.ShowAsContext();
            }
            else
            {
                menu.DropDown(positionRect);
            }
        }

        private void ApplyTemplateToProperty(SerializedProperty property, FormulaTemplateSO templateSO)
        {
            if (templateSO == null)
            {
                return;
            }

            SerializedProperty evaluatorProp = property.FindPropertyRelative("_evaluator");
            Undo.RecordObject(property.serializedObject.targetObject, "Apply Formula Preset");

            FormulaBase newInstance = templateSO.CreateInstance();
            evaluatorProp.managedReferenceValue = newInstance;
            property.serializedObject.ApplyModifiedProperties();

            Debug.Log($"[FormulaDrawer] 已成功应用预设 [{templateSO.name}] 并覆盖当前公式参数（未绑定资产引用）。");
        }

        private void SavePropertyAsTemplate(SerializedProperty property)
        {
            SerializedProperty evaluatorProp = property.FindPropertyRelative("_evaluator");
            FormulaBase currentFormula = evaluatorProp?.managedReferenceValue as FormulaBase;

            if (currentFormula == null)
            {
                EditorUtility.DisplayDialog("Notice", "Current formula is empty. Cannot save as template.", "OK");
                return;
            }

            string defaultName = $"{property.displayName}_Preset";
            string path = EditorUtility.SaveFilePanelInProject("Save Formula Preset", defaultName, "asset", "Please choose a location to save the FormulaTemplateSO");

            if (string.IsNullOrEmpty(path))
            {
                return;
            }

            FormulaTemplateSO asset = ScriptableObject.CreateInstance<FormulaTemplateSO>();
            asset.SetFormula(currentFormula);

            AssetDatabase.CreateAsset(asset, path);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log($"[FormulaDrawer] 成功将当前公式另存为新预设资产：{path}（当前配置保持独立，未产生强绑定）。");
        }

        private static void OpenScriptForType(Type type)
        {
            if (type == null) return;

            string[] guids = AssetDatabase.FindAssets($"t:MonoScript {type.Name}");
            foreach (var guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                var script = AssetDatabase.LoadAssetAtPath<MonoScript>(path);
                if (script != null && script.GetClass() == type)
                {
                    AssetDatabase.OpenAsset(script);
                    return;
                }
            }

            EditorUtility.DisplayDialog("Notice", $"Could not find script file for {type.Name}.\nPlease ensure the script file name matches the class name ({type.Name}.cs).", "OK");
        }

        private static List<FormulaTypeInfo> GetStrategies()
        {
            if (_cachedStrategies == null)
            {
                _cachedStrategies = new List<FormulaTypeInfo>();
                var types = TypeCache.GetTypesDerivedFrom<FormulaBase>();
                foreach (var type in types)
                {
                    if (type.IsAbstract || type.IsInterface) continue;
                    var attr = (FormulaStrategyAttribute)Attribute.GetCustomAttribute(type, typeof(FormulaStrategyAttribute));
                    string shortName = attr != null && !string.IsNullOrEmpty(attr.ShortName) ? attr.ShortName : GetFallbackShortName(type);
                    string displayName = attr != null && !string.IsNullOrEmpty(attr.DisplayName) ? attr.DisplayName : shortName;
                    int order = attr != null ? attr.Order : 100;
                    _cachedStrategies.Add(new FormulaTypeInfo
                    {
                        Type = type,
                        ShortName = shortName,
                        DisplayName = displayName,
                        Order = order
                    });
                }
                _cachedStrategies.Sort((a, b) => a.Order.CompareTo(b.Order));
            }
            return _cachedStrategies;
        }

        private static string GetFallbackShortName(Type type)
        {
            if (type == null) return "None";
            string name = type.Name;
            if (name.EndsWith("Formula"))
            {
                name = name.Substring(0, name.Length - "Formula".Length);
            }
            return name;
        }

        private static string GetShortTypeName(Type type)
        {
            if (type == null) return "None";
            var list = GetStrategies();
            for (int i = 0; i < list.Count; i++)
            {
                if (list[i].Type == type) return list[i].ShortName;
            }
            return GetFallbackShortName(type);
        }
        #endregion
    }
}

