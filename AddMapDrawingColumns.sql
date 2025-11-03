-- 添加地图绘制相关字段到AnswerRecords表
-- 执行日期: 2024-11-01
-- 目的: 支持地图绘制题型的数据保存

-- 检查表是否存在
SELECT name FROM sqlite_master WHERE type='table' AND name='AnswerRecords';

-- 添加MapDrawingData字段 - 存储地图绘制的JSON数据
ALTER TABLE AnswerRecords ADD COLUMN MapDrawingData TEXT NULL;

-- 添加MapCenter字段 - 存储地图中心坐标
ALTER TABLE AnswerRecords ADD COLUMN MapCenter TEXT NULL;

-- 添加MapZoom字段 - 存储地图缩放级别
ALTER TABLE AnswerRecords ADD COLUMN MapZoom INTEGER NULL;

-- 验证字段是否添加成功
PRAGMA table_info(AnswerRecords);

-- 显示添加的字段
SELECT 
    name as 'Column Name',
    type as 'Data Type',
    [notnull] as 'Not Null',
    dflt_value as 'Default Value'
FROM pragma_table_info('AnswerRecords') 
WHERE name IN ('MapDrawingData', 'MapCenter', 'MapZoom');