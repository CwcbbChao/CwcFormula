using System;
using UnityEngine;

namespace Cwcbb.Tools.CwcFormula
{
    /// <summary>
    /// 数值公式多态抽象基类。
    /// 基于纯 C# 类与 [SerializeReference] 序列化，支持无资产依赖的独立微调与多态求值。
    /// </summary>
    [Serializable]
    public abstract class FormulaBase : IFormula
    {
        #region Public Properties
        /// <summary>
        /// 公式名称（用于 Inspector 显示与识别）。
        /// </summary>
        public virtual string FormulaName => GetType().Name;

        /// <summary>
        /// 公式的基准起始自变量 X（如起始等级 1）。
        /// 用于坐标系起点对齐、参考基线与准星读数。默认返回 1f。
        /// </summary>
        public virtual float StartX => 1f;

        /// <summary>
        /// 是否拥有基准起始锚点概念。
        /// 若为 true，动态曲线预览会在 StartX 位置标出起始基准圆盘与参考线。
        /// </summary>
        public virtual bool HasStartAnchor => true;
        #endregion

        #region Public Methods
        /// <summary>
        /// 根据输入参数计算输出数值。
        /// </summary>
        /// <param name="inputParam">输入参数（如等级、时间、层数等）</param>
        /// <returns>计算结果数值</returns>
        public abstract float Evaluate(float inputParam);

        /// <summary>
        /// 创建当前公式的深拷贝独立副本。
        /// </summary>
        /// <returns>深拷贝后的新实例</returns>
        public abstract FormulaBase Clone();

        /// <summary>
        /// 计算当前公式实例的参数指纹哈希值。
        /// 当公式参数发生微调时该值应改变，用于驱动编辑器动态曲线图表热重绘。
        /// 基类提供基于类型哈希的默认实现，具体公式派生类应重写此方法进行零 GC 的字段哈希组合。
        /// </summary>
        /// <returns>参数指纹哈希</returns>
        public virtual int GetParameterFingerprint()
        {
            return GetType().GetHashCode();
        }

        /// <summary>
        /// 接口显式实现：返回深拷贝副本。
        /// </summary>
        IFormula IFormula.Clone()
        {
            return Clone();
        }
        #endregion
    }
}
