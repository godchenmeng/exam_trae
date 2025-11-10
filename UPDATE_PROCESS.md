# 服务器版本升级程序实施方案（v1.1）

作者：技术管理组  
日期：2025-11-10  
适用范围：服务端（Web/API/作业服务）与相关数据库的版本更新与回滚  

本方案采用语义化版本控制（Semantic Versioning）x.y.z 格式，结合 6A 工作流（Assessment/Architecture/Application/Acceptance/Arrangement/Aftercare）与敏捷开发 5S 个人规则，确保更新过程安全、可控、可回滚、可审计。

---

## 1. 概述

目标：设计并实现一个可复用的服务器版本升级程序，支持：
- 版本状态检查（本地 vs 服务器最新）
- 更新文件下载（HTTPS、断点续传、MD5 校验）
- 数据库增量更新（含回滚）
- 原子化执行（全部成功或全部回滚）
- 完整审计日志与告警监控

核心目录结构：
```
/updates/{version}/
  ├─ manifest.json               # 文件清单、校验值、脚本列表
  ├─ files/                      # 程序文件、配置文件、资源
  ├─ db/                         # 数据库升级脚本与回滚脚本
  │   ├─ 2025-11-10_01_add_table.sql
  │   └─ 2025-11-10_01_add_table.rollback.sql
  ├─ signatures/                 # 数字签名与证书
  │   ├─ manifest.json.sig
  │   └─ public.pem
  └─ scripts/                    # 自动化更新执行脚本
```

---

### 版本变更记录

- v1.1（2025-11-10）
  - 新增 Workspace 集成与落地说明，明确在本项目内创建升级模块的目录与文件组织方式
  - 增补一键执行命令示例（PowerShell），提供在本项目路径下的实际调用范例
  - 完善状态报告机制与日志落地位置示例，统一到项目 Logs 目录
  - 文档精修：修正文件名引用、清理冗余说明，保持与脚本示例一致

## 2. 版本状态检查模块

设计：
- 版本检测服务通过 REST API 提供当前服务器最新版本号、更新说明、兼容矩阵。
- 客户端对比本地版本与服务器最新版本，决定是否更新。

语义化版本规范（x.y.z）：
- x（Major）：不兼容变更（需手动审批）
- y（Minor）：向后兼容新增功能
- z（Patch）：向后兼容问题修复

版本比较示例（PowerShell，可直接执行）：
```powershell
# file: scripts/VersionCheck.ps1
param(
  [string]$ServerUrl = "https://updates.example.com",
  [string]$LocalVersion = "1.4.2"
)

Write-Host "[VersionCheck] server=$ServerUrl local=$LocalVersion"
$versionApi = "$ServerUrl/api/version"

try {
  $resp = Invoke-RestMethod -Method GET -Uri $versionApi -TimeoutSec 15
  # 服务器返回示例：{ "latest":"1.5.0", "minCompatibleClient":"1.4.0", "notes":"..." }
  $latest = [System.Version]$resp.latest
  $local = [System.Version]$LocalVersion

  if ($latest -gt $local) {
    Write-Host "发现新版本：$($resp.latest)（本地：$LocalVersion）" -ForegroundColor Green
    exit 10 # 约定：10 表示需要更新
  } else {
    Write-Host "已是最新版本（本地：$LocalVersion）"
    exit 0
  }
}
catch {
  Write-Host "版本检查失败：$($_.Exception.Message)" -ForegroundColor Red
  exit 2
}
```

API 返回示例（JSON）：
```json
{
  "latest": "1.5.0",
  "minCompatibleClient": "1.4.0",
  "notes": "修复安全漏洞，优化下载性能",
  "publishedAt": "2025-11-10T08:30:00Z"
}
```

---

## 3. 更新文件下载模块

要求：
- 仅通过 HTTPS（TLS 1.2+）下载
- 断点续传（Range 请求或 BITS）
- 完整性校验（MD5 或更优的 SHA256）

下载与校验（PowerShell，可执行）：
```powershell
# file: scripts/DownloadAndVerify.ps1
param(
  [string]$ServerUrl,
  [string]$Version,
  [string]$TargetDir = "./updates/$Version"
)

$manifestUrl = "$ServerUrl/updates/$Version/manifest.json"
$manifestPath = Join-Path $TargetDir "manifest.json"
New-Item -ItemType Directory -Force -Path $TargetDir | Out-Null

Invoke-WebRequest -Uri $manifestUrl -OutFile $manifestPath
$manifest = Get-Content $manifestPath | ConvertFrom-Json

# 验证签名（可选，推荐）：
$sigUrl = "$ServerUrl/updates/$Version/signatures/manifest.json.sig"
$pubUrl = "$ServerUrl/updates/$Version/signatures/public.pem"
$sigPath = Join-Path $TargetDir "manifest.json.sig"
$pubPath = Join-Path $TargetDir "public.pem"
Invoke-WebRequest -Uri $sigUrl -OutFile $sigPath
Invoke-WebRequest -Uri $pubUrl -OutFile $pubPath
Write-Host "(建议) 使用 openssl 验证签名：openssl dgst -sha256 -verify public.pem -signature manifest.json.sig manifest.json"

# 断点续传下载文件并校验哈希
foreach ($f in $manifest.files) {
  $dest = Join-Path $TargetDir "files/$($f.path)"
  New-Item -ItemType Directory -Force -Path (Split-Path $dest) | Out-Null
  $url = "$ServerUrl/updates/$Version/files/$($f.path)"

  # 简单下载，可替换为 BITS 或 Range 续传
  Invoke-WebRequest -Uri $url -OutFile $dest

  $hash = Get-FileHash -Path $dest -Algorithm MD5
  if ($hash.Hash -ne $f.md5.ToUpper()) {
    throw "校验失败：$($f.path) MD5=$($hash.Hash) 期望=$($f.md5)"
  }
}

Write-Host "下载与校验完成：$Version" -ForegroundColor Green
```

manifest.json 示例：
```json
{
  "version": "1.5.0",
  "files": [
    { "path": "api/Service.dll", "md5": "F1E2D3C4B5A6978899..." },
    { "path": "config/appsettings.json", "md5": "ABCD1234..." }
  ],
  "dbScripts": [
    { "apply": "2025-11-10_01_add_table.sql", "rollback": "2025-11-10_01_add_table.rollback.sql" }
  ]
}
```

断点续传（C# HttpClient Range 请求示例）：
```csharp
// 可嵌入更新器服务中，用 Range 头实现断点续传
using var client = new HttpClient();
var dest = Path.Combine(targetDir, "files", relativePath);
Directory.CreateDirectory(Path.GetDirectoryName(dest)!);
var existing = File.Exists(dest) ? new FileInfo(dest).Length : 0L;
var req = new HttpRequestMessage(HttpMethod.Get, fileUrl);
if (existing > 0) req.Headers.Range = new System.Net.Http.Headers.RangeHeaderValue(existing, null);
var resp = await client.SendAsync(req, HttpCompletionOption.ResponseHeadersRead);
resp.EnsureSuccessStatusCode();
using var src = await resp.Content.ReadAsStreamAsync();
using var dst = new FileStream(dest, FileMode.Append, FileAccess.Write, FileShare.None);
await src.CopyToAsync(dst);
```

---

## 3.1 Workspace 集成与目录落地（本项目）

为便于在当前项目中落地升级模块，推荐在项目根目录下新增以下结构（Windows 环境，PowerShell 执行）：

```
e:\Project\exam_trae\
  ├─ scripts\
  │   ├─ VersionCheck.ps1          # 版本检测
  │   ├─ DownloadAndVerify.ps1     # 下载与校验
  │   ├─ ApplyDbUpdates.ps1        # 数据库升级与回滚
  │   └─ AtomicUpdate.ps1          # 原子化升级 orchestrator
  ├─ updates\{version}\           # 每个版本一个子目录
  │   ├─ manifest.json
  │   ├─ files\
  │   ├─ db\
  │   └─ signatures\
  └─ Logs\updates\                # 升级器状态与审计日志（JSON/文本）
```

说明：
- scripts 目录内的脚本与本文档示例一致；如已存在同名脚本，请以本文最新版为准进行替换或对比合并。
- updates/{version} 按发布包解压后的结构组织，保持 manifest 与 db 脚本配对完整。
- Logs/updates 为本项目统一的升级日志落地点，便于与现有 Serilog 或其他日志工具统一检索。

## 4. 数据库更新模块

设计：
- SQL 脚本版本控制：每个脚本包含 apply 与 rollback 配对。
- 增量更新：仅执行未应用的更新脚本。
- 回滚：失败时按逆序执行 rollback 脚本。

版本跟踪表（示例）：
```sql
CREATE TABLE IF NOT EXISTS SchemaVersions (
  Id INTEGER PRIMARY KEY AUTOINCREMENT,
  ScriptName TEXT NOT NULL,
  AppliedAt DATETIME NOT NULL,
  Hash TEXT NOT NULL,
  Success INTEGER NOT NULL CHECK (Success IN (0,1))
);
```

示例升级脚本（apply）：
```sql
-- file: db/2025-11-10_01_add_table.sql
BEGIN TRANSACTION;
CREATE TABLE IF NOT EXISTS t_new_feature (
  id INTEGER PRIMARY KEY,
  name TEXT NOT NULL,
  created_at DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP
);
COMMIT;
```

示例回滚脚本（rollback）：
```sql
-- file: db/2025-11-10_01_add_table.rollback.sql
BEGIN TRANSACTION;
DROP TABLE IF EXISTS t_new_feature;
COMMIT;
```

执行器（PowerShell，SQLite/SQL Server 通用思想）：
```powershell
# file: scripts/ApplyDbUpdates.ps1
param(
  [string]$DbType = "sqlite",  # sqlite | sqlserver
  [string]$ConnString,
  [string]$UpdateDir
)

$manifest = Get-Content (Join-Path $UpdateDir "manifest.json") | ConvertFrom-Json
$dbDir = Join-Path $UpdateDir "db"

function Invoke-Sqlite([string]$sqlFile) {
  $sql = Get-Content $sqlFile -Raw
  # 可替换为 sqlite3.exe 或 System.Data.SQLite 调用
  Add-Type -AssemblyName System.Data.SQLite
  $conn = New-Object System.Data.SQLite.SQLiteConnection($ConnString)
  $conn.Open()
  $cmd = $conn.CreateCommand(); $cmd.CommandText = $sql; $cmd.ExecuteNonQuery();
  $conn.Close()
}

try {
  foreach ($pair in $manifest.dbScripts) {
    $apply = Join-Path $dbDir $pair.apply
    Invoke-Sqlite $apply
    Write-Host "Applied: $($pair.apply)" -ForegroundColor Green
  }
}
catch {
  Write-Host "DB 更新失败，开始回滚：$($_.Exception.Message)" -ForegroundColor Yellow
  foreach ($pair in ($manifest.dbScripts | Sort-Object -Descending)) {
    $rb = Join-Path $dbDir $pair.rollback
    try { Invoke-Sqlite $rb; Write-Host "Rolled back: $($pair.rollback)" }
    catch { Write-Host "回滚失败：$($_.Exception.Message)" -ForegroundColor Red }
  }
  throw
}
```

---

## 5. 执行流程控制

预检查：
- 磁盘空间：更新包体积×1.5 余量
- 权限：目标目录写权限、数据库连接权限
- 依赖项：.NET 运行时、证书、网络连通性

原子化更新（PowerShell，失败即回滚）：
```powershell
# file: scripts/AtomicUpdate.ps1
param(
  [string]$ServerUrl,
  [string]$Version,
  [string]$ServiceName = "ExampleService",
  [string]$DeployDir = "C:\\Services\\Example",
  [string]$BackupDir = "C:\\Backups\\Example"
)

try {
  # 1. 版本检查
  $rc = & ./scripts/VersionCheck.ps1 -ServerUrl $ServerUrl -LocalVersion "1.4.2"
  if ($LASTEXITCODE -ne 10) { Write-Host "无需更新"; return }

  # 2. 预检查
  $freeGB = (Get-PSDrive -Name C).Free/1GB
  if ($freeGB -lt 5) { throw "磁盘空间不足：$freeGB GB" }

  # 3. 下载更新
  & ./scripts/DownloadAndVerify.ps1 -ServerUrl $ServerUrl -Version $Version

  # 4. 停止服务并备份
  Stop-Service -Name $ServiceName -ErrorAction SilentlyContinue
  New-Item -ItemType Directory -Force -Path $BackupDir | Out-Null
  robocopy $DeployDir $BackupDir /MIR | Out-Null

  # 5. 部署文件
  robocopy "./updates/$Version/files" $DeployDir /MIR | Out-Null

  # 6. 数据库更新
  & ./scripts/ApplyDbUpdates.ps1 -DbType sqlite -ConnString "Data Source=C:\\data\\example.db" -UpdateDir "./updates/$Version"

  # 7. 启动服务并健康检查
  Start-Service -Name $ServiceName
  Start-Sleep -Seconds 5
  $health = Invoke-RestMethod -Method GET -Uri "https://api.example.com/health"
  if ($health.status -ne "ok") { throw "健康检查失败" }

  # 8. 成功报告
  Write-Host "更新成功：$Version" -ForegroundColor Green
}
catch {
  Write-Host "更新失败：$($_.Exception.Message) 开始回滚" -ForegroundColor Yellow
  # 回滚文件
  robocopy $BackupDir $DeployDir /MIR | Out-Null
  # 回滚数据库（ApplyDbUpdates.ps1 内部已处理逆序回滚）
  try { & ./scripts/ApplyDbUpdates.ps1 -DbType sqlite -ConnString "Data Source=C:\\data\\example.db" -UpdateDir "./updates/$Version" } catch {}
  # 尝试重启服务
  Start-Service -Name $ServiceName -ErrorAction SilentlyContinue
  throw
}
```

状态报告机制：
- 更新日志写入：logs/update_{yyyyMMdd}.log（JSON 格式）
- 错误报告：发送至监控/告警系统（WebHook 或邮件）

---

## 6. 安全与监控

安全：
- 所有更新包必须签名（manifest.json.sig + public.pem），更新器在执行前验证签名与校验值。
- 仅允许 HTTPS 下载，拒绝明文传输。
- 更新服务采用最小化权限的专用账户（仅对目标目录与数据库具备写权限）。

审计：
- 记录更新开始、结束、失败、回滚事件，含操作者、主机、版本号、耗时、摘要。
- 示例日志（JSON）：
```json
{ "ts":"2025-11-10T09:30:12Z", "host":"srv-01", "user":"svc_update", "event":"update_start", "version":"1.5.0" }
{ "ts":"2025-11-10T09:32:45Z", "host":"srv-01", "user":"svc_update", "event":"db_apply", "script":"2025-11-10_01_add_table.sql" }
{ "ts":"2025-11-10T09:35:02Z", "host":"srv-01", "user":"svc_update", "event":"update_success", "version":"1.5.0", "elapsedMs":290000 }
```

监控与告警：
- 监控更新失败、版本不一致、健康检查失败等事件；
- 告警渠道（企业微信/钉钉/邮件/WebHook），阈值与重试策略可配置。

---

## 7. 文档规范

在项目根目录维护 UPDATE_PROCESS.md（本文件）与配套文档：
- 更新程序架构图（文字/ASCII 流程图）
- 版本兼容性矩阵（含客户端/服务端）
- 回滚操作指南
- 故障排查手册
- API 接口文档

架构流程（ASCII 示意）：
```
[Client Updater] --HTTPS--> [/api/version]
     |  new? yes
     v
 [/updates/{version}/manifest.json]
     | verify signature
     v
   download files -> verify hashes -> stop service -> backup -> deploy -> db apply
     | health check ok?
     | yes -> success
     | no  -> rollback (files + db) -> report
```

---

## 8. 测试方案

- 单元测试：版本比较、校验计算、签名验证、DB 执行器结果判断。
- 集成测试：模拟完整更新流程（本地搭建一个简单 API 与 SQLite）。
- 压力测试：大文件（>1GB）断点续传稳定性（网络抖动、限速）。
- 异常测试：网络中断、权限不足、磁盘满、签名不合法、健康检查失败。

示例单元测试断言（C# 伪代码）：
```csharp
Assert.True(IsVersionNewer("1.5.0", "1.4.2"));
Assert.Equal(expectedMd5, ComputeMd5(filePath));
Assert.Throws<SignatureException>(() => VerifySignature(manifestPath, sigPath, pubPath));
```

---

## 9. 部署要求

- 最小化权限原则：更新服务账户仅具备必要权限；拒绝管理员权限执行（除审批场景）。
- 资源隔离：测试环境与生产环境严格分离，使用不同的配置与仓库。
- 备份策略：执行前自动备份关键数据（程序目录、数据库）并验证可恢复性。

---

## 10. 目录与配置示例

配置文件（update-config.json）：
```json
{
  "serverUrl": "https://updates.example.com",
  "serviceName": "ExampleService",
  "deployDir": "C:/Services/Example",
  "backupDir": "C:/Backups/Example",
  "db": {
    "type": "sqlite",
    "connString": "Data Source=C:/data/example.db"
  },
  "alerts": {
    "webhook": "https://monitor.example.com/webhook/update",
    "email": "ops@example.com"
  }
}
```

manifest.json（详细示例）：
```json
{
  "version": "1.5.0",
  "publishedAt": "2025-11-10T08:30:00Z",
  "files": [
    { "path": "api/Example.Api.dll", "md5": "3E23..." },
    { "path": "api/Example.Core.dll", "md5": "92AB..." },
    { "path": "config/appsettings.Production.json", "md5": "B77C..." }
  ],
  "dbScripts": [
    { "apply": "2025-11-10_01_add_table.sql", "rollback": "2025-11-10_01_add_table.rollback.sql" },
    { "apply": "2025-11-10_02_add_column.sql", "rollback": "2025-11-10_02_add_column.rollback.sql" }
  ],
  "compatibility": {
    "minClient": "1.4.0",
    "requiresRuntime": ".NET 8",
    "notes": "服务器端无破坏性变更，建议客户端升级至 1.5.x"
  }
}
```

---

## 11. 回滚与故障排查

回滚策略：
- 文件：以备份目录为基准，镜像恢复（robocopy /MIR）。
- 数据库：按执行顺序逆序运行对应 rollback 脚本。
- 服务：回滚后重启并执行健康检查。

故障排查清单：
- 网络：DNS、TLS 证书有效性、代理拦截。
- 权限：部署目录 ACL、服务账户权限、数据库写权限。
- 校验：MD5/SHA256 不一致，检查下载文件是否被篡改。
- 签名：public.pem 与 manifest.json.sig 不匹配；检查证书链与信任根。
- 健康检查：依赖服务不可用、配置文件误改、端口冲突。

---

## 12. API 接口文档（示例）

1) 获取最新版本信息
- 方法：GET /api/version
- 响应：
```json
{ "latest": "1.5.0", "minCompatibleClient": "1.4.0", "notes": "...", "publishedAt": "2025-11-10T08:30:00Z" }
```

2) 获取更新清单（指定版本）
- 方法：GET /updates/{version}/manifest.json
- 响应：参考第 10 节 manifest.json 示例

3) 下载指定文件
- 方法：GET /updates/{version}/files/{path}
- 响应：二进制文件（支持 Range 断点续传）。

4) 报告更新状态
- 方法：POST /api/update/report
- 请求：
```json
{ "host": "srv-01", "user": "svc_update", "version": "1.5.0", "event": "update_success", "elapsedMs": 290000 }
```

---

## 13. 6A 与 5S 的融合实践

- Assessment（评估）：在版本发布前完成风险评估与工作量估算；更新器内置预检查模块。
- Architecture（设计）：定义更新器架构、目录结构与接口规范；manifest 与脚本配对设计。
- Application（实现）：实现断点续传、校验、原子化执行；严格代码规范与注释。
- Acceptance（验收）：集成测试与用户验收（灰度发布）；性能与安全测试。
- Arrangement（部署）：环境准备、回滚策略、监控配置；最小权限与资源隔离。
- Aftercare（维护）：日志审计、问题修复、优化与文档更新；每次更新形成知识沉淀。

5S 个人规则落地：
- Sort：仅保留与当前版本更新相关的脚本与文件；限制在制品数量。
- Set in Order：更新目录与命名规范统一；manifest 管理清单。
- Shine：每日清扫技术债（脚本/说明同步更新）；Boy Scout Rule。
- Standardize：建立检查清单（磁盘/权限/健康检查）；自动化工具确保质量。
- Sustain：定期回顾更新数据与反馈，持续优化流程与工具。

---

## 14. 附录

- 推荐工具：
  - 证书与签名：OpenSSL、Get-AuthenticodeSignature（Windows）
  - 下载与续传：HttpClient（C#）、BITS（PowerShell）、Invoke-WebRequest
  - 日志：Serilog（JSON 输出）、Elastic Stack（集中化）
- 参考资料：
  - Semantic Versioning: https://semver.org/
  - OWASP 安全实践
  - Windows 服务管理与 ACL 配置指南

---

更新维护说明：
- 本文档随更新器代码变更同步维护；若接口或目录结构发生变化，需在发布前完成文档审核与批准（质量管理第 5 章）。

---

## 15. 项目内一键执行示例（Workspace）

以下示例展示如何在本项目中直接执行升级流程（请先将更新包放置到 updates/{version} 目录）：

```powershell
# 在项目根目录执行（终端当前目录：e:\Project\exam_trae）

$ServerUrl = "https://updates.example.com"
$Version   = "1.5.0"
$Service   = "ExampleService"           # 需要更新的目标服务名（如 Windows 服务）
$DeployDir = "C:\\Services\\Example"   # 目标部署目录
$BackupDir = "C:\\Backups\\Example"   # 备份目录

# 1) 版本检查（可选）
& ./scripts/VersionCheck.ps1 -ServerUrl $ServerUrl -LocalVersion "1.4.2"

# 2) 下载与校验（将文件下载到本项目 updates/$Version 内）
& ./scripts/DownloadAndVerify.ps1 -ServerUrl $ServerUrl -Version $Version

# 3) 原子化升级（包含：停止服务、备份、部署、DB更新、健康检查、失败回滚）
& ./scripts/AtomicUpdate.ps1 -ServerUrl $ServerUrl -Version $Version -ServiceName $Service -DeployDir $DeployDir -BackupDir $BackupDir

# 日志输出：e:\Project\exam_trae\Logs\updates\update_$(Get-Date -Format yyyyMMdd).log
```

注意事项：
- 若目标服务非 Windows 服务（例如仅为文件部署），可将 AtomicUpdate.ps1 中 Stop-Service/Start-Service 步骤替换为自定义进程管理或站点重启逻辑。
- 健康检查接口可按实际系统替换（示例使用 https://api.example.com/health）。
- 所有脚本默认使用 HTTPS 通道与签名/校验机制，强烈建议在生产环境严格启用。

---

## 16. 状态报告与日志落地（Workspace）

为统一项目内的升级审计与故障分析，建议将状态与审计日志集中输出至 Logs/updates 目录：

- 文件命名：`Logs/updates/update_{yyyyMMdd}.log`
- 内容格式：JSON 或文本，建议 JSON 以便后续集中检索与分析
- 示例（JSON）：
```json
{ "ts":"2025-11-10T09:30:12Z", "host":"srv-01", "user":"svc_update", "event":"update_start", "version":"1.5.0" }
{ "ts":"2025-11-10T09:32:45Z", "host":"srv-01", "user":"svc_update", "event":"db_apply", "script":"2025-11-10_01_add_table.sql" }
{ "ts":"2025-11-10T09:35:02Z", "host":"srv-01", "user":"svc_update", "event":"update_success", "version":"1.5.0", "elapsedMs":290000 }
```

与现有日志系统（如 Serilog）整合建议：
- 使用统一的日志目录与命名规范（Logs/updates）
- 将关键事件（开始/结束/失败/回滚）写入结构化日志，便于过滤与告警
- 在异常分支中包含完整错误信息与回滚结果摘要

---

## 17. 验收清单（Workspace 执行）

- 版本检测返回最新版本且高于本地版本
- 更新文件下载完成且 MD5/SHA256 校验通过、签名验证通过
- 目标服务停止成功，程序文件与数据库备份完成
- 部署文件成功替换，数据库脚本按顺序应用成功
- 健康检查通过（status=ok），系统恢复运行
- 日志记录完整，出现异常时自动回滚并记录详情
- 文档更新已同步至 UPDATE_PROCESS.md 并通过审核（质量管理第 5 章）