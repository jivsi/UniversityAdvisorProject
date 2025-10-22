/*
  # University Advisor Database Schema

  1. New Tables
    - `universities`
      - `id` (uuid, primary key)
      - `name` (text, university name)
      - `country` (text, country location)
      - `city` (text, city location)
      - `description` (text, university description)
      - `website_url` (text, official website)
      - `logo_url` (text, university logo)
      - `tuition_fee_min` (numeric, minimum annual tuition)
      - `tuition_fee_max` (numeric, maximum annual tuition)
      - `living_cost_monthly` (numeric, estimated monthly living cost)
      - `acceptance_rate` (numeric, acceptance rate percentage)
      - `student_count` (integer, total student population)
      - `founded_year` (integer, year founded)
      - `created_at` (timestamptz)
      - `updated_at` (timestamptz)
    
    - `programs`
      - `id` (uuid, primary key)
      - `university_id` (uuid, foreign key to universities)
      - `name` (text, program/major name)
      - `degree_type` (text, Bachelor/Master/PhD)
      - `duration_years` (numeric, program duration)
      - `language` (text, teaching language)
      - `description` (text, program description)
      - `created_at` (timestamptz)
    
    - `user_profiles`
      - `id` (uuid, primary key, references auth.users)
      - `email` (text, user email)
      - `full_name` (text, user's full name)
      - `country` (text, user's country)
      - `interests` (text, study interests)
      - `created_at` (timestamptz)
      - `updated_at` (timestamptz)
    
    - `favorites`
      - `id` (uuid, primary key)
      - `user_id` (uuid, references auth.users)
      - `university_id` (uuid, references universities)
      - `created_at` (timestamptz)
    
    - `search_history`
      - `id` (uuid, primary key)
      - `user_id` (uuid, references auth.users)
      - `search_query` (text, search keywords)
      - `filters_applied` (jsonb, filter data)
      - `created_at` (timestamptz)

  2. Security
    - Enable RLS on all tables
    - Public read access for universities and programs
    - Authenticated users can manage their own profiles, favorites, and search history
*/

-- Create universities table
CREATE TABLE IF NOT EXISTS universities (
  id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
  name text NOT NULL,
  country text NOT NULL,
  city text NOT NULL,
  description text,
  website_url text,
  logo_url text,
  tuition_fee_min numeric DEFAULT 0,
  tuition_fee_max numeric DEFAULT 0,
  living_cost_monthly numeric DEFAULT 0,
  acceptance_rate numeric,
  student_count integer,
  founded_year integer,
  created_at timestamptz DEFAULT now(),
  updated_at timestamptz DEFAULT now()
);

-- Create programs table
CREATE TABLE IF NOT EXISTS programs (
  id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
  university_id uuid REFERENCES universities(id) ON DELETE CASCADE,
  name text NOT NULL,
  degree_type text NOT NULL,
  duration_years numeric,
  language text DEFAULT 'English',
  description text,
  created_at timestamptz DEFAULT now()
);

-- Create user_profiles table
CREATE TABLE IF NOT EXISTS user_profiles (
  id uuid PRIMARY KEY REFERENCES auth.users(id) ON DELETE CASCADE,
  email text UNIQUE NOT NULL,
  full_name text,
  country text,
  interests text,
  created_at timestamptz DEFAULT now(),
  updated_at timestamptz DEFAULT now()
);

-- Create favorites table
CREATE TABLE IF NOT EXISTS favorites (
  id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
  user_id uuid REFERENCES auth.users(id) ON DELETE CASCADE,
  university_id uuid REFERENCES universities(id) ON DELETE CASCADE,
  created_at timestamptz DEFAULT now(),
  UNIQUE(user_id, university_id)
);

-- Create search_history table
CREATE TABLE IF NOT EXISTS search_history (
  id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
  user_id uuid REFERENCES auth.users(id) ON DELETE CASCADE,
  search_query text NOT NULL,
  filters_applied jsonb,
  created_at timestamptz DEFAULT now()
);

-- Create indexes for better performance
CREATE INDEX IF NOT EXISTS idx_programs_university ON programs(university_id);
CREATE INDEX IF NOT EXISTS idx_programs_name ON programs(name);
CREATE INDEX IF NOT EXISTS idx_universities_country ON universities(country);
CREATE INDEX IF NOT EXISTS idx_universities_city ON universities(city);
CREATE INDEX IF NOT EXISTS idx_favorites_user ON favorites(user_id);
CREATE INDEX IF NOT EXISTS idx_search_history_user ON search_history(user_id);

-- Enable Row Level Security
ALTER TABLE universities ENABLE ROW LEVEL SECURITY;
ALTER TABLE programs ENABLE ROW LEVEL SECURITY;
ALTER TABLE user_profiles ENABLE ROW LEVEL SECURITY;
ALTER TABLE favorites ENABLE ROW LEVEL SECURITY;
ALTER TABLE search_history ENABLE ROW LEVEL SECURITY;

-- RLS Policies for universities (public read)
CREATE POLICY "Anyone can view universities"
  ON universities FOR SELECT
  TO public
  USING (true);

-- RLS Policies for programs (public read)
CREATE POLICY "Anyone can view programs"
  ON programs FOR SELECT
  TO public
  USING (true);

-- RLS Policies for user_profiles
CREATE POLICY "Users can view own profile"
  ON user_profiles FOR SELECT
  TO authenticated
  USING (auth.uid() = id);

CREATE POLICY "Users can insert own profile"
  ON user_profiles FOR INSERT
  TO authenticated
  WITH CHECK (auth.uid() = id);

CREATE POLICY "Users can update own profile"
  ON user_profiles FOR UPDATE
  TO authenticated
  USING (auth.uid() = id)
  WITH CHECK (auth.uid() = id);

-- RLS Policies for favorites
CREATE POLICY "Users can view own favorites"
  ON favorites FOR SELECT
  TO authenticated
  USING (auth.uid() = user_id);

CREATE POLICY "Users can add favorites"
  ON favorites FOR INSERT
  TO authenticated
  WITH CHECK (auth.uid() = user_id);

CREATE POLICY "Users can delete own favorites"
  ON favorites FOR DELETE
  TO authenticated
  USING (auth.uid() = user_id);

-- RLS Policies for search_history
CREATE POLICY "Users can view own search history"
  ON search_history FOR SELECT
  TO authenticated
  USING (auth.uid() = user_id);

CREATE POLICY "Users can add search history"
  ON search_history FOR INSERT
  TO authenticated
  WITH CHECK (auth.uid() = user_id);

-- Insert sample data
INSERT INTO universities (name, country, city, description, website_url, tuition_fee_min, tuition_fee_max, living_cost_monthly, acceptance_rate, student_count, founded_year) VALUES
('University of Toronto', 'Canada', 'Toronto', 'Top-ranked Canadian research university with diverse programs and international student community.', 'https://www.utoronto.ca', 45000, 55000, 1500, 43, 90000, 1827),
('Harvard University', 'United States', 'Cambridge', 'Prestigious Ivy League institution known for excellence across all disciplines.', 'https://www.harvard.edu', 51000, 57000, 2000, 5, 23000, 1636),
('Dalhousie University', 'Canada', 'Halifax', 'Leading maritime university offering comprehensive programs in arts, sciences, and professional studies.', 'https://www.dal.ca', 20000, 30000, 1200, 60, 20000, 1818),
('University of Oxford', 'United Kingdom', 'Oxford', 'One of the world''s oldest and most prestigious universities with a collegiate system.', 'https://www.ox.ac.uk', 30000, 40000, 1800, 17, 24000, 1096),
('Technical University of Munich', 'Germany', 'Munich', 'Leading European technical university specializing in engineering and natural sciences.', 'https://www.tum.de', 0, 500, 900, 25, 45000, 1868),
('University of Melbourne', 'Australia', 'Melbourne', 'Australia''s leading university known for research excellence and graduate employability.', 'https://www.unimelb.edu.au', 35000, 45000, 1600, 70, 50000, 1853),
('ETH Zurich', 'Switzerland', 'Zurich', 'World-renowned institution for technology and natural sciences.', 'https://www.ethz.ch', 1500, 2000, 1700, 27, 22000, 1855),
('University of Tokyo', 'Japan', 'Tokyo', 'Japan''s most prestigious university with strong programs across all fields.', 'https://www.u-tokyo.ac.jp', 5000, 8000, 1100, 35, 28000, 1877)
ON CONFLICT DO NOTHING;

-- Insert sample programs
INSERT INTO programs (university_id, name, degree_type, duration_years, language, description)
SELECT 
  u.id,
  prog.name,
  prog.degree_type,
  prog.duration_years,
  prog.language,
  prog.description
FROM universities u
CROSS JOIN (
  VALUES 
    ('Computer Science', 'Bachelor', 4, 'English', 'Comprehensive study of computing, algorithms, and software development'),
    ('Medicine', 'Bachelor', 6, 'English', 'Professional medical education leading to medical practice'),
    ('Business Administration', 'Bachelor', 4, 'English', 'Management, finance, marketing, and entrepreneurship studies'),
    ('Engineering', 'Bachelor', 4, 'English', 'Applied sciences including mechanical, electrical, and civil engineering'),
    ('Psychology', 'Bachelor', 4, 'English', 'Study of human behavior and mental processes'),
    ('Law', 'Bachelor', 4, 'English', 'Legal studies and jurisprudence'),
    ('Data Science', 'Master', 2, 'English', 'Advanced analytics, machine learning, and big data'),
    ('International Relations', 'Bachelor', 4, 'English', 'Global politics, diplomacy, and international affairs'),
    ('Architecture', 'Bachelor', 5, 'English', 'Design and construction of buildings and spaces'),
    ('Economics', 'Bachelor', 4, 'English', 'Economic theory, policy, and financial systems')
) AS prog(name, degree_type, duration_years, language, description)
WHERE u.name IN ('University of Toronto', 'Harvard University', 'Dalhousie University', 'University of Oxford')
ON CONFLICT DO NOTHING;