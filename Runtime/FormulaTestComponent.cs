using UnityEngine;

namespace Cwcbb.Tools.CwcFormula
{
    /// <summary>
    /// 数值公式测试组件。
    /// 挂载到场景中任意 GameObject 上，用于在 Inspector 面板中测试公式配置、
    /// 模板复制（Apply Preset）、另存为预设（Save As Preset）、多态切换及采样求值。
    /// </summary>
    [AddComponentMenu("Cwcbb/Formula/Tests/Formula Test Component")]
    public class FormulaTestComponent : MonoBehaviour
    {
        #region Inspector Fields
        [Tooltip("Damage formula example.")]
        [SerializeField] private Formula _damageFormula = new Formula(new SegmentedFormula());

        [Tooltip("Cooldown formula example.")]
        [SerializeField] private Formula _cooldownFormula = new Formula(new LinearFormula());

        [Header("Runtime Test Settings")]
        [Tooltip("Test input parameter (e.g. level).")]
        [SerializeField] private float _testInputParam = 5f;
        #endregion

        #region Public Properties
        public Formula DamageFormula => _damageFormula;
        public Formula CooldownFormula => _cooldownFormula;
        public float TestInputParam { get => _testInputParam; set => _testInputParam = value; }
        #endregion

        #region Public Methods
        /// <summary>
        /// 外部或右键菜单触发公式求值测试。
        /// </summary>
        [ContextMenu("Execute Formula Test Evaluation")]
        public void RunTestEvaluation()
        {
            float damageResult = _damageFormula != null ? _damageFormula.Evaluate(_testInputParam) : 0f;
            float cooldownResult = _cooldownFormula != null ? _cooldownFormula.Evaluate(_testInputParam) : 0f;

            Debug.Log($"[FormulaTestComponent] 测试输入参数 X = {_testInputParam} | 伤害公式输出 = {damageResult} | 冷却公式输出 = {cooldownResult}");
        }
        #endregion
    }
}
