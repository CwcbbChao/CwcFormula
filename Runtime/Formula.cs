using System;
using UnityEngine;

namespace Cwcbb.Tools.CwcFormula
{
    /// <summary>
    /// 通用数值公式容器。
    /// 供 Modifier、Buff、技能等任意业务字段直接序列化引用的标准包装类。
    /// 核心设计：
    /// 1. 持有独立的多态 _evaluator 实例，运行时完全由其驱动，支持自由微调，绝不产生对 SO 的强运行时依赖；
    /// 2. 可选引用 _template 模板资产，用于在 Inspector 中一键复制预设或回写另存为预设。
    /// </summary>
    [Serializable]
    public class Formula : IFormula
    {
        #region Inspector Fields
        [Tooltip("Runtime formula evaluator.")]
        [SerializeReference] private FormulaBase _evaluator;
        #endregion

        #region Public Properties
        public FormulaBase Evaluator
        {
            get => _evaluator;
            set => _evaluator = value;
        }
        #endregion

        #region Public Methods
        public Formula()
        {
            _evaluator = FormulaRegistry.CreateDefault();
        }

        public Formula(FormulaBase evaluator)
        {
            _evaluator = evaluator != null ? evaluator.Clone() : FormulaRegistry.CreateDefault();
        }

        public Formula(FormulaTemplateSO template)
        {
            _evaluator = template != null ? template.CreateInstance() : FormulaRegistry.CreateDefault();
        }

        /// <summary>
        /// 执行公式计算。
        /// 纯粹的底层计算，零运行时 GC。
        /// </summary>
        /// <param name="inputParam">输入参数（如等级、时间、层数等）</param>
        /// <returns>计算结果数值</returns>
        public float Evaluate(float inputParam)
        {
            if (_evaluator == null)
            {
                return 0f;
            }

            return _evaluator.Evaluate(inputParam);
        }

        /// <summary>
        /// 从预设模板深拷贝配置并覆盖当前实例（仅拷贝参数，不绑定该模板资产）。
        /// </summary>
        /// <param name="template">目标模板预设资产</param>
        /// <returns>是否应用成功</returns>
        public bool CopyFromTemplate(FormulaTemplateSO template)
        {
            if (template == null)
            {
                Debug.LogWarning("[Formula] 应用预设失败：未指定有效的 FormulaTemplateSO 资产。");
                return false;
            }

            _evaluator = template.CreateInstance();
            return true;
        }

        /// <summary>
        /// 创建当前公式容器的深拷贝副本。
        /// </summary>
        public Formula Clone()
        {
            var cloned = new Formula();
            cloned._evaluator = _evaluator != null ? _evaluator.Clone() : FormulaRegistry.CreateDefault();
            return cloned;
        }

        IFormula IFormula.Clone()
        {
            return Clone();
        }
        #endregion
    }
}
