BEGIN TRANSACTION;

-- Questions table new columns
ALTER TABLE Questions ADD COLUMN MapDrawingConfigJson TEXT;
ALTER TABLE Questions ADD COLUMN GuidanceOverlaysJson TEXT;
ALTER TABLE Questions ADD COLUMN ReferenceOverlaysJson TEXT;
ALTER TABLE Questions ADD COLUMN ReviewRubricJson TEXT;
ALTER TABLE Questions ADD COLUMN TimeLimitSeconds INTEGER NOT NULL DEFAULT 0;
ALTER TABLE Questions ADD COLUMN ShowBuildingLayersJson TEXT;

-- AnswerRecords table new columns
ALTER TABLE AnswerRecords ADD COLUMN DrawDurationSeconds INTEGER NOT NULL DEFAULT 0;
ALTER TABLE AnswerRecords ADD COLUMN ClientInfoJson TEXT;
ALTER TABLE AnswerRecords ADD COLUMN RubricScoresJson TEXT;

COMMIT;
