# 开发任务清单（WPF 地图绘制题，采用 Chromium WebView 控件）

本清单用于指导在现有 WPF 项目中新增“地图绘制题”功能的端到端开发。按照产品需求《产品需求-百度地图编辑器.md》（WPF本地实现版），并采纳技术建议：采用基于 Chromium 的 WebView 控件（Microsoft Edge WebView2）作为地图容器。

## 技术选型与原则
- 地图容器：Microsoft Edge WebView2（Chromium）。
- 地图实现：本地 HTML/JS（可使用百度地图 JS 能力），通过 JS <-> .NET 桥接与 WPF 交互。
- 数据存储：SQLite（本地），覆盖物与配置使用 JSON 序列化存储。
- 安全与权限：参考答案仅教师端可见；学生端仅显示辅助指引图层且不可编辑；完全本地实现，不依赖外部接口。

## 迭代与里程碑（建议）
- 迭代1：技术方案评审 → DB设计与迁移 → Domain/Repo/Services最小闭环 → WebView2地图页面基础 → 教师出题页面基础
- 迭代2：学生作答整合 → 阅卷页面 → 桥接协议完善 → 流程打通
- 迭代3：日志与性能 → UAT与验收 → 构建发布与种子数据 → 冒烟测试与回归

## 详细任务清单（含交付物）
1) 技术方案与系统设计评审（T1-TECH-DESIGN，优先级：高）
   - 明确技术路线（WPF + WebView2 + 本地HTML/JS）与数据结构（JSON）。
   - 输出《技术方案评审记录.md》，确定里程碑与验收标准。

2) 数据库设计与迁移（T2-DB-SCHEMA/T3-DB-MIGRATIONS，优先级：高）
   - Question 表新增：MapDrawingConfigJson、GuidanceOverlaysJson、ReferenceOverlaysJson、ReviewRubricJson、ShowBuildingLayersJson、TimeLimitSeconds。
   - AnswerRecord 表新增：OverlaysJson、DrawDurationSeconds、ClientInfoJson、Score、ReviewRemark、ReviewedByUserId、ReviewedAt、RubricScoresJson。
   - 交付：SQLite迁移脚本（含回滚）、执行说明，更新根目录与 WPF 项目内两处 db 副本。

3) Domain 层更新（T4/T5/T6，优先级：高）
   - QuestionType 枚举新增 MapDrawing；Question/AnswerRecord 实体扩展字段映射。
   - 设计 OverlayDTO/StyleDTO 及 JSON 序列化工具（双向）。
   - 交付：ExamSystem.Domain 更新 PR 与单元测试。

4) Infrastructure 仓储（T7，优先级：高）
   - Question/AnswerRecord 的 JSON 字段读写，事务与错误处理。
   - 交付：Repositories 代码与集成测试。

5) Services 扩展（T8，优先级：高）
   - 创建 MapDrawing 题、学生端获取题目（隐藏参考图层）、提交答案、阅卷保存评分。
   - 交付：ExamService/QuestionService 更新与测试。

6) 权限与安全（T9，优先级：高）
   - 教师端可见参考答案；学生端仅辅助指引且锁定；基本操作日志记录。
   - 交付：权限校验逻辑与日志写入。

7) 教师出题页面（T10，优先级：高）
   - 新增 Views/MapDrawingAuthoring.xaml + ViewModel。
   - 配置题目信息（城市/底图/路网/中心/缩放）、allowedTools/requiredOverlays，管理参考答案与辅助指引图层。
   - 交付：XAML+VM，基本交互打通（与 WebView2）。

8) 学生作答页面（T11，优先级：高）
   - 在 ExamView 中按 QuestionType 切换到 MapDrawingAnswering.xaml。
   - 工具栏、约束提示、已绘制列表（删除重画）、提交答案；倒计时（可选）。
   - 交付：XAML+VM，提交流程打通。

9) 教师阅卷页面（T12，优先级：高）
   - Views/MapDrawingReview.xaml；参考 vs 学生对比（叠加/分屏/透明度/图层开关），评分录入与备注。
   - 交付：XAML+VM，评分保存与结果回显。

10) WebView2 本地地图页面（T13，优先级：高）
    - 目录：ExamSystem.WPF/Assets/Map/index.html + scripts/。
    - 绘图工具（标记/折线/多边形/矩形/圆）、图标选择、水带纹理与头标记、辅助指引图层显示；模式：authoring/answering/review。
    - 交付：HTML/JS 页面与本地资源加载。

11) JS <-> .NET 桥接实现（T14，优先级：高）
    - CoreWebView2 初始化；postMessage/HostObject；消息协议：
      - from .NET：loadConfig、loadGuidance、loadReference(教师端)、setMode(authoring|answering|review)
      - from JS：overlaysChanged、submitAnswer、error、ready
    - 交付：桥接与消息处理代码，协议文档。

12) 本地资源准备（T15，优先级：中）
    - 城市坐标JSON、图标库与分类JSON、纹理图片 line.png/line_top.png，打包到资源并复制到输出。
    - 交付：Assets/Map 与复制规则（构建时）。

13) 覆盖物序列化一致性测试（T16，优先级：高）
    - JS 与 C# overlay 结构一致性（字段名/坐标顺序/样式）。
    - 交付：双向解析与回写测试报告。

14) ExamView 流程集成（T17，优先级：高）
    - 试卷加载路由视图；提交答案写入 AnswerRecord；阅卷读取参考与答案。
    - 交付：端到端流程打通。

15) 日志与错误处理（T18，优先级：中）
    - 地图未初始化保护、资源加载失败回退、读写异常提示与草稿保存，本地日志写入 Logs/。

16) 性能优化（T19，优先级：中）
    - 大量覆盖物重绘控制、折线抽稀、WebView2 消息批量传输与节流。

17) 单元与集成测试（T20，优先级：高）
    - Domain 序列化、Repository 读写、Services 流程、UI 基本交互、桥接协议调用。

18) 验收用例实现（T21，优先级：高）
    - 样例：参考折线 + 两指引点 → 学生绘制折线 → 阅卷对比与录分；形成测试脚本。

19) 文档更新（T22，优先级：中）
    - README、技术实施任务清单、用户指南（教师/学生/阅卷）、资源目录与数据格式。

20) 样例题与数据种子（T23，优先级：中）
    - 创建 MapDrawing 样例题，保存到题库并关联试卷。

21) 构建与发布（T24，优先级：高）
    - 复制 Assets/Map 到输出；保证运行时资源路径；端到端演示通过。

22) 冒烟测试与回归（T25，优先级：高）
    - 重建并运行 WPF 应用，验证地图绘制题全流程；确保无新异常。

## 数据库设计概要（复核）
- Question：MapDrawingConfigJson(TEXT)、GuidanceOverlaysJson(TEXT)、ReferenceOverlaysJson(TEXT)、ReviewRubricJson(TEXT)、ShowBuildingLayersJson(TEXT)、TimeLimitSeconds(INTEGER)
- AnswerRecord：OverlaysJson(TEXT)、DrawDurationSeconds(INTEGER)、ClientInfoJson(TEXT)、Score(REAL)、ReviewRemark(TEXT)、ReviewedByUserId(INTEGER)、ReviewedAt(DATETIME)、RubricScoresJson(TEXT)

## 资源与目录规范
- WebView2 页面：ExamSystem.WPF/Assets/Map/index.html 与 scripts/
- 本地 JSON：城市坐标、图标库与分类、示例题与示例覆盖物
- 纹理与图标：line.png、line_top.png、icons/*.png
- 构建复制：在项目构建后将 Assets/Map 目录复制到输出（bin/...）

## 验收标准（摘自需求）
- 教师端：能创建地图绘制题，保存参考答案与辅助指引图层；设置作答约束与评分量表；参考答案学生端不可见；辅助指引学生端可见且锁定。
- 学生端：按要求绘制指定数量与类型；满足约束后可提交；提交仅保存，不即时评分。
- 阅卷：对比参考与答案图层（叠加/分屏），录入分数与备注，提交评分并在学生端回显。

## 备注与注意事项
- 采用 Microsoft Edge WebView2（Chromium）是当前 Windows WPF 的主流方案；需安装 Edge WebView2 Runtime。
- 根目录与 WPF 项目内有两个 exam_system.db 副本，迁移时务必保持结构一致。
- 大量覆盖物与纹理叠加情况下需关注性能与内存使用；桥接消息建议批量与节流。

—— 完 ——