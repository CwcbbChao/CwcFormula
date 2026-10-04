# CwcFormula - Unity 数值成长公式与可视化曲线编辑系统

[![Unity](https://img.shields.io/badge/Unity-2021.3+-blue.svg)](https://unity.com/)
[![License](https://img.shields.io/badge/License-CWC_Tools_1.0-orange.svg)](#许可证条款)
[![GC-Alloc](https://img.shields.io/badge/GC--Alloc-0_Bytes-brightgreen.svg)](#性能保障)

通俗、好用、开箱即用的 Unity 游戏数值公式工具库。专为 RPG、动作、策略与放置类游戏设计，帮你彻底告别**“公式配完不知道曲线长啥样”**、**“改个参数全靠猜”**和**“调个数值要建几百个 ScriptableObject 小文件”**的痛苦体验。

---

## 游戏开发痛点：为什么做这个系统？

| 常见痛点 | 传统折磨做法 | CwcFormula 的解决方案 |
| :--- | :--- | :--- |
| **曲线形态盲猜** | 改了公式参数不知道实际走向，必须进 PlayMode 跑游戏看或者去 Excel 画图。 | 鼠标停在 Inspector 的 **`[Curve]`** 按钮上，直接弹窗显示平滑函数曲线，光标划过实时给出对应等级的数值读数，不用键盘输入。 |
| **公式难以复用与切换** | 线性公式写在代码里，策划想换成幂函数或分段成长就得改代码或重写公式。 | 统一的多态包装类 `Formula`。面板下拉菜单秒切：线性、指数、幂函数、分段、曲线随时换，业务逻辑只管调 `Evaluate(level)`。 |
| **SO 资产泛滥成灾** | 每个小技能想调下基础斜率，就得在 Project 窗口建一个单独的 `.asset` 文件，工程被小文件淹没。 | 字段直接内嵌序列化在组件里！随时独立改参数；想要复用时也能一键另存为/应用 `FormulaTemplateSO` 模板资产。 |
| **高频战斗掉帧与 GC** | 运行时用表达式树或字符串动态解析公式，战斗中频繁触发 GC 引起卡顿。 | 底层纯数学解析式直接计算，单次求值耗时仅数纳秒，**完全 0 内存分配 (0-GC)**，支持每秒几万次高频计算。 |

---

## 3 步极简上手

### 1. 在任意脚本中声明公式字段
```csharp
using UnityEngine;
using Cwcbb.Tools.CwcFormula;

public class HeroStats : MonoBehaviour
{
    // 基础攻击力成长：默认初始为线性成长
    [SerializeField] private Formula _attackFormula = new Formula(new LinearFormula(new Vector2(1, 10), 2.5f));

    // 护甲减伤率：使用带软上限的渐近线公式（起始10%，最大理论上限75%）
    [SerializeField] private Formula _armorReductionFormula = new Formula(new AsymptoticFormula(1, 0.1f, 0.75f, 100f));
}
```

### 2. 运行时求值（一行代码）
```csharp
public float GetAttackAtLevel(int level)
{
    // 纯数学纳秒级计算，零 GC
    return _attackFormula.Evaluate(level);
}

public float GetDamageReduction(float armor)
{
    return _armorReductionFormula.Evaluate(armor);
}
```

### 3. 在 Inspector 中可视化微调与预览
- 挂载该脚本后，面板会显示卡片式控件。
- 点击下拉列表，可直接在 **Linear**、**Exponential**、**Power**、**Segmented** 等模式间一键切换。
- **鼠标悬停在 `[Curve]` 按钮上**：立即弹出抗锯齿曲线图，移动鼠标即可看到“第 10 级多少数值”、“第 100 级会不会膨胀过高”。点击右上角 **Pin** 可常驻固定。

---

## 7 大内置经典成长模型速查表

不用手写复杂的数学解析式，插件开箱自带游戏数值设计最常用的 7 大模型：

| 公式类型 (Type) | 人话描述与游戏典型场景 | 核心参数说明 |
| :--- | :--- | :--- |
| **Linear (线性增长)** | 基础数值每级固定增加（如每级 +5 点攻击力、每强化一次 +10 防御）。 | `StartPoint`（起始输入与基础值）、`Slope`（每级增长量） |
| **Exponential (指数复利)** | 挂机放置游戏数值膨胀、每级按百分比利息式复利递增（如每级属性 $\times 1.08$）。 | `StartPoint`（起始点）、`GrowthFactor`（增长底数，如 1.05 代表每级涨 5%） |
| **Power (幂函数曲线)** | 适合怪物血量模型与收益递减模型。指数 $< 1$ 时前期成长快后期平缓；指数 $> 1$ 时前期平缓后期爆发。 | `StartPoint`（起始点）、`Coefficient`（系数）、`Exponent`（幂指数，如 0.6 或 1.5） |
| **Asymptotic (渐近线/软上限)** | 护甲免伤率、暴击抵抗、移速衰减等**永远不能超过 100%** 的属性，越堆收益越低，平滑逼近理论极值。 | `StartPoint`（初始基准）、`MaxValue`（绝对上限，如 0.75）、`HalfPointK`（达到半收益的衰减速度） |
| **Segmented (分段复合)** | 真正的大型 RPG 成长逻辑：1~10 级新手期平缓增长，11~30 级幂律成长，31+ 级转入后期指数平缓过度，一段接一段。 | 包含任意多段独立的子策略（支持线性、幂函数、指数、曲线混合拼接） |
| **Curve (动画曲线)** | 策划用 Unity 原生贝塞尔曲线任意手搓非规则曲线，支持位移和缩放。 | `Curve`（Unity AnimationCurve）、`Scale`（缩放比） |
| **Constant (固定常数)** | 保底固定数值，无论输入自变量多少，始终输出恒定数值。 | `Value`（固定值） |

---

## 核心编辑器特性

### 1. 悬停抗锯齿曲线预览 (Hover Curve Popup)
- **划过即视**：鼠标移动到 Inspector 的 `[Curve]` 按钮上方时立即呼出浮动窗口，移出平滑收回；
- **抗锯齿致密采样**：120 点致密采样渲染平滑曲线，自适应缩放原点空间感；
- **动态十字准星实时读数**：鼠标在图表上滑动，准星动态跟随，即时显示 `(X, Y)` 精度读数，无需敲键盘改参数反复试探；
- **防内存泄漏与编译保护**：内置三层生命周期防护，在 Unity 脚本重新编译或 Domain Reload 时自动清理，绝不残留幽灵窗口。

### 2. 兼顾“组件内嵌微调”与“全局模板预设”
- **零资产强绑定**：常规公式直接内嵌在怪物、武器或技能组件内部，绝不在 Assets 目录产生垃圾小文件；
- **模板克隆 (ScriptableObject)**：
  - 在 Project 视图右键创建 `FormulaTemplateSO`（如 `Warrior_HP_Curve`）；
  - 在卡片菜单一键拾取该模板克隆数据，或将面板上调好的绝妙曲线直接“另存为新模板”，两全其美。

### 3. 便捷快捷操作
- 支持右键或卡片菜单一键 **Copy / Paste** 整个公式配置；
- 支持一键 **Reset to Default** 恢复默认初始状态。

---

## 插件目录结构

```text
Assets/CwcPlugins/CwcFormula/
├── Runtime/
│   ├── Formula.cs                  # 通用公式包装容器（面向业务）
│   ├── FormulaBase.cs              # 纯 C# 多态公式抽象基类
│   ├── IFormula.cs                 # 基础计算接口
│   ├── FormulaRegistry.cs          # 策略注册中心与解耦工厂
│   ├── FormulaStrategyAttribute.cs # 策略特性标注
│   ├── FormulaTemplateSO.cs        # ScriptableObject 模板预设资产
│   ├── FormulaTestComponent.cs     # 开箱即用的测试组件
│   └── Types/                      # 7 大内置公式数学实现
│       ├── ConstantFormula.cs
│       ├── LinearFormula.cs
│       ├── ExponentialFormula.cs
│       ├── PowerFormula.cs
│       ├── AsymptoticFormula.cs
│       ├── SegmentedFormula.cs
│       └── CurveFormula.cs
├── Editor/
│   ├── FormulaDrawer.cs            # 卡片式 Inspector 属性抽屉
│   ├── FormulaPreviewPopup.cs      # 悬停抗锯齿动态曲线预览弹窗
│   └── SegmentDrawer.cs            # 分段公式可视化编辑抽屉
├── CHANGELOG.md
├── LICENSE
├── package.json
├── README.md
└── README_EN.md
```

---

## 性能保障

- **GC 内存分配**：所有公式的 `Evaluate(float)` 底层均为纯数学解析式或轻量结构运算，**每帧调用 0 堆内存分配 (0 B GC)**。
- **计算开销**：单次计算耗时均在纳秒级别，可极其放心地挂载于千万级高频子弹击中判定、连续 Buff 伤害结算等高负载业务中。

---

## 许可证条款

本项目采用 **CWC Tools Public License v1.0**：
- **免费商业使用**：允许在任意商业、非商业最终游戏作品（如 PC、手游、主机游戏）中免费集成与发布，无需支付任何版税或授权费用。
- **源码保护限制**：严禁将本源码、工具包或其派生修改版本以开发工具包、资产包形式直接打包转售、重新分发或公开发布至任何资产商店（包括 Unity Asset Store、GitHub、开源仓库等）。
