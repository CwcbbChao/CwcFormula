using System;
using UnityEngine;

namespace Cwcbb.Tools.CwcFormula
{
    /// <summary>
    /// 数值公式通用模板资产 (ScriptableObject)。
    /// 类似于 UIEffect 等插件的预设模板资产：
    /// 仅作为基准模板或常用配置库存在。实例字段通过一键复制克隆其内部数据，克隆后完全独立互不影响。
    /// </summary>
    [CreateAssetMenu(fileName = "NewFormulaTemplate", menuName = "Cwcbb/Formula/Formula Template")]
    public class FormulaTemplateSO : ScriptableObject
    {
        #region Inspector Fields
        [Tooltip("公式配置容器（使用标准 Formula 字段，支持策略切换、曲线预览、采样测试及模板克隆）。")]
        [SerializeField] private Formula _formula = new Formula();
        #endregion

        #region Public Properties
        /// <summary>
        /// 获取内部封装的通用公式容器。
        /// </summary>
        public Formula Formula => _formula;

        /// <summary>
        /// 快捷获取底层实际执行求值的公式算法实例。
        /// </summary>
        public FormulaBase Evaluator => _formula?.Evaluator;
        #endregion

        #region Public Methods
        /// <summary>
        /// 快捷评估模板公式数值。
        /// </summary>
        public float Evaluate(float inputParam)
        {
            if (_formula == null)
            {
                return 0f;
            }

            return _formula.Evaluate(inputParam);
        }

        /// <summary>
        /// 创建并返回一份深拷贝的独立公式实例。
        /// </summary>
        public FormulaBase CreateInstance()
        {
            if (_formula == null || _formula.Evaluator == null)
            {
                return FormulaRegistry.CreateDefault();
            }

            return _formula.Evaluator.Clone();
        }

        /// <summary>
        /// 设置/覆盖模板内部的公式数据（通常由编辑器将调好的实例另存为模板时调用）。
        /// </summary>
        public void SetFormula(FormulaBase formula)
        {
            if (_formula == null)
            {
                _formula = new Formula(formula);
            }
            else
            {
                _formula.Evaluator = formula != null ? formula.Clone() : FormulaRegistry.CreateDefault();
            }
        }

        /// <summary>
        /// 使用完整的公式容器覆盖当前模板数据。
        /// </summary>
        public void SetFormula(Formula formula)
        {
            _formula = formula != null ? formula.Clone() : new Formula();
        }
        #endregion
    }
}
