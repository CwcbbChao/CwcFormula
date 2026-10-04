using System;
using System.Collections.Generic;
using UnityEngine;

namespace Cwcbb.Tools.CwcFormula
{
    /// <summary>
    /// 分段复合计算公式。
    /// 参考并升级自 SegmentedValue，支持多段组合（线性、指数、动画曲线），
    /// 支持纯 C# 序列化与自适应预计算，无任何外部插件依赖。
    /// </summary>
    [Serializable]
    [FormulaStrategy("Segmented", "Segmented", 30)]
    public class SegmentedFormula : FormulaBase, ISerializationCallbackReceiver
    {
        #region Nested Types
        /// <summary>
        /// 分段计算类型枚举。
        /// </summary>
        public enum SegmentType
        {
            Linear,         // 线性插值
            Power,          // 幂函数曲线（支持指数配置：< 1 前期快后期慢，> 1 前期慢后期快）
            Exponential,    // 复利指数增长（几何倍率）
            AnimationCurve  // 动画曲线插值（自定义形状）
        }

        /// <summary>
        /// 单个计算分段数据项。
        /// </summary>
        [Serializable]
        public class Segment
        {
            #region Inspector Fields
            [Tooltip("Segment algorithm type.")]
            [SerializeField] private SegmentType _type = SegmentType.Linear;

            [Min(0.001f)]
            [Tooltip("Parameter span covered by this segment.")]
            [SerializeField] private float _parameterDelta = 10f;

            [Tooltip("Total value change over this segment.")]
            [SerializeField] private float _valueChange = 10f;

            [Min(0.01f)]
            [Tooltip("Power exponent.")]
            [SerializeField] private float _exponent = 0.7f;

            [Min(0f)]
            [Tooltip("Geometric growth multiplier per unit.")]
            [SerializeField] private float _growthFactor = 1.15f;

            [Tooltip("Progression shape curve.")]
            [SerializeField] private AnimationCurve _curve = AnimationCurve.Linear(0f, 0f, 1f, 1f);
            #endregion

            #region NonSerialized Cache
            [NonSerialized] private float _cachedStartParam;
            [NonSerialized] private float _cachedStartValue;
            #endregion

            #region Public Properties
            public SegmentType Type { get => _type; set => _type = value; }
            public float ParameterDelta { get => _parameterDelta; set => _parameterDelta = Mathf.Max(0.001f, value); }
            public float ValueChange { get => _valueChange; set => _valueChange = value; }
            public float Exponent { get => _exponent; set => _exponent = Mathf.Max(0.01f, value); }
            public float GrowthFactor { get => _growthFactor; set => _growthFactor = Mathf.Max(0f, value); }
            public AnimationCurve Curve { get => _curve; set => _curve = value; }
            public float CachedStartParam { get => _cachedStartParam; set => _cachedStartParam = value; }
            public float CachedStartValue { get => _cachedStartValue; set => _cachedStartValue = value; }
            #endregion

            #region Public Methods
            public Segment()
            {
                _type = SegmentType.Linear;
                _parameterDelta = 10f;
                _valueChange = 10f;
                _exponent = 0.7f;
                _growthFactor = 1.15f;
                _curve = AnimationCurve.Linear(0f, 0f, 1f, 1f);
            }

            public Segment(SegmentType type, float delta, float valueChange, float exponent, float growthFactor, AnimationCurve curve)
            {
                _type = type;
                _parameterDelta = Mathf.Max(0.001f, delta);
                _valueChange = valueChange;
                _exponent = Mathf.Max(0.01f, exponent);
                _growthFactor = Mathf.Max(0f, growthFactor);
                _curve = curve != null ? new AnimationCurve(curve.keys) : AnimationCurve.Linear(0f, 0f, 1f, 1f);
            }

            /// <summary>
            /// 根据段内进度评估数值。
            /// </summary>
            /// <param name="progress">段内相对进度 (0.0 ~ 1.0)</param>
            /// <returns>求值结果</returns>
            public float Evaluate(float progress)
            {
                return EvaluateStatic(_type, _cachedStartValue, _valueChange, _exponent, _growthFactor, _curve, _parameterDelta, progress);
            }

            /// <summary>
            /// 静态评估方法，供编辑器无 GC 调用。
            /// </summary>
            public static float EvaluateStatic(
                SegmentType type,
                float startValue,
                float valueChange,
                float exponent,
                float growthFactor,
                AnimationCurve curve,
                float parameterDelta,
                float progress)
            {
                float localParam = progress * parameterDelta;

                switch (type)
                {
                    case SegmentType.Linear:
                        return Mathf.LerpUnclamped(startValue, startValue + valueChange, progress);

                    case SegmentType.Power:
                        float safeExp = Mathf.Max(0.01f, exponent);
                        float powerProgress = Mathf.Pow(Mathf.Max(0f, progress), safeExp);
                        return Mathf.LerpUnclamped(startValue, startValue + valueChange, powerProgress);

                    case SegmentType.Exponential:
                        float safeFactor = Mathf.Max(0f, growthFactor);
                        return startValue * Mathf.Pow(safeFactor, localParam);

                    case SegmentType.AnimationCurve:
                        float curveProgress = curve != null ? curve.Evaluate(progress) : progress;
                        return Mathf.LerpUnclamped(startValue, startValue + valueChange, curveProgress);

                    default:
                        return startValue;
                }
            }

            /// <summary>
            /// 克隆当前分段。
            /// </summary>
            public Segment Clone()
            {
                var clonedCurve = _curve != null ? new AnimationCurve(_curve.keys) : AnimationCurve.Linear(0f, 0f, 1f, 1f);
                var seg = new Segment(_type, _parameterDelta, _valueChange, _exponent, _growthFactor, clonedCurve);
                seg.CachedStartParam = _cachedStartParam;
                seg.CachedStartValue = _cachedStartValue;
                return seg;
            }
            #endregion
        }
        #endregion

        #region Inspector Fields
        [Tooltip("Base point (X: initial input, Y: initial value).")]
        [SerializeField] private Vector2 _startPoint = new Vector2(1f, 1f);

        [Tooltip("List of calculation segments.")]
        [SerializeField] private List<Segment> _segments = new List<Segment>();
        #endregion

        #region Private Fields
        [NonSerialized] private bool _isCacheValid = false;
        #endregion

        #region Public Properties
        public override string FormulaName => "Segmented";
        public override float StartX => _startPoint.x;
        public override bool HasStartAnchor => true;
        public Vector2 StartPoint { get => _startPoint; set { _startPoint = value; InvalidateCache(); } }
        public List<Segment> Segments => _segments;
        #endregion

        #region Lifecycle & Serialization Callbacks
        public void OnBeforeSerialize()
        {
        }

        public void OnAfterDeserialize()
        {
            _isCacheValid = false;
        }
        #endregion

        #region Public Methods
        public SegmentedFormula()
        {
            _startPoint = new Vector2(1f, 1f);
            _segments = new List<Segment>
            {
                new Segment(SegmentType.Linear, 9f, 1f, 1.0f, 1.15f, null),
                new Segment(SegmentType.Linear, 20f, 2f, 1.0f, 1.15f, null)
            };
            _isCacheValid = false;
        }

        public SegmentedFormula(Vector2 startPoint, List<Segment> segments)
        {
            _startPoint = startPoint;
            _segments = segments ?? new List<Segment>();
            _isCacheValid = false;
        }

        /// <summary>
        /// 根据输入参数计算最终数值。
        /// </summary>
        public override float Evaluate(float inputParam)
        {
            EnsureCacheValid();

            if (_segments == null || _segments.Count == 0)
            {
                return _startPoint.y;
            }

            if (inputParam <= _startPoint.x)
            {
                return _startPoint.y;
            }

            for (int i = 0; i < _segments.Count; i++)
            {
                var segment = _segments[i];
                if (segment == null)
                {
                    continue;
                }

                float segmentStart = segment.CachedStartParam;
                float delta = Mathf.Max(0.001f, segment.ParameterDelta);
                float segmentEnd = segmentStart + delta;

                // 若处于当前段区间内，或者已到达最后一段（向后延续发散）
                if (inputParam <= segmentEnd || i == _segments.Count - 1)
                {
                    float rawProgress = (inputParam - segmentStart) / delta;
                    // 中间段严格截断在 [0, 1]，最后一段允许大于 1 从而随参数向后外推发散趋势
                    float progress = (i == _segments.Count - 1) ? Mathf.Max(0f, rawProgress) : Mathf.Clamp01(rawProgress);
                    return segment.Evaluate(progress);
                }
            }

            return _startPoint.y;
        }

        /// <summary>
        /// 深度克隆整个分段公式。
        /// </summary>
        public override FormulaBase Clone()
        {
            var clonedSegments = new List<Segment>(_segments.Count);
            for (int i = 0; i < _segments.Count; i++)
            {
                if (_segments[i] != null)
                {
                    clonedSegments.Add(_segments[i].Clone());
                }
            }

            var clonedFormula = new SegmentedFormula(_startPoint, clonedSegments);
            clonedFormula.PrecomputeSegmentData();
            return clonedFormula;
        }

        /// <summary>
        /// 预计算所有分段缓存起点，提升运行时查询效率。
        /// </summary>
        public void PrecomputeSegmentData()
        {
            if (_segments == null)
            {
                _isCacheValid = true;
                return;
            }

            float currentParam = _startPoint.x;
            float currentValue = _startPoint.y;

            for (int i = 0; i < _segments.Count; i++)
            {
                var segment = _segments[i];
                if (segment == null)
                {
                    continue;
                }

                segment.CachedStartParam = currentParam;
                segment.CachedStartValue = currentValue;

                currentValue = segment.Evaluate(1.0f);
                currentParam += Mathf.Max(0.001f, segment.ParameterDelta);
            }

            _isCacheValid = true;
        }

        /// <summary>
        /// 标记缓存失效，下一次计算将自动重新预计算。
        /// </summary>
        public void InvalidateCache()
        {
            _isCacheValid = false;
        }

        public override int GetParameterFingerprint()
        {
            unchecked
            {
                int hash = GetType().GetHashCode();
                hash = (hash * 397) ^ _startPoint.GetHashCode();
                if (_segments != null)
                {
                    hash = (hash * 397) ^ _segments.Count;
                    for (int i = 0; i < _segments.Count; i++)
                    {
                        var s = _segments[i];
                        if (s == null) continue;
                        hash = (hash * 397) ^ (int)s.Type;
                        hash = (hash * 397) ^ s.ParameterDelta.GetHashCode();
                        hash = (hash * 397) ^ s.ValueChange.GetHashCode();
                        hash = (hash * 397) ^ s.Exponent.GetHashCode();
                        hash = (hash * 397) ^ s.GrowthFactor.GetHashCode();
                    }
                }
                return hash;
            }
        }
        #endregion

        #region Private Methods
        private void EnsureCacheValid()
        {
            if (!_isCacheValid)
            {
                PrecomputeSegmentData();
            }
        }
        #endregion
    }
}
