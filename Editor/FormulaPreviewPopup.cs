using System;
using UnityEditor;
using UnityEngine;
using Cwcbb.Tools.CwcFormula;

namespace Cwcbb.Tools.CwcFormula.Editor
{
    /// <summary>
    /// 数值公式曲线浮动预览窗口。
    /// 纯事件与脏标记驱动 (Dirty-Driven)，彻底告别 Update 轮询计算与垃圾分配。
    /// 仅在公式参数改变、模式切换或跨度切换时重算曲线，支持原点0空间感与鼠标准星实时采样。
    /// </summary>
    public class FormulaPreviewPopup : EditorWindow
    {
        #region Private Static Fields
        private static FormulaPreviewPopup _instance;
        private static FormulaBase _activeFormula;
        private static string _formulaTitle;
        private static Rect _triggerScreenRect;
        private static double _lastHoverTime;
        private static bool _isPinned = false;
        private static EditorWindow _hostWindow;

        private static readonly int[] _rangeOptions = new int[] { 20, 50, 100 };
        private static int _selectedRangeIndex = 1; // 默认 50

        private static int _cachedFormulaFingerprint = 0;
        private static bool _isCurveDataDirty = true;

        private static readonly Color _windowBgColor = new Color(0.18f, 0.18f, 0.20f, 1f);
        private static readonly Color _canvasBgColor = new Color(0.11f, 0.11f, 0.13f, 1f);
        private static readonly Color _canvasBorderColor = new Color(0.26f, 0.26f, 0.30f, 1f);
        private static readonly Color _gridLineColor = new Color(1f, 1f, 1f, 0.08f);
        private static readonly Color _curveColor = new Color(0.35f, 0.72f, 0.98f, 1f);
        private static readonly Color _cursorCrossColor = new Color(1f, 0.75f, 0.25f, 1f);
        private static UnityEngine.Object _boundTargetObject;
        private static string _boundPropertyPath;
        #endregion

        #region Private Fields
        // 预分配缓冲区与计算缓存，容量增至 120 点以实现全局超密集均匀采样（平均每 2px 一个采样点）
        private readonly Vector3[] _curvePointsBuffer = new Vector3[120];
        private readonly float[] _xSamplesBuffer = new float[120];
        private readonly float[] _yValuesBuffer = new float[120];

        private int _cachedRangeIndex = -1;
        private Rect _cachedCanvasRect;

        private float _cachedMinY;
        private float _cachedMaxY;
        private float _cachedPaddedMinY;
        private float _cachedPaddedSpan;
        private float _cachedAxisMinX;
        private float _cachedAxisMaxX;
        private float _cachedFormulaStartX;
        private float _cachedStartValY;
        private bool _cachedHasStartAnchor;
        private Vector3 _cachedAnchorPos;
        #endregion

        #region Public Properties
        public static bool IsOpen => _instance != null;
        public static bool IsPinned => _isPinned;
        public static UnityEngine.Object BoundTargetObject => _boundTargetObject;
        public static string BoundPropertyPath => _boundPropertyPath;

        public static bool IsBoundTo(SerializedProperty property)
        {
            if (_instance == null || property == null) return false;
            return _boundTargetObject == property.serializedObject.targetObject
                && _boundPropertyPath == property.propertyPath;
        }
        #endregion

        #region Unity Lifecycle Methods
        private void OnEnable()
        {
            if (_activeFormula == null)
            {
                // 编译重载（Domain Reload）或丢失数值配置时立即销毁，彻底杜绝幽灵窗口
                Close();
                return;
            }

            wantsMouseMove = true;
            _isCurveDataDirty = true;
        }

        private void OnDisable()
        {
            if (_instance == this)
            {
                _instance = null;
                _boundTargetObject = null;
                _boundPropertyPath = null;
            }
            EditorApplication.update -= OnEditorUpdate;
        }

        private void OnLostFocus()
        {
            if (!_isPinned)
            {
                ClosePopup();
            }
        }

        private void OnGUI()
        {
            // 若当前窗口没有对应的数值配置（如编译重载后或意外脱离宿主），绝不渲染任何底框，直接销毁退出
            if (_activeFormula == null)
            {
                Close();
                GUIUtility.ExitGUI();
                return;
            }

            // 只要鼠标位于弹窗整个矩形区域内（包括画布、边缘、Header、边框与内边距），持续刷新悬停时间戳
            Rect windowBounds = new Rect(0, 0, position.width, position.height);
            if (windowBounds.Contains(Event.current.mousePosition))
            {
                _lastHoverTime = EditorApplication.timeSinceStartup;
            }

            // 绘制弹窗整体底板
            EditorGUI.DrawRect(windowBounds, _windowBgColor);

            DrawCurveCanvas();

            // 在最顶层绘制窗口 4 边精确像素内边框（确保上下左右四边绝对完整、不受视口裁剪影响）
            DrawPixelOutline(windowBounds, new Color(0.45f, 0.48f, 0.58f, 1f), 1f);

            // 仅在鼠标在画布内移动时触发重绘，以流畅跟随准星；静止时 0 CPU 消耗
            if (Event.current.type == EventType.MouseMove && _cachedCanvasRect.Contains(Event.current.mousePosition))
            {
                Repaint();
            }
        }
        #endregion

        #region Public Methods
        /// <summary>
        /// 外部悬停调用：通知鼠标正在预览触发区域上方。
        /// </summary>
        public static void RequestHover(Rect triggerGuiRect, FormulaBase formula, string title, SerializedProperty property = null)
        {
            if (formula == null) return;

            // 若弹窗已锁定到其他属性，单纯鼠标移过不强行打扰已锁定的视图
            if (_instance != null && _isPinned && property != null && !IsBoundTo(property))
            {
                return;
            }

            _lastHoverTime = EditorApplication.timeSinceStartup;
            _activeFormula = formula;
            _formulaTitle = title;
            if (property != null)
            {
                _boundTargetObject = property.serializedObject.targetObject;
                _boundPropertyPath = property.propertyPath;
            }

            Vector2 triggerTopLeft = EditorGUIUtility.GUIToScreenPoint(new Vector2(triggerGuiRect.x, triggerGuiRect.y));
            _triggerScreenRect = new Rect(triggerTopLeft.x, triggerTopLeft.y, triggerGuiRect.width, triggerGuiRect.height);

            if (_instance == null)
            {
                _hostWindow = EditorWindow.mouseOverWindow;
                _instance = CreateInstance<FormulaPreviewPopup>();
                _instance.wantsMouseMove = true;
                _isCurveDataDirty = true;

                UpdatePopupPosition(triggerTopLeft, triggerGuiRect);
                _instance.ShowPopup();

                EditorApplication.update -= OnEditorUpdate;
                EditorApplication.update += OnEditorUpdate;
            }
            else
            {
                UpdatePopupPosition(triggerTopLeft, triggerGuiRect);
                NotifyFormula(formula, title, property);
            }
        }

        /// <summary>
        /// 外部数据通知：由 Inspector 在渲染或数值改动时驱动，按需通知脏标记。
        /// 具备属性绑定保护：仅接收当前绑定字段的通知，避免多公式字段相互覆盖。
        /// </summary>
        public static void NotifyFormula(FormulaBase formula, string title, SerializedProperty property = null)
        {
            if (_instance == null || formula == null) return;
            if (property != null && !IsBoundTo(property)) return;

            int newFingerprint = ComputeFormulaFingerprint(formula);
            if (newFingerprint != _cachedFormulaFingerprint || _activeFormula != formula || _formulaTitle != title)
            {
                _activeFormula = formula;
                _formulaTitle = title;
                _isCurveDataDirty = true;
                _instance.Repaint();
            }
        }

        /// <summary>
        /// 切换锁定状态（点击预览按钮触发）。
        /// 支持点击不同字段按钮时平滑切换绑定并保持锁定。
        /// </summary>
        public static void TogglePin(Rect triggerGuiRect, FormulaBase formula, string title, SerializedProperty property = null)
        {
            bool isSameProperty = property != null && IsBoundTo(property);

            if (_instance == null)
            {
                RequestHover(triggerGuiRect, formula, title, property);
                _isPinned = true;
            }
            else
            {
                if (isSameProperty)
                {
                    _isPinned = !_isPinned;
                    if (!_isPinned)
                    {
                        ClosePopup();
                    }
                    else
                    {
                        _instance.Repaint();
                    }
                }
                else
                {
                    // 点击了不同的公式字段：切换绑定到该字段并保持固定 (Pinned)
                    _isPinned = true;
                    if (property != null)
                    {
                        _boundTargetObject = property.serializedObject.targetObject;
                        _boundPropertyPath = property.propertyPath;
                    }
                    _activeFormula = formula;
                    _formulaTitle = title;
                    _lastHoverTime = EditorApplication.timeSinceStartup;

                    Vector2 triggerTopLeft = EditorGUIUtility.GUIToScreenPoint(new Vector2(triggerGuiRect.x, triggerGuiRect.y));
                    _triggerScreenRect = new Rect(triggerTopLeft.x, triggerTopLeft.y, triggerGuiRect.width, triggerGuiRect.height);
                    UpdatePopupPosition(triggerTopLeft, triggerGuiRect);

                    _isCurveDataDirty = true;
                    _instance.Repaint();
                }
            }
        }

        /// <summary>
        /// 关闭浮动弹窗并重置绑定。
        /// </summary>
        public static void ClosePopup()
        {
            _isPinned = false;
            _boundTargetObject = null;
            _boundPropertyPath = null;
            if (_instance != null)
            {
                _instance.Close();
                _instance = null;
            }
            EditorApplication.update -= OnEditorUpdate;
        }
        #endregion

        #region Private Methods
        private static void UpdatePopupPosition(Vector2 triggerTopLeft, Rect triggerGuiRect)
        {
            if (_instance == null) return;

            // 尺寸极简紧凑优化为 310 x 150，彻底消除顶部与底部冗余空白
            float width = 310f;
            float height = 150f;
            float posX = triggerTopLeft.x - (width - triggerGuiRect.width) * 0.5f;

            // 默认向上弹出在条目正上方，绝不遮挡下方展开的数值配置项
            float posY = triggerTopLeft.y - height - 3f;

            // 若靠屏幕顶边缘空间不足，才退回向下弹出
            if (posY < 35f)
            {
                posY = triggerTopLeft.y + triggerGuiRect.height + 3f;
            }

            // 屏幕左右边界保护
            Resolution res = Screen.currentResolution;
            posX = Mathf.Clamp(posX, 10f, res.width - width - 10f);

            _instance.position = new Rect(posX, posY, width, height);
        }
        private static void OnEditorUpdate()
        {
            if (_instance == null)
            {
                EditorApplication.update -= OnEditorUpdate;
                return;
            }

            if (_isPinned)
            {
                // Pin 锁定模式下无需进行任何悬停计时与强制轮询
                return;
            }

            // 非锁定状态下仅用于检测光标是否离开整个有效区域
            bool isOverPopup = EditorWindow.mouseOverWindow == _instance;
            bool isOverHost = _hostWindow != null && EditorWindow.mouseOverWindow == _hostWindow;

            if (isOverPopup)
            {
                _lastHoverTime = EditorApplication.timeSinceStartup;
                return;
            }

            if (isOverHost)
            {
                // 鼠标在 Inspector 宿主窗口内，允许宿主重绘以捕获按钮悬停
                _hostWindow.Repaint();
            }

            // 当光标离开整个弹窗与触发按钮超过 250ms 时平滑销毁
            if (EditorApplication.timeSinceStartup - _lastHoverTime > 0.25f)
            {
                ClosePopup();
            }
        }

        private void DrawCurveCanvas()
        {
            // 曲线坐标系区域 (无顶部标题，直接从顶部 y=7f 开始；左侧 32px 紧贴左边框给 Y 轴刻度，底部紧凑排布)
            Rect canvasRect = new Rect(32f, 7f, position.width - 40f, 104f);

            // 1. 脏数据按需重算：仅当参数、模式、跨度或画布尺寸改变时，才执行采样计算
            int currentFingerprint = ComputeFormulaFingerprint(_activeFormula);
            if (_isCurveDataDirty 
                || currentFingerprint != _cachedFormulaFingerprint 
                || _selectedRangeIndex != _cachedRangeIndex 
                || canvasRect != _cachedCanvasRect)
            {
                RecomputeCurveData(canvasRect, currentFingerprint);
            }

            // 2. 纯静态绘制
            EditorGUI.DrawRect(canvasRect, _canvasBgColor);
            DrawPixelOutline(canvasRect, _canvasBorderColor, 1f);

            // 绘制网格线 (1/4 分隔，共 3 条内部细网格线)
            Handles.color = _gridLineColor;
            for (int i = 1; i <= 3; i++)
            {
                float norm = i * 0.25f;
                float gy = Mathf.Lerp(canvasRect.yMax, canvasRect.y, norm);
                Handles.DrawLine(new Vector3(canvasRect.x, gy, 0f), new Vector3(canvasRect.xMax, gy, 0f));

                float gx = Mathf.Lerp(canvasRect.x, canvasRect.xMax, norm);
                Handles.DrawLine(new Vector3(gx, canvasRect.y, 0f), new Vector3(gx, canvasRect.yMax, 0f));
            }

            // 统一抗锯齿曲线绘制
            Handles.color = _curveColor;
            Handles.DrawAAPolyLine(2.0f, _curvePointsBuffer);

            // 绘制起始基准锚点
            if (_cachedHasStartAnchor && _cachedFormulaStartX >= _cachedAxisMinX && _cachedFormulaStartX <= _cachedAxisMaxX)
            {
                Handles.color = new Color(1f, 1f, 1f, 0.2f);
                Handles.DrawDottedLine(_cachedAnchorPos, new Vector3(_cachedAnchorPos.x, canvasRect.yMax, 0f), 2f);

                // 轻量级微型空心环（中心镂空透明，绝不遮挡曲线细节）
                Handles.color = new Color(0.35f, 0.85f, 1f, 0.9f);
                Handles.DrawWireDisc(_cachedAnchorPos, Vector3.forward, 2.2f);
                Handles.DrawSolidDisc(_cachedAnchorPos, Vector3.forward, 0.8f);
            }

            // 绘制 Y 轴 1/4 分隔刻度标签 (0, 0.25, 0.5, 0.75, 1.0 共 5 个刻度，紧贴窗口左边框)
            GUIStyle yAxisLabelStyle = new GUIStyle(EditorStyles.miniLabel)
            {
                fontSize = 9,
                alignment = TextAnchor.MiddleRight,
                normal = { textColor = new Color(0.65f, 0.65f, 0.65f) }
            };
            for (int i = 0; i <= 4; i++)
            {
                float norm = i * 0.25f;
                float gy = Mathf.Lerp(canvasRect.yMax, canvasRect.y, norm);
                float val = Mathf.Lerp(_cachedMinY, _cachedMaxY, norm);
                Rect yRect = new Rect(2f, gy - 6f, 28f, 12f);
                GUI.Label(yRect, FormatCompactNumber(val), yAxisLabelStyle);
            }

            // 绘制 X 轴 1/4 分隔刻度标签 (0, 0.25, 0.5, 0.75, 1.0 共 5 个刻度)
            GUIStyle xAxisLabelStyle = new GUIStyle(EditorStyles.miniLabel)
            {
                fontSize = 9,
                normal = { textColor = new Color(0.55f, 0.55f, 0.55f) }
            };
            float xLabelY = canvasRect.yMax + 1f;
            for (int i = 0; i <= 4; i++)
            {
                float norm = i * 0.25f;
                float gx = Mathf.Lerp(canvasRect.x, canvasRect.xMax, norm);
                float val = Mathf.Lerp(_cachedAxisMinX, _cachedAxisMaxX, norm);
                string valStr = Mathf.RoundToInt(val).ToString();

                Rect xRect;
                if (i == 0)
                {
                    xAxisLabelStyle.alignment = TextAnchor.MiddleLeft;
                    xRect = new Rect(gx, xLabelY, 30f, 12f);
                }
                else if (i == 4)
                {
                    xAxisLabelStyle.alignment = TextAnchor.MiddleRight;
                    xRect = new Rect(gx - 30f, xLabelY, 30f, 12f);
                }
                else
                {
                    xAxisLabelStyle.alignment = TextAnchor.MiddleCenter;
                    xRect = new Rect(gx - 18f, xLabelY, 36f, 12f);
                }

                GUI.Label(xRect, valStr, xAxisLabelStyle);
            }

            // 3. 鼠标交互十字准星：仅在光标位于画布上时按需进行单点求值
            Vector2 mousePos = Event.current.mousePosition;
            bool isHoveringCanvas = canvasRect.Contains(mousePos);
            int hoverX = 0;
            float hoverY = 0f;

            if (isHoveringCanvas)
            {
                float hoverNormX = Mathf.Clamp01((mousePos.x - canvasRect.x) / canvasRect.width);
                hoverX = Mathf.RoundToInt(Mathf.Lerp(_cachedAxisMinX, _cachedAxisMaxX, hoverNormX));
                try
                {
                    hoverY = _activeFormula.Evaluate(hoverX);
                }
                catch (Exception)
                {
                    hoverY = 0f;
                }

                float crossX = Mathf.Lerp(canvasRect.x, canvasRect.xMax, (hoverX - _cachedAxisMinX) / (_cachedAxisMaxX - _cachedAxisMinX));
                float crossY = Mathf.Lerp(canvasRect.yMax, canvasRect.y, Mathf.Clamp01((hoverY - _cachedPaddedMinY) / _cachedPaddedSpan));

                Handles.color = new Color(1f, 1f, 1f, 0.35f);
                Handles.DrawDottedLine(new Vector3(crossX, canvasRect.y, 0f), new Vector3(crossX, canvasRect.yMax, 0f), 2f);

                // 轻盈微型空心准星环（中心镂空透光，针尖式微点，不遮挡大尺度数据走势）
                Handles.color = _cursorCrossColor;
                Handles.DrawWireDisc(new Vector3(crossX, crossY, 0f), Vector3.forward, 2.5f);
                Handles.DrawSolidDisc(new Vector3(crossX, crossY, 0f), Vector3.forward, 0.8f);
            }

            // 4. 底部相邻操作工具栏（按钮下移紧贴底边，消除冗余空白）
            float footerY = canvasRect.yMax + 14f;
            float rightAreaX = canvasRect.xMax;

            // 关闭按钮 x（18x18 严格正方形按钮，零 padding 保证字符完整居中呼吸不被裁切，右侧严格与画布右边界对齐）
            float closeBtnSize = 18f;
            rightAreaX -= closeBtnSize;
            Rect closeRect = new Rect(rightAreaX, footerY, closeBtnSize, closeBtnSize);
            GUIStyle closeBtnStyle = new GUIStyle(EditorStyles.miniButton)
            {
                fontSize = 10,
                alignment = TextAnchor.MiddleCenter,
                padding = new RectOffset(0, 0, 0, 0),
                margin = new RectOffset(0, 0, 0, 0)
            };
            if (GUI.Button(closeRect, new GUIContent("x", "Close preview window"), closeBtnStyle))
            {
                ClosePopup();
                return;
            }

            // 钉住按钮 Pin
            float pinBtnWidth = 34f;
            rightAreaX -= (pinBtnWidth + 3f);
            Rect pinRect = new Rect(rightAreaX, footerY, pinBtnWidth, 18f);
            Color prevBg = GUI.backgroundColor;
            if (_isPinned)
            {
                GUI.backgroundColor = new Color(0.35f, 0.75f, 1f, 1f);
            }
            if (GUI.Button(pinRect, "Pin", EditorStyles.miniButton))
            {
                _isPinned = !_isPinned;
            }
            GUI.backgroundColor = prevBg;

            // 范围选择按钮样式：紧凑 padding，字号优化，保证 "100" 完整呼吸居中
            GUIStyle rangeBtnStyle = new GUIStyle(EditorStyles.miniButton)
            {
                fontSize = 9,
                alignment = TextAnchor.MiddleCenter,
                padding = new RectOffset(1, 1, 0, 0)
            };

            // 范围选择按钮 (20, 50, 100)，"100" 特别分配 38px 宽度以完整容纳
            for (int i = _rangeOptions.Length - 1; i >= 0; i--)
            {
                int optValue = _rangeOptions[i];
                float rWidth = optValue >= 100 ? 38f : 28f;

                rightAreaX -= (rWidth + 2f);
                Rect rRect = new Rect(rightAreaX, footerY, rWidth, 18f);
                bool isSelected = _selectedRangeIndex == i;

                if (isSelected) GUI.backgroundColor = new Color(0.4f, 0.4f, 0.45f, 1f);
                if (GUI.Button(rRect, optValue.ToString(), rangeBtnStyle))
                {
                    if (_selectedRangeIndex != i)
                    {
                        _selectedRangeIndex = i;
                        _isCurveDataDirty = true;
                        Repaint();
                    }
                }
                if (isSelected) GUI.backgroundColor = prevBg;
            }

            // 左侧：单点准星读数 / 提示文本（起始点推至 canvasRect.x，与坐标系垂直对齐）
            float readOutX = canvasRect.x;
            float readOutWidth = Mathf.Max(20f, rightAreaX - readOutX - 4f);
            Rect readOutRect = new Rect(readOutX, footerY, readOutWidth, 18f);
            GUIStyle footerStyle = new GUIStyle(EditorStyles.miniLabel)
            {
                fontSize = 10,
                alignment = TextAnchor.MiddleLeft,
                clipping = TextClipping.Ellipsis,
                normal = { textColor = new Color(0.85f, 0.85f, 0.85f) }
            };

            if (isHoveringCanvas)
            {
                string tag = (_cachedHasStartAnchor && hoverX <= _cachedFormulaStartX) ? " [Base]" : "";
                string readOut = $"X = {hoverX}  =>  Y = {Mathf.RoundToInt(hoverY):N0}{tag}";
                footerStyle.normal.textColor = _cursorCrossColor;
                GUI.Label(readOutRect, readOut, footerStyle);
            }
            else
            {
                string defaultInfo = $"0 -> {Mathf.RoundToInt(_cachedAxisMaxX)} | Start: {Mathf.RoundToInt(_cachedStartValY):N0}";
                GUI.Label(readOutRect, defaultInfo, footerStyle);
            }
        }

        private void RecomputeCurveData(Rect canvasRect, int fingerprint)
        {
            _cachedFormulaStartX = _activeFormula != null ? _activeFormula.StartX : 1f;
            _cachedHasStartAnchor = _activeFormula != null && _activeFormula.HasStartAnchor;

            _cachedAxisMinX = Mathf.Min(0f, _cachedFormulaStartX);
            int baseRange = _rangeOptions[_selectedRangeIndex];
            _cachedAxisMaxX = Mathf.Max(baseRange, _cachedFormulaStartX + 5f);

            // 采样点分布生成：全区间高密均匀采样（120个点，平均每2px一个点，步长恒定严格单调递增）
            // 彻底消除任何零长度线段、法线除零退化与特殊分支判断
            int sampleCount = 120;
            float span = _cachedAxisMaxX - _cachedAxisMinX;
            float step = span / (sampleCount - 1);

            _cachedMinY = float.MaxValue;
            _cachedMaxY = float.MinValue;

            for (int i = 0; i < sampleCount; i++)
            {
                float x = _cachedAxisMinX + i * step;
                _xSamplesBuffer[i] = x;
                float y = 0f;
                try
                {
                    y = _activeFormula.Evaluate(x);
                }
                catch (Exception)
                {
                    y = 0f;
                }

                if (float.IsNaN(y) || float.IsInfinity(y)) y = 0f;
                _yValuesBuffer[i] = y;

                if (y < _cachedMinY) _cachedMinY = y;
                if (y > _cachedMaxY) _cachedMaxY = y;
            }

            if (Mathf.Approximately(_cachedMinY, _cachedMaxY))
            {
                _cachedMinY -= 1f;
                _cachedMaxY += 1f;
            }

            if (_cachedMinY >= 0f && _cachedMinY <= (_cachedMaxY - _cachedMinY) * 0.6f)
            {
                _cachedMinY = 0f;
            }

            float ySpan = _cachedMaxY - _cachedMinY;
            _cachedPaddedMinY = _cachedMinY - ySpan * 0.05f;
            float paddedMaxY = _cachedMaxY + ySpan * 0.05f;
            _cachedPaddedSpan = paddedMaxY - _cachedPaddedMinY;

            // 统一转换屏幕坐标顶点（单循环线性映射）
            for (int i = 0; i < sampleCount; i++)
            {
                float normX = Mathf.Clamp01((_xSamplesBuffer[i] - _cachedAxisMinX) / span);
                float normY = Mathf.Clamp01((_yValuesBuffer[i] - _cachedPaddedMinY) / _cachedPaddedSpan);

                float sx = Mathf.Lerp(canvasRect.x, canvasRect.xMax, normX);
                float sy = Mathf.Lerp(canvasRect.yMax, canvasRect.y, normY);
                _curvePointsBuffer[i] = new Vector3(sx, sy, 0f);
            }

            // 计算起始基准锚点（仅用于在上层绘制提示圆盘与参考虚线，不参与曲线生成）
            _cachedStartValY = 0f;
            try { _cachedStartValY = _activeFormula.Evaluate(_cachedFormulaStartX); } catch { _cachedStartValY = 0f; }

            if (_cachedHasStartAnchor && _cachedFormulaStartX >= _cachedAxisMinX && _cachedFormulaStartX <= _cachedAxisMaxX)
            {
                float startNormX = Mathf.Clamp01((_cachedFormulaStartX - _cachedAxisMinX) / span);
                float startNormY = Mathf.Clamp01((_cachedStartValY - _cachedPaddedMinY) / _cachedPaddedSpan);
                float anchorSx = Mathf.Lerp(canvasRect.x, canvasRect.xMax, startNormX);
                float anchorSy = Mathf.Lerp(canvasRect.yMax, canvasRect.y, startNormY);
                _cachedAnchorPos = new Vector3(anchorSx, anchorSy, 0f);
            }

            _isCurveDataDirty = false;
            _cachedFormulaFingerprint = fingerprint;
            _cachedRangeIndex = _selectedRangeIndex;
            _cachedCanvasRect = canvasRect;
        }

        private static int ComputeFormulaFingerprint(FormulaBase formula)
        {
            if (formula == null) return 0;
            return formula.GetParameterFingerprint();
        }

        private static string FormatCompactNumber(float value)
        {
            float abs = Mathf.Abs(value);
            if (abs >= 1000000f) return (value / 1000000f).ToString("0.#") + "M";
            if (abs >= 10000f) return (value / 1000f).ToString("0.#") + "k";
            return Mathf.RoundToInt(value).ToString("N0");
        }

        /// <summary>
        /// 绘制像素级精确内边框。
        /// 彻底替代 Handles.DrawSolidRectangleWithOutline，彻底避免在视口边缘 (0, 0) 处因半像素裁剪而缺失上边与左边的问题。
        /// </summary>
        private static void DrawPixelOutline(Rect rect, Color color, float thickness = 1f)
        {
            // 顶边：严格处于 rect.y 像素以内
            EditorGUI.DrawRect(new Rect(rect.x, rect.y, rect.width, thickness), color);
            // 底边：严格处于 rect.yMax - thickness 像素以内
            EditorGUI.DrawRect(new Rect(rect.x, rect.yMax - thickness, rect.width, thickness), color);
            // 左边：严格处于 rect.x 像素以内
            EditorGUI.DrawRect(new Rect(rect.x, rect.y, thickness, rect.height), color);
            // 右边：严格处于 rect.xMax - thickness 像素以内
            EditorGUI.DrawRect(new Rect(rect.xMax - thickness, rect.y, thickness, rect.height), color);
        }

        [InitializeOnLoadMethod]
        private static void InitAssemblyReloadHandler()
        {
            AssemblyReloadEvents.beforeAssemblyReload -= ClosePopup;
            AssemblyReloadEvents.beforeAssemblyReload += ClosePopup;
        }
        #endregion
    }
}
