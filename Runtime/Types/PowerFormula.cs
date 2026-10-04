using System;
using UnityEngine;

namespace Cwcbb.Tools.CwcFormula
{
    /// <summary>
    /// 幂函数公式（自变量在底数，指数为常数）。
    /// 典型应用：怪物生命前期平缓增长（b < 1 开方衰减）、抛物线伤害成长（b = 2）等。
    /// 当输入参数小于等于起始点X时，恒定保持起始点Y。
    /// 公式：y = StartPoint.Y + Coefficient * ((inputParam - StartPoint.X) ^ Exponent)
    /// </summary>
    [Serializable]
    [FormulaStrategy("Power", "Power", 25)]
    public class PowerFormula : FormulaBase
    {
        #region Inspector Fields
        [Tooltip("Base point (X: base input, Y: base value). Held constant for input <= X.")]
        [SerializeField] private Vector2 _startPoint = new Vector2(1f, 1f);

        [Tooltip("Scale coefficient.")]
        [SerializeField] private float _coefficient = 1f;

        [Tooltip("Power exponent.")]
        [SerializeField] private float _exponent = 2f;
        #endregion

        #region Public Properties
        public override string FormulaName => "Power";
        public override float StartX => _startPoint.x;
        public override bool HasStartAnchor => true;
        public Vector2 StartPoint { get => _startPoint; set => _startPoint = value; }
        public float Coefficient { get => _coefficient; set => _coefficient = value; }
        public float Exponent { get => _exponent; set => _exponent = value; }
        #endregion

        #region Public Methods
        public PowerFormula()
        {
            _startPoint = new Vector2(1f, 1f);
            _coefficient = 1f;
            _exponent = 2f;
        }

        public PowerFormula(Vector2 startPoint, float coefficient, float exponent)
        {
            _startPoint = startPoint;
            _coefficient = coefficient;
            _exponent = exponent;
        }

        public PowerFormula(float coefficient, float exponent)
        {
            _startPoint = new Vector2(1f, 1f);
            _coefficient = coefficient;
            _exponent = exponent;
        }

        public override float Evaluate(float inputParam)
        {
            if (inputParam <= _startPoint.x)
            {
                return _startPoint.y;
            }

            float deltaParam = inputParam - _startPoint.x;
            return _startPoint.y + _coefficient * Mathf.Pow(deltaParam, _exponent);
        }

        public override FormulaBase Clone()
        {
            return new PowerFormula(_startPoint, _coefficient, _exponent);
        }

        public override int GetParameterFingerprint()
        {
            unchecked
            {
                int hash = GetType().GetHashCode();
                hash = (hash * 397) ^ _startPoint.GetHashCode();
                hash = (hash * 397) ^ _coefficient.GetHashCode();
                hash = (hash * 397) ^ _exponent.GetHashCode();
                return hash;
            }
        }
        #endregion
    }
}
