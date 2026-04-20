using Microsoft.Data.Sqlite;
using System;
using System.Collections.Generic;
using ProGlassAutomation.Models;

namespace ProGlassAutomation.Data.Database
{
    public static class DbHelper
    {
        private static string connStr = "Data Source=glass.db";

        // ================= VERSION CONTROL =================
        private static int LatestVersion = 3;

        // ================= INIT =================
        public static void Init()
        {
            using var conn = new SqliteConnection(connStr);
            conn.Open();

            CreateVersionTable(conn);

            int current = GetVersion(conn);

            for (int v = current + 1; v <= LatestVersion; v++)
            {
                ApplyMigration(conn, v);
                SetVersion(conn, v);
            }
        }

        // ================= VERSION TABLE =================
        private static void CreateVersionTable(SqliteConnection conn)
        {
            var cmd = conn.CreateCommand();
            cmd.CommandText =
            @"CREATE TABLE IF NOT EXISTS DbVersion (
                Id INTEGER PRIMARY KEY,
                Version INTEGER
            );";
            cmd.ExecuteNonQuery();
        }

        private static int GetVersion(SqliteConnection conn)
        {
            var cmd = conn.CreateCommand();
            cmd.CommandText = "SELECT Version FROM DbVersion WHERE Id = 1";

            var result = cmd.ExecuteScalar();
            return result == null ? 0 : Convert.ToInt32(result);
        }

        private static void SetVersion(SqliteConnection conn, int version)
        {
            var cmd = conn.CreateCommand();
            cmd.CommandText =
            @"INSERT OR REPLACE INTO DbVersion (Id, Version)
              VALUES (1, $v);";

            cmd.Parameters.AddWithValue("$v", version);
            cmd.ExecuteNonQuery();
        }

        // ================= AUTO MIGRATION ENGINE =================
        private static void ApplyMigration(SqliteConnection conn, int version)
        {
            var cmd = conn.CreateCommand();

            switch (version)
            {
                // ---------------- BASE TABLES ----------------
                case 1:
                    cmd.CommandText = @"
                    CREATE TABLE IF NOT EXISTS SGURecords (
                        Id INTEGER PRIMARY KEY AUTOINCREMENT,
                        Thickness TEXT,
                        Color TEXT,
                        Result REAL,
                        CreatedAt TEXT
                    );

                    CREATE TABLE IF NOT EXISTS DGURecords (
                        Id INTEGER PRIMARY KEY AUTOINCREMENT,
                        Thickness1 TEXT,
                        Color1 TEXT,
                        Thickness2 TEXT,
                        Color2 TEXT,
                        Spacer TEXT,
                        Result REAL,
                        CreatedAt TEXT
                    );

                    CREATE TABLE IF NOT EXISTS LaminationRecords (
                        Id INTEGER PRIMARY KEY AUTOINCREMENT,
                        Thickness1 TEXT,
                        Color1 TEXT,
                        Thickness2 TEXT,
                        Color2 TEXT,
                        PVBType TEXT,
                        Result REAL,
                        CreatedAt TEXT
                    );
                    ";
                    cmd.ExecuteNonQuery();
                    break;

                // ---------------- LAMINATION UPGRADE ----------------
                case 2:
                    cmd.CommandText = "ALTER TABLE LaminationRecords ADD COLUMN Cutting REAL DEFAULT 0;";
                    cmd.ExecuteNonQuery();

                    cmd.CommandText = "ALTER TABLE LaminationRecords ADD COLUMN Tempering REAL DEFAULT 0;";
                    cmd.ExecuteNonQuery();

                    cmd.CommandText = "ALTER TABLE LaminationRecords ADD COLUMN IncludeCutting INTEGER DEFAULT 0;";
                    cmd.ExecuteNonQuery();

                    cmd.CommandText = "ALTER TABLE LaminationRecords ADD COLUMN IncludeTempering INTEGER DEFAULT 0;";
                    cmd.ExecuteNonQuery();
                    break;

                // ---------------- FUTURE SAFE EXTENSION ----------------
                case 3:
                    // reserved for future (profit tracking, GST, etc)
                    break;
            }
        }

        // =====================================================
        // SGU (UNCHANGED)
        // =====================================================
        public static void Save(string thickness, string color, double result)
        {
            using var conn = new SqliteConnection(connStr);
            conn.Open();

            var cmd = conn.CreateCommand();
            cmd.CommandText = @"
            INSERT INTO SGURecords (Thickness, Color, Result, CreatedAt)
            VALUES ($t, $c, $r, $d);
            ";

            cmd.Parameters.AddWithValue("$t", thickness ?? "");
            cmd.Parameters.AddWithValue("$c", color ?? "");
            cmd.Parameters.AddWithValue("$r", result);
            cmd.Parameters.AddWithValue("$d", DateTime.Now.ToString("yyyy-MM-dd HH:mm"));

            cmd.ExecuteNonQuery();
        }

        // =====================================================
        // DGU (UNCHANGED)
        // =====================================================
        public static void SaveDgu(string t1, string c1, string t2, string c2, string spacer, double result)
        {
            using var conn = new SqliteConnection(connStr);
            conn.Open();

            var cmd = conn.CreateCommand();
            cmd.CommandText = @"
            INSERT INTO DGURecords 
            (Thickness1, Color1, Thickness2, Color2, Spacer, Result, CreatedAt)
            VALUES ($t1,$c1,$t2,$c2,$s,$r,$d);
            ";

            cmd.Parameters.AddWithValue("$t1", t1 ?? "");
            cmd.Parameters.AddWithValue("$c1", c1 ?? "");
            cmd.Parameters.AddWithValue("$t2", t2 ?? "");
            cmd.Parameters.AddWithValue("$c2", c2 ?? "");
            cmd.Parameters.AddWithValue("$s", spacer ?? "");
            cmd.Parameters.AddWithValue("$r", result);
            cmd.Parameters.AddWithValue("$d", DateTime.Now.ToString("yyyy-MM-dd HH:mm"));

            cmd.ExecuteNonQuery();
        }

        // =====================================================
        // LAMINATION (NO CHANGE REQUIRED NOW)
        // =====================================================
        public static void SaveLamination(LaminationRecord r)
        {
            using var conn = new SqliteConnection(connStr);
            conn.Open();

            var cmd = conn.CreateCommand();
            cmd.CommandText = @"
            INSERT INTO LaminationRecords
            (Thickness1, Color1, Thickness2, Color2, PVBType, Result, CreatedAt,
             Cutting, Tempering, IncludeCutting, IncludeTempering)
            VALUES ($t1,$c1,$t2,$c2,$p,$r,$d,$cut,$temp,$ic,$it);
            ";

            cmd.Parameters.AddWithValue("$t1", r.Thickness1 ?? "");
            cmd.Parameters.AddWithValue("$c1", r.Color1 ?? "");
            cmd.Parameters.AddWithValue("$t2", r.Thickness2 ?? "");
            cmd.Parameters.AddWithValue("$c2", r.Color2 ?? "");
            cmd.Parameters.AddWithValue("$p", r.PVBType ?? "");
            cmd.Parameters.AddWithValue("$r", r.Result);
            cmd.Parameters.AddWithValue("$d", r.CreatedAt ?? DateTime.Now.ToString("yyyy-MM-dd HH:mm"));

            cmd.Parameters.AddWithValue("$cut", 0);
            cmd.Parameters.AddWithValue("$temp", 0);
            cmd.Parameters.AddWithValue("$ic", 0);
            cmd.Parameters.AddWithValue("$it", 0);

            cmd.ExecuteNonQuery();
        }

        public static List<LaminationRecord> GetAllLamination()
        {
            var list = new List<LaminationRecord>();

            using var conn = new SqliteConnection(connStr);
            conn.Open();

            var cmd = conn.CreateCommand();
            cmd.CommandText = "SELECT * FROM LaminationRecords ORDER BY Id DESC";

            using var r = cmd.ExecuteReader();

            while (r.Read())
            {
                list.Add(new LaminationRecord
                {
                    Id = r.GetInt32(0),
                    Thickness1 = r.GetString(1),
                    Color1 = r.GetString(2),
                    Thickness2 = r.GetString(3),
                    Color2 = r.GetString(4),
                    PVBType = r.GetString(5),
                    Result = r.GetDouble(6),
                    CreatedAt = r.GetString(7)
                });
            }

            return list;
        }
    }
}