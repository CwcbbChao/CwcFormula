using System;
using UnityEngine;

namespace Cwcbb.Tools.CwcFormula
{
    /// <summary>
    /// 双曲渐近线 / 饱和增长公式。
    /// 典型应用：边际效益递减模型、带理论极值上限的成长（如好感度/熟练度软上限）、护甲与抗性减伤（收益递减逼近上限）等。
    /// 当自变量小于等于起始点X时，恒定保持起始点Y；当自变量向右延伸时，数值平滑向理论上限 MaxValue 逼近，斜率递减，永不超出上限。
    /// 公式：y = LerpUnclamped(StartPoint.Y, MaxValue, (inputParam - StartPoint.X) / ((inputParam - StartPoint.X) + HalfSpan))
    /// </summary>
    [Serializable]
    [FormulaStrategy("Asymptotic", "Asymptotic", 35)]
    public class AsymptoticFormula : FormulaBase
    {
        #region Inspector Fields
        [Tooltip("Base point (X: base input, Y: base value). Held constant for input <= X.")]
        [SerializeField] private Vector2 _startPoint = new Vector2(1f, 1f);

        [Tooltip("Theoretical upper limit as input approaches infinity.")]
        [SerializeField] private float _maxValue = 2f;

        [Min(0.001f)]
        [Tooltip("Input delta to reach 50% of the max value range.")]
        [SerializeField] private float _halfSpan = 10f;
        #endregion

        #region Public Properties
        public override string FormulaName => "Asymptotic";
        public override float StartX => _startPoint.x;
        public override bool HasStartAnchor => true;
        public Vector2 StartPoint { get => _startPoint; set => _startPoint = value; }
        public float MaxValue { get => _maxValue; set => _maxValue = value; }
        public float HalfSpan { get => _halfSpan; set => _halfSpan = Mathf.Max(0.001f, value); }
        #endregion

        #region Public Methods
        public AsymptoticFormula()
        {
            _startPoint = new Vector2(1f, 1f);
            _maxValue = 2f;
            _halfSpan = 10f;
        }

        public AsymptoticFormula(Vector2 startPoint, float maxValue, float halfSpan)
        {
            _startPoint = startPoint;
            _maxValue = maxValue;
            _halfSpan = Mathf.Max(0.001f, halfSpan);
        }

        /// <summary>
        /// 根据输入参数评估渐近线数值。
        /// </summary>
        public override float Evaluate(float inputParam)
        {
            if (inputParam <= _startPoint.x)
            {
                return _startPoint.y;
            }

            float deltaX = inputParam - _startPoint.x;
            float safeHalf = Mathf.Max(0.001f, _halfSpan);
            float ratio = deltaX / (deltaX + safeHalf);

            return Mathf.LerpUnclamped(_startPoint.y, _maxValue, ratio);
        }

        /// <summary>
        /// 深度克隆当前渐近线公式实例。
        /// </summary>
        public override FormulaBase Clone()
        {
            return new AsymptoticFormula(_startPoint, _maxValue, _halfSpan);
        }

        public override int GetParameterFingerprint()
        {
            unchecked
            {
                int hash = GetType().GetHashCode();
                hash = (hash * 397) ^ _startPoint.GetHashCode();
                hash = (hash * 397) ^ _maxValue.GetHashCode();
                hash = (hash * 397) ^ _halfSpan.GetHashCode();
                return hash;
            }
        }
        #endregion
    }
}
