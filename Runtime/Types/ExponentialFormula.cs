using System;
using UnityEngine;

namespace Cwcbb.Tools.CwcFormula
{
    /// <summary>
    /// 指数函数公式（自变量在指数，底数为固定常数增长因子）。
    /// 典型应用：每级递增固定百分比复利（如每级 +5% 攻击力）、放置类游戏数值几何级爆发成长、强化翻倍消耗等。
    /// 当输入参数小于等于起始点X时，恒定保持起始点Y。
    /// 公式：y = StartPoint.Y * (GrowthFactor ^ (inputParam - StartPoint.X))
    /// </summary>
    [Serializable]
    [FormulaStrategy("Exponential", "Exponential", 20)]
    public class ExponentialFormula : FormulaBase
    {
        #region Inspector Fields
        [Tooltip("Base point (X: base input, Y: base value). Held constant for input <= X.")]
        [SerializeField] private Vector2 _startPoint = new Vector2(1f, 1f);

        [Min(0f)]
        [Tooltip("Multiplication factor per unit input (e.g. 1.05 for +5%).")]
        [SerializeField] private float _growthFactor = 1.05f;
        #endregion

        #region Public Properties
        public override string FormulaName => "Exponential";
        public override float StartX => _startPoint.x;
        public override bool HasStartAnchor => true;
        public Vector2 StartPoint { get => _startPoint; set => _startPoint = value; }
        public float GrowthFactor { get => _growthFactor; set => _growthFactor = Mathf.Max(0f, value); }
        #endregion

        #region Public Methods
        public ExponentialFormula()
        {
            _startPoint = new Vector2(1f, 1f);
            _growthFactor = 1.05f;
        }

        public ExponentialFormula(Vector2 startPoint, float growthFactor)
        {
            _startPoint = startPoint;
            _growthFactor = Mathf.Max(0f, growthFactor);
        }

        /// <summary>
        /// 根据输入参数评估指数数值。
        /// </summary>
        public override float Evaluate(float inputParam)
        {
            if (inputParam <= _startPoint.x)
            {
                return _startPoint.y;
            }

            float deltaParam = inputParam - _startPoint.x;
            float safeFactor = Mathf.Max(0f, _growthFactor);
            return _startPoint.y * Mathf.Pow(safeFactor, deltaParam);
        }

        /// <summary>
        /// 深度克隆当前指数公式实例。
        /// </summary>
        public override FormulaBase Clone()
        {
            return new ExponentialFormula(_startPoint, _growthFactor);
        }

        public override int GetParameterFingerprint()
        {
            unchecked
            {
                int hash = GetType().GetHashCode();
                hash = (hash * 397) ^ _startPoint.GetHashCode();
                hash = (hash * 397) ^ _growthFactor.GetHashCode();
                return hash;
            }
        }
        #endregion
    }
}
