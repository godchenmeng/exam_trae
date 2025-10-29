-- RollbackMapDrawingColumns.sql
-- 目的：在需要移除地图绘制题型新增列时，重建 Questions 与 AnswerRecords（SQLite不支持DROP COLUMN）
-- 注意：该操作会重建表结构，请在执行前做好数据库备份！
-- 使用说明：在两个数据库副本分别执行（项目根 exam_system.db 与 ExamSystem.WPF/exam_system.db）

BEGIN TRANSACTION;

-- 1) 处理 Questions 表
-- 备份到临时表（不包含新增列）
CREATE TABLE IF NOT EXISTS Questions_backup AS
SELECT 
    QuestionId, Title, Content, Answer, Analysis, Score, Difficulty, Tags,
    BankId, QuestionType, CreatedAt, UpdatedAt, IsActive
FROM Questions;

-- 删除原表
DROP TABLE IF EXISTS Questions;

-- 按原结构重建 Questions 表（不含地图绘制题扩展列）
CREATE TABLE Questions (
    QuestionId INTEGER NOT NULL CONSTRAINT PK_Questions PRIMARY KEY AUTOINCREMENT,
    BankId INTEGER NOT NULL,
    QuestionType INTEGER NOT NULL,
    Title TEXT NOT NULL,
    Content TEXT NOT NULL,
    Answer TEXT NOT NULL,
    Analysis TEXT NULL,
    Score decimal(5,2) NOT NULL,
    Difficulty INTEGER NOT NULL,
    Tags TEXT NULL,
    CreatedAt TEXT NOT NULL,
    UpdatedAt TEXT NOT NULL,
    IsActive INTEGER NOT NULL,
    CONSTRAINT FK_Questions_QuestionBanks_BankId FOREIGN KEY (BankId) REFERENCES QuestionBanks(BankId) ON DELETE CASCADE
);

-- 恢复数据
INSERT INTO Questions (
    QuestionId, Title, Content, Answer, Analysis, Score, Difficulty, Tags,
    BankId, QuestionType, CreatedAt, UpdatedAt, IsActive)
SELECT 
    QuestionId, Title, Content, Answer, Analysis, Score, Difficulty, Tags,
    BankId, QuestionType, CreatedAt, UpdatedAt, IsActive
FROM Questions_backup;

-- 删除备份表
DROP TABLE IF EXISTS Questions_backup;


-- 2) 处理 AnswerRecords 表
-- 备份到临时表（不包含新增列）
CREATE TABLE IF NOT EXISTS AnswerRecords_backup AS
SELECT 
    AnswerId, RecordId, QuestionId, UserAnswer, Score,
    IsCorrect, IsGraded, Comment, AnswerTime, GradeTime, GraderId
FROM AnswerRecords;

-- 删除原表
DROP TABLE IF EXISTS AnswerRecords;

-- 按原结构重建 AnswerRecords 表（不含地图绘制题扩展列）
CREATE TABLE AnswerRecords (
    AnswerId INTEGER NOT NULL CONSTRAINT PK_AnswerRecords PRIMARY KEY AUTOINCREMENT,
    RecordId INTEGER NOT NULL,
    QuestionId INTEGER NOT NULL,
    UserAnswer TEXT NULL,
    Score decimal(5,2) NOT NULL,
    IsCorrect INTEGER NOT NULL,
    IsGraded INTEGER NOT NULL,
    Comment TEXT NULL,
    AnswerTime TEXT NULL,
    GradeTime TEXT NULL,
    GraderId INTEGER NULL,
    CONSTRAINT FK_AnswerRecords_ExamRecords_RecordId FOREIGN KEY (RecordId) REFERENCES ExamRecords(RecordId) ON DELETE CASCADE,
    CONSTRAINT FK_AnswerRecords_Questions_QuestionId FOREIGN KEY (QuestionId) REFERENCES Questions(QuestionId) ON DELETE RESTRICT,
    CONSTRAINT FK_AnswerRecords_Users_GraderId FOREIGN KEY (GraderId) REFERENCES Users(UserId) ON DELETE SET NULL
);

-- 恢复数据
INSERT INTO AnswerRecords (
    AnswerId, RecordId, QuestionId, UserAnswer, Score,
    IsCorrect, IsGraded, Comment, AnswerTime, GradeTime, GraderId)
SELECT 
    AnswerId, RecordId, QuestionId, UserAnswer, Score,
    IsCorrect, IsGraded, Comment, AnswerTime, GradeTime, GraderId
FROM AnswerRecords_backup;

-- 删除备份表
DROP TABLE IF EXISTS AnswerRecords_backup;

COMMIT;