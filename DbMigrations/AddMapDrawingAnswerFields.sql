-- AddMapDrawingAnswerFields.sql
-- 目的：为 AnswerRecord 表添加地图绘制题型答案存储字段
-- 使用说明：在两个数据库副本分别执行（项目根 exam_system.db 与 ExamSystem.WPF/exam_system.db）

BEGIN TRANSACTION;

-- 为 AnswerRecord 表添加地图绘制相关字段
ALTER TABLE AnswerRecords ADD COLUMN MapDrawingData TEXT NULL;
ALTER TABLE AnswerRecords ADD COLUMN MapCenter TEXT NULL;
ALTER TABLE AnswerRecords ADD COLUMN MapZoom INTEGER NULL;

-- 添加索引以提高查询性能
CREATE INDEX IF NOT EXISTS IX_AnswerRecords_MapDrawingData ON AnswerRecords(MapDrawingData) WHERE MapDrawingData IS NOT NULL;

-- 验证字段添加成功
SELECT name FROM pragma_table_info('AnswerRecords') WHERE name IN ('MapDrawingData', 'MapCenter', 'MapZoom');

COMMIT;

-- 字段说明：
-- MapDrawingData: JSON格式存储绘制的图形数据，包含所有覆盖物的坐标、类型、名称等信息
-- MapCenter: JSON格式存储地图中心点坐标 {"lng": 106.63, "lat": 26.65}
-- MapZoom: 地图缩放级别，整数类型