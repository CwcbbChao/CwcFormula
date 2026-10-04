using System;
using UnityEngine;

namespace Cwcbb.Tools.CwcFormula
{
    /// <summary>
    /// 一次线性公式。根据起始基准点与斜率进行线性增减。
    /// 当输入参数小于等于起始点X时，恒定保持起始点Y。
    /// 公式：y = StartPoint.Y + Slope * (inputParam - StartPoint.X)
    /// </summary>
    [Serializable]
    [FormulaStrategy("Linear", "Linear", 10)]
    public class LinearFormula : FormulaBase
    {
        #region Inspector Fields
        [Tooltip("Base point (X: base input, Y: base value). Held constant for input <= X.")]
        [SerializeField] private Vector2 _startPoint = new Vector2(1f, 1f);

        [Tooltip("Rate of change per unit input.")]
        [SerializeField] private float _slope = 1f;
        #endregion

        #region Public Properties
        public override string FormulaName => "Linear";
        public override float StartX => _startPoint.x;
        public override bool HasStartAnchor => true;
        public Vector2 StartPoint { get => _startPoint; set => _startPoint = value; }
        public float Slope { get => _slope; set => _slope = value; }
        #endregion

        #region Public Methods
        public LinearFormula()
        {
            _startPoint = new Vector2(1f, 1f);
            _slope = 1f;
        }

        public LinearFormula(Vector2 startPoint, float slope)
        {
            _startPoint = startPoint;
            _slope = slope;
        }

        public override float Evaluate(float inputParam)
        {
            if (inputParam <= _startPoint.x)
            {
                return _startPoint.y;
            }

            return _startPoint.y + _slope * (inputParam - _startPoint.x);
        }

        public override FormulaBase Clone()
        {
            return new LinearFormula(_startPoint, _slope);
        }

        public override int GetParameterFingerprint()
        {
            unchecked
            {
                int hash = GetType().GetHashCode();
                hash = (hash * 397) ^ _startPoint.GetHashCode();
                hash = (hash * 397) ^ _slope.GetHashCode();
                return hash;
            }
        }
        #endregion
    }
}
