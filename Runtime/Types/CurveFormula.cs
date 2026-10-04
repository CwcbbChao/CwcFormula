using System;
using UnityEngine;

namespace Cwcbb.Tools.CwcFormula
{
    /// <summary>
    /// 基于 Unity AnimationCurve 的动画曲线公式。
    /// 支持参数位移与缩放，以及数值位移与缩放。
    /// 公式：y = ValueOffset + ValueScale * Curve.Evaluate((inputParam - ParamOffset) * ParamScale)
    /// </summary>
    [Serializable]
    [FormulaStrategy("Curve", "Curve", 40)]
    public class CurveFormula : FormulaBase
    {
        #region Inspector Fields
        [Tooltip("Evaluation curve.")]
        [SerializeField] private AnimationCurve _curve = AnimationCurve.Linear(0f, 1f, 9f, 2f);

        [Tooltip("Input parameter offset.")]
        [SerializeField] private float _paramOffset = 1f;

        [Tooltip("Input parameter scale.")]
        [SerializeField] private float _paramScale = 1f;

        [Tooltip("Output value offset.")]
        [SerializeField] private float _valueOffset = 0f;

        [Tooltip("Output value scale.")]
        [SerializeField] private float _valueScale = 1f;
        #endregion

        #region Public Properties
        public override string FormulaName => "Curve";
        public override float StartX => _paramOffset;
        public override bool HasStartAnchor => true;
        public AnimationCurve Curve => _curve;
        public float ParamOffset => _paramOffset;
        public float ParamScale => _paramScale;
        public float ValueOffset => _valueOffset;
        public float ValueScale => _valueScale;
        #endregion

        #region Public Methods
        public CurveFormula()
        {
            _curve = AnimationCurve.Linear(0f, 1f, 9f, 2f);
            _paramOffset = 1f;
            _paramScale = 1f;
            _valueOffset = 0f;
            _valueScale = 1f;
        }

        public CurveFormula(AnimationCurve curve, float paramOffset = 0f, float paramScale = 1f, float valueOffset = 0f, float valueScale = 1f)
        {
            _curve = curve != null ? new AnimationCurve(curve.keys) : AnimationCurve.Linear(0f, 0f, 1f, 1f);
            _paramOffset = paramOffset;
            _paramScale = paramScale;
            _valueOffset = valueOffset;
            _valueScale = valueScale;
        }

        public override float Evaluate(float inputParam)
        {
            if (_curve == null)
            {
                return _valueOffset;
            }

            float normalizedParam = (inputParam - _paramOffset) * _paramScale;
            float curveValue = _curve.Evaluate(normalizedParam);
            return _valueOffset + _valueScale * curveValue;
        }

        public override FormulaBase Clone()
        {
            var clonedCurve = _curve != null ? new AnimationCurve(_curve.keys) : AnimationCurve.Linear(0f, 0f, 1f, 1f);
            return new CurveFormula(clonedCurve, _paramOffset, _paramScale, _valueOffset, _valueScale);
        }

        public override int GetParameterFingerprint()
        {
            unchecked
            {
                int hash = GetType().GetHashCode();
                hash = (hash * 397) ^ _paramOffset.GetHashCode();
                hash = (hash * 397) ^ _paramScale.GetHashCode();
                hash = (hash * 397) ^ _valueOffset.GetHashCode();
                hash = (hash * 397) ^ _valueScale.GetHashCode();
                if (_curve != null) hash = (hash * 397) ^ _curve.length;
                return hash;
            }
        }
        #endregion
    }
}
