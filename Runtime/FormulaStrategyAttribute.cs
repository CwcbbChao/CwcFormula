using System;

namespace Cwcbb.Tools.CwcFormula
{
    /// <summary>
    /// 用于标记数值公式派生类（FormulaBase）的简短名称、菜单全称与优先级排序（Order 越小越靠前）。
    /// 遵循 CwcCombatEntity 的 Attribute 注解规范，支持在声明类时直接配置优先级与展示名称。
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
    public sealed class FormulaStrategyAttribute : Attribute
    {
        public string ShortName { get; }
        public string DisplayName { get; }
        public int Order { get; }

        public FormulaStrategyAttribute(string shortName, string displayName = null, int order = 100)
        {
            ShortName = shortName;
            DisplayName = displayName ?? shortName;
            Order = order;
        }
    }
}
