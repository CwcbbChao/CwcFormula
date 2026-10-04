namespace Cwcbb.Tools.CwcFormula
{
    /// <summary>
    /// 数值公式通用计算契约接口。
    /// 纯粹的纯 C# 数值求值抽象，不产生任何运行时 GC 分配。
    /// </summary>
    public interface IFormula
    {
        /// <summary>
        /// 根据输入参数计算输出数值。
        /// </summary>
        /// <param name="inputParam">输入参数（如等级、时间、层数等）</param>
        /// <returns>计算结果数值</returns>
        float Evaluate(float inputParam);

        /// <summary>
        /// 创建当前公式的深拷贝独立副本。
        /// </summary>
        IFormula Clone();
    }
}
