#!/usr/bin/env python3
import sqlite3
import os
import sys

def apply_migration():
    print("=== Applying Database Migration ===")
    
    db_paths = ["exam_system.db", "ExamSystem.WPF/exam_system.db"]
    
    migration_sql = """
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
"""
    
    for db_path in db_paths:
        print(f"\n--- Processing database: {db_path} ---")
        
        if not os.path.exists(db_path):
            print(f"⚠️  Database file not found, skipping: {db_path}")
            continue
            
        try:
            conn = sqlite3.connect(db_path)
            cursor = conn.cursor()
            
            # Check if migration already applied
            cursor.execute("PRAGMA table_info(Questions);")
            columns = [row[1] for row in cursor.fetchall()]
            
            if "MapDrawingConfigJson" in columns:
                print("✅ Migration already applied, skipping this database")
                conn.close()
                continue
            
            # Apply migration
            cursor.executescript(migration_sql)
            conn.commit()
            conn.close()
            
            print("✅ Database migration executed successfully")
            
        except sqlite3.Error as e:
            print(f"❌ Migration failed: {e}")
            if "duplicate column name" in str(e):
                print("✅ Column already exists, migration may have been applied")
        except Exception as e:
            print(f"❌ Unexpected error: {e}")
    
    print("\n=== Migration Complete ===")

if __name__ == "__main__":
    apply_migration()