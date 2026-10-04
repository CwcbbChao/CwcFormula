using System;
using System.Reflection;
using UnityEngine;

namespace Cwcbb.Tools.CwcFormula
{
    /// <summary>
    /// 数值公式策略注册中心与默认实例工厂。
    /// 核心解耦枢纽：基础结构通过此类动态解析出优先级最高（Order 最小）的公式类型，
    /// 从而彻底摆脱基础结构对具体公式实现类的静态依赖。
    /// </summary>
    public static class FormulaRegistry
    {
        #region Private Static Fields
        private static Type _defaultFormulaType;
        private static readonly object _lock = new object();
        #endregion

        #region Public Methods
        /// <summary>
        /// 创建优先级最高的默认公式实例。
        /// </summary>
        /// <returns>新公式实例</returns>
        public static FormulaBase CreateDefault()
        {
            Type defaultType = GetDefaultFormulaType();
            if (defaultType != null)
            {
                try
                {
                    return (FormulaBase)Activator.CreateInstance(defaultType);
                }
                catch (Exception ex)
                {
                    Debug.LogError($"[FormulaRegistry] 实例化默认公式类型 '{defaultType.FullName}' 失败: {ex}");
                }
            }

            return null;
        }

        /// <summary>
        /// 获取当前工程中优先级最高（Order 最小）的公式派生类 Type。
        /// </summary>
        /// <returns>默认公式类型</returns>
        public static Type GetDefaultFormulaType()
        {
            if (_defaultFormulaType == null)
            {
                lock (_lock)
                {
                    if (_defaultFormulaType == null)
                    {
                        _defaultFormulaType = ResolveDefaultFormulaType();
                    }
                }
            }

            return _defaultFormulaType;
        }

        /// <summary>
        /// 手动重置缓存的默认公式类型（通常在脚本重新编译或动态加载后按需调用）。
        /// </summary>
        public static void ClearCache()
        {
            lock (_lock)
            {
                _defaultFormulaType = null;
            }
        }
        #endregion

        #region Private Methods
        private static Type ResolveDefaultFormulaType()
        {
            Type bestType = null;
            int lowestOrder = int.MaxValue;

            Assembly[] assemblies = AppDomain.CurrentDomain.GetAssemblies();
            for (int i = 0; i < assemblies.Length; i++)
            {
                Assembly asm = assemblies[i];
                string asmName = asm.FullName;

                // 过滤与业务无关联的庞大系统程序集，大幅提升反射效率
                if (asmName.StartsWith("System") || asmName.StartsWith("mscorlib") || asmName.StartsWith("Microsoft") || asmName.StartsWith("UnityEngine.") || asmName.StartsWith("UnityEditor."))
                {
                    continue;
                }

                Type[] types;
                try
                {
                    types = asm.GetTypes();
                }
                catch (ReflectionTypeLoadException ex)
                {
                    types = ex.Types;
                }
                catch
                {
                    continue;
                }

                if (types == null)
                {
                    continue;
                }

                for (int j = 0; j < types.Length; j++)
                {
                    Type t = types[j];
                    if (t == null || t.IsAbstract || t.IsInterface || !typeof(FormulaBase).IsAssignableFrom(t))
                    {
                        continue;
                    }

                    var attr = (FormulaStrategyAttribute)Attribute.GetCustomAttribute(t, typeof(FormulaStrategyAttribute));
                    int order = attr != null ? attr.Order : 1000;

                    if (bestType == null || order < lowestOrder)
                    {
                        lowestOrder = order;
                        bestType = t;
                    }
                }
            }

            return bestType;
        }
        #endregion
    }
}
