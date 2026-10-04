using System;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;
using Cwcbb.Tools.CwcFormula;

namespace Cwcbb.Tools.CwcFormula.Editor
{
    /// <summary>
    /// SegmentedFormula.Segment 单个分段项的自定义属性绘制器。
    /// 遵循 CwcCombatEntity 精致卡片与紧凑排版规范：
    /// 1. 顶部 Header 实时计算并展示当前段的参数起止与数值起止 (如 #0 [Power]: X [1 -> 20] => Y [100 -> 1,250])；
    /// 2. 展开后根据公式类型条件显示核心字段，隐藏无关字段；
    /// 3. 支持 Power 幂函数模式（幂指数滑块支持调控前期快后期慢）；
    /// 4. 实时级联无 GC 链式计算，所见即所得。
    /// </summary>
    [CustomPropertyDrawer(typeof(SegmentedFormula.Segment))]
    public class SegmentDrawer : PropertyDrawer
    {
        #region Private Static Fields
        private const float PADDING = 2f;
        private static readonly Regex _indexRegex = new Regex(@"\[(\d+)\]$", RegexOptions.Compiled);
        #endregion

        #region Public Methods
        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            float lineHeight = EditorGUIUtility.singleLineHeight;
            float headerHeight = lineHeight + PADDING;

            if (!property.isExpanded)
            {
                return headerHeight;
            }

            float innerHeight = GetInnerHeight(property);
            return headerHeight + innerHeight + PADDING * 3;
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginProperty(position, label, property);

            float lineHeight = EditorGUIUtility.singleLineHeight;
            int index = GetPropertyIndex(property);

            // 1. 动态链式计算当前分段在整个曲线中的起点与终点数值
            var bounds = ComputeSegmentBounds(property, index);
            float startParam = bounds.startParam;
            float startVal = bounds.startVal;
            float endParam = bounds.endParam;
            float endVal = bounds.endVal;
            float deltaVal = endVal - startVal;

            SerializedProperty typeProp = property.FindPropertyRelative("_type");
            var segType = (SegmentedFormula.SegmentType)typeProp.enumValueIndex;
            string typeShortName = segType.ToString();

            // 2. 格式化 Header 标签
            string sign = deltaVal >= 0f ? "+" : "";
            string deltaFormatted = $"{sign}{deltaVal:N0}";
            string headerTitle = $"#{index} {typeShortName}: X[{startParam:0.#}..{endParam:0.#}] => Y[{startVal:N0}..{endVal:N0}] ({deltaFormatted})";

            Rect headerRect = new Rect(position.x, position.y, position.width, lineHeight);
            property.isExpanded = EditorGUI.Foldout(headerRect, property.isExpanded, headerTitle, true);

            if (!property.isExpanded)
            {
                EditorGUI.EndProperty();
                return;
            }

            // 3. 展开后的内容容器 (HelpBox)
            float innerHeight = GetInnerHeight(property);
            Rect boxRect = new Rect(position.x, headerRect.yMax + PADDING, position.width, innerHeight + PADDING * 2);
            GUI.Box(boxRect, GUIContent.none, EditorStyles.helpBox);

            Rect rowRect = new Rect(boxRect.x + 6f, boxRect.y + 4f, boxRect.width - 12f, lineHeight);

            EditorGUI.indentLevel++;
            EditorGUI.BeginChangeCheck();

            // 3.1 算法类型
            EditorGUI.PropertyField(rowRect, typeProp, new GUIContent("Type", "Segment calculation type"));
            rowRect.y += lineHeight + PADDING;

            // 3.2 参数跨度 Delta X
            SerializedProperty deltaParamProp = property.FindPropertyRelative("_parameterDelta");
            EditorGUI.PropertyField(rowRect, deltaParamProp, new GUIContent("Param Delta", "Parameter span covered by this segment"));
            rowRect.y += lineHeight + PADDING;

            // 3.3 根据算法类型条件显示具体参数
            switch (segType)
            {
                case SegmentedFormula.SegmentType.Linear:
                    {
                        SerializedProperty valChangeProp = property.FindPropertyRelative("_valueChange");
                        EditorGUI.PropertyField(rowRect, valChangeProp, new GUIContent("Value Delta", "Total value change across this segment"));
                        rowRect.y += lineHeight + PADDING;
                    }
                    break;

                case SegmentedFormula.SegmentType.Power:
                    {
                        SerializedProperty valChangeProp = property.FindPropertyRelative("_valueChange");
                        EditorGUI.PropertyField(rowRect, valChangeProp, new GUIContent("Value Delta", "Total value change across this segment"));
                        rowRect.y += lineHeight + PADDING;

                        SerializedProperty expProp = property.FindPropertyRelative("_exponent");
                        expProp.floatValue = EditorGUI.Slider(rowRect, new GUIContent("Exponent", "Power exponent (p)"), expProp.floatValue, 0.05f, 4.0f);
                        rowRect.y += lineHeight + PADDING;
                    }
                    break;

                case SegmentedFormula.SegmentType.Exponential:
                    {
                        SerializedProperty growthProp = property.FindPropertyRelative("_growthFactor");
                        EditorGUI.PropertyField(rowRect, growthProp, new GUIContent("Growth Factor", "Geometric multiplier per unit"));
                        rowRect.y += lineHeight + PADDING;
                    }
                    break;

                case SegmentedFormula.SegmentType.AnimationCurve:
                    {
                        SerializedProperty valChangeProp = property.FindPropertyRelative("_valueChange");
                        EditorGUI.PropertyField(rowRect, valChangeProp, new GUIContent("Value Delta", "Total value change across this segment"));
                        rowRect.y += lineHeight + PADDING;

                        SerializedProperty curveProp = property.FindPropertyRelative("_curve");
                        EditorGUI.PropertyField(rowRect, curveProp, new GUIContent("Curve", "Progression curve shape (0..1)"));
                        rowRect.y += lineHeight + PADDING;
                    }
                    break;
            }

            if (EditorGUI.EndChangeCheck())
            {
                property.serializedObject.ApplyModifiedProperties();
                NotifyParentChanged(property);
            }

            EditorGUI.indentLevel--;
            EditorGUI.EndProperty();
        }
        #endregion

        #region Private Methods
        private static int GetPropertyIndex(SerializedProperty property)
        {
            Match match = _indexRegex.Match(property.propertyPath);
            return match.Success ? int.Parse(match.Groups[1].Value) : 0;
        }

        private static float GetInnerHeight(SerializedProperty property)
        {
            float lineHeight = EditorGUIUtility.singleLineHeight;
            SerializedProperty typeProp = property.FindPropertyRelative("_type");
            var segType = (SegmentedFormula.SegmentType)(typeProp != null ? typeProp.enumValueIndex : 0);

            // 基础两行：Type 与 Param Delta
            int rowCount = 2;

            switch (segType)
            {
                case SegmentedFormula.SegmentType.Linear:
                    rowCount += 1; // Value Change
                    break;
                case SegmentedFormula.SegmentType.Power:
                    rowCount += 2; // Value Change + Exponent
                    break;
                case SegmentedFormula.SegmentType.Exponential:
                    rowCount += 1; // Growth Factor
                    break;
                case SegmentedFormula.SegmentType.AnimationCurve:
                    rowCount += 2; // Value Change + Curve
                    break;
            }

            return rowCount * (lineHeight + PADDING) + 4f;
        }

        private static (float startParam, float startVal, float endParam, float endVal) ComputeSegmentBounds(SerializedProperty segmentProp, int index)
        {
            string path = segmentProp.propertyPath;
            int arrayIdx = path.LastIndexOf(".Array.data");
            if (arrayIdx < 0)
            {
                return (1f, 0f, 10f, 0f);
            }

            string segmentsPath = path.Substring(0, arrayIdx);
            int segIdx = segmentsPath.LastIndexOf("._segments");
            if (segIdx < 0)
            {
                return (1f, 0f, 10f, 0f);
            }

            string startPointPath = segmentsPath.Substring(0, segIdx) + "._startPoint";
            SerializedProperty startPointProp = segmentProp.serializedObject.FindProperty(startPointPath);
            SerializedProperty segmentsProp = segmentProp.serializedObject.FindProperty(segmentsPath);

            Vector2 startPoint = startPointProp != null ? startPointProp.vector2Value : new Vector2(1f, 1f);

            float curParam = startPoint.x;
            float curVal = startPoint.y;

            if (segmentsProp != null)
            {
                for (int i = 0; i < index && i < segmentsProp.arraySize; i++)
                {
                    SerializedProperty prev = segmentsProp.GetArrayElementAtIndex(i);
                    if (prev != null)
                    {
                        float pDelta = Mathf.Max(0.001f, prev.FindPropertyRelative("_parameterDelta").floatValue);
                        curVal = EvaluateSegmentProp(prev, curVal, 1.0f);
                        curParam += pDelta;
                    }
                }
            }

            float thisDelta = Mathf.Max(0.001f, segmentProp.FindPropertyRelative("_parameterDelta").floatValue);
            float thisEndVal = EvaluateSegmentProp(segmentProp, curVal, 1.0f);

            return (curParam, curVal, curParam + thisDelta, thisEndVal);
        }

        private static float EvaluateSegmentProp(SerializedProperty segProp, float startVal, float progress)
        {
            if (segProp == null) return startVal;

            var type = (SegmentedFormula.SegmentType)segProp.FindPropertyRelative("_type").enumValueIndex;
            float delta = Mathf.Max(0.001f, segProp.FindPropertyRelative("_parameterDelta").floatValue);
            float valChange = segProp.FindPropertyRelative("_valueChange").floatValue;
            float exponent = segProp.FindPropertyRelative("_exponent").floatValue;
            float growthFactor = segProp.FindPropertyRelative("_growthFactor").floatValue;
            AnimationCurve curve = segProp.FindPropertyRelative("_curve").animationCurveValue;

            return SegmentedFormula.Segment.EvaluateStatic(type, startVal, valChange, exponent, growthFactor, curve, delta, progress);
        }

        private static void NotifyParentChanged(SerializedProperty property)
        {
            if (property.serializedObject.targetObject != null)
            {
                EditorUtility.SetDirty(property.serializedObject.targetObject);
            }
        }
        #endregion
    }
}
