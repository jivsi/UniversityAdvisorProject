-- Run in Supabase: SQL Editor → New query → Run
-- Allows "Дистанционно" in UniversityPrograms.StudyForm

ALTER TABLE public."UniversityPrograms"
  DROP CONSTRAINT IF EXISTS universityprograms_studyform_chk;

ALTER TABLE public."UniversityPrograms"
  ADD CONSTRAINT universityprograms_studyform_chk
  CHECK (
    "StudyForm" IS NULL
    OR "StudyForm" IN (
      'Редовно',
      'Задочно',
      'Редовно, Задочно',
      'Дистанционно'
    )
  );
