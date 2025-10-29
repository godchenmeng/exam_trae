# Database Migration Script
Write-Host "=== Applying Database Migration ===" -ForegroundColor Green

$dbPaths = @("exam_system.db", "ExamSystem.WPF/exam_system.db")

$migrationSql = @"
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
"@

# Create SQL file for manual execution
$sqlFile = "migration.sql"
$migrationSql | Out-File -FilePath $sqlFile -Encoding UTF8

foreach ($dbPath in $dbPaths) {
    Write-Host "`n--- Processing database: $dbPath ---" -ForegroundColor Cyan
    
    if (-not (Test-Path $dbPath)) {
        Write-Host "Database file not found, skipping: $dbPath" -ForegroundColor Yellow
        continue
    }

    Write-Host "Database file exists: $dbPath" -ForegroundColor Green
}

Write-Host "`nSQL migration file created: $sqlFile" -ForegroundColor Green
Write-Host "Please apply the migration manually using SQLite tools." -ForegroundColor Yellow
Write-Host "`n=== Migration Preparation Complete ===" -ForegroundColor Green