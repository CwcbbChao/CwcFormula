using System;
using UnityEngine;

namespace Cwcbb.Tools.CwcFormula
{
    /// <summary>
    /// 常数公式。无论输入何值，均返回固定的常量。
    /// </summary>
    [Serializable]
    [FormulaStrategy("Constant", "Constant", 50)]
    public class ConstantFormula : FormulaBase
    {
        #region Inspector Fields
        [Tooltip("Constant value.")]
        [SerializeField] private float _value = 1f;
        #endregion

        #region Public Properties
        public override string FormulaName => "Constant";
        public override float StartX => 0f;
        public override bool HasStartAnchor => false;
        public float Value => _value;
        #endregion

        #region Public Methods
        public ConstantFormula()
        {
            _value = 1f;
        }

        public ConstantFormula(float value)
        {
            _value = value;
        }

        public override float Evaluate(float inputParam)
        {
            return _value;
        }

        public override FormulaBase Clone()
        {
            return new ConstantFormula(_value);
        }

        public override int GetParameterFingerprint()
        {
            unchecked
            {
                int hash = GetType().GetHashCode();
                hash = (hash * 397) ^ _value.GetHashCode();
                return hash;
            }
        }
        #endregion
    }
}
