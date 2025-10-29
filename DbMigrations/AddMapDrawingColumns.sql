-- AddMapDrawingColumns.sql
-- 目的：为地图绘制题型扩展 Questions 与 AnswerRecords 的列（SQLite）
-- 使用说明：在两个数据库副本分别执行（项目根 exam_system.db 与 ExamSystem.WPF/exam_system.db）

BEGIN TRANSACTION;

-- Questions 表新增列（均允许为 NULL，除 TimeLimitSeconds 默认 0）
ALTER TABLE Questions ADD COLUMN MapDrawingConfigJson TEXT;
ALTER TABLE Questions ADD COLUMN GuidanceOverlaysJson TEXT;
ALTER TABLE Questions ADD COLUMN ReferenceOverlaysJson TEXT;
ALTER TABLE Questions ADD COLUMN ReviewRubricJson TEXT;
ALTER TABLE Questions ADD COLUMN TimeLimitSeconds INTEGER NOT NULL DEFAULT 0;
ALTER TABLE Questions ADD COLUMN ShowBuildingLayersJson TEXT;

-- AnswerRecords 表新增列（时长与客户端信息、评分量表明细）
ALTER TABLE AnswerRecords ADD COLUMN DrawDurationSeconds INTEGER NOT NULL DEFAULT 0;
ALTER TABLE AnswerRecords ADD COLUMN ClientInfoJson TEXT;
ALTER TABLE AnswerRecords ADD COLUMN RubricScoresJson TEXT;

COMMIT;

-- 验证建议：
-- PRAGMA table_info(Questions);
-- PRAGMA table_info(AnswerRecords);