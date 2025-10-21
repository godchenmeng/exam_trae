-- 添加 IsPublished 列到 ExamPapers 表
ALTER TABLE ExamPapers ADD COLUMN IsPublished INTEGER NOT NULL DEFAULT 0;

-- 根据 Status 字段更新 IsPublished 值
UPDATE ExamPapers SET IsPublished = 1 WHERE Status = '已发布';
UPDATE ExamPapers SET IsPublished = 0 WHERE Status != '已发布' OR Status IS NULL;

-- 验证更新结果
SELECT PaperId, Name, Status, IsPublished FROM ExamPapers;