using Microsoft.Data.Sqlite;
using System;
using System.Collections.Generic;
using System.IO;
using ProGlassAutomation.Models;

namespace ProGlassAutomation.Data.Database
{
    public static class DbHelper
    {
        private static string connStr = "Data Source=glass.db";

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

            EnsureLaminationColumns(conn);
        }

        // ================= VERSION =================
        private static void CreateVersionTable(SqliteConnection conn)
        {
            using var cmd = conn.CreateCommand();
            cmd.CommandText =
            @"CREATE TABLE IF NOT EXISTS DbVersion (
                Id INTEGER PRIMARY KEY,
                Version INTEGER
            );";
            cmd.ExecuteNonQuery();
        }

        private static int GetVersion(SqliteConnection conn)
        {
            using var cmd = conn.CreateCommand();
            cmd.CommandText = "SELECT Version FROM DbVersion WHERE Id = 1";

            var result = cmd.ExecuteScalar();
            return result == null ? 0 : Convert.ToInt32(result);
        }

        private static void SetVersion(SqliteConnection conn, int version)
        {
            using var cmd = conn.CreateCommand();
            cmd.CommandText =
            @"INSERT OR REPLACE INTO DbVersion (Id, Version)
              VALUES (1, $v);";

            cmd.Parameters.AddWithValue("$v", version);
            cmd.ExecuteNonQuery();
        }

        // ================= MIGRATION =================
        private static void ApplyMigration(SqliteConnection conn, int version)
        {
            switch (version)
            {
                case 1:
                    using (var cmd = conn.CreateCommand())
                    {
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
                    }
                    break;

                case 2:
                    ExecuteSafeAlter(conn, "ALTER TABLE LaminationRecords ADD COLUMN Cutting REAL DEFAULT 0");
                    ExecuteSafeAlter(conn, "ALTER TABLE LaminationRecords ADD COLUMN Tempering REAL DEFAULT 0");
                    ExecuteSafeAlter(conn, "ALTER TABLE LaminationRecords ADD COLUMN IncludeCutting INTEGER DEFAULT 0");
                    ExecuteSafeAlter(conn, "ALTER TABLE LaminationRecords ADD COLUMN IncludeTempering INTEGER DEFAULT 0");
                    break;

                case 3:
                    break;
            }
        }

        private static void ExecuteSafeAlter(SqliteConnection conn, string sql)
        {
            try
            {
                using var cmd = conn.CreateCommand();
                cmd.CommandText = sql;
                cmd.ExecuteNonQuery();
            }
            catch { }
        }

        private static void EnsureLaminationColumns(SqliteConnection conn)
        {
            ExecuteSafeAlter(conn, "ALTER TABLE LaminationRecords ADD COLUMN Cutting REAL DEFAULT 0");
            ExecuteSafeAlter(conn, "ALTER TABLE LaminationRecords ADD COLUMN Tempering REAL DEFAULT 0");
            ExecuteSafeAlter(conn, "ALTER TABLE LaminationRecords ADD COLUMN IncludeCutting INTEGER DEFAULT 0");
            ExecuteSafeAlter(conn, "ALTER TABLE LaminationRecords ADD COLUMN IncludeTempering INTEGER DEFAULT 0");
        }

        // =====================================================
        // AUTO BACKUP SYSTEM
        // =====================================================
        public static void AutoBackup()
        {
            try
            {
                string source = "glass.db";

                string folder = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.Desktop),
                    "GlassBackup"
                );

                if (!Directory.Exists(folder))
                    Directory.CreateDirectory(folder);

                string backupFile =
                    Path.Combine(folder, $"glass_backup_{DateTime.Now:yyyyMMdd_HHmmss}.db");

                File.Copy(source, backupFile, true);
            }
            catch { }
        }

        // =====================================================
        // SGU
        // =====================================================
        public static void Save(string thickness, string color, double result)
        {
            using var conn = new SqliteConnection(connStr);
            conn.Open();

            using var cmd = conn.CreateCommand();
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

        public static List<SguRecord> GetAllFormatted()
        {
            var list = new List<SguRecord>();

            using var conn = new SqliteConnection(connStr);
            conn.Open();

            using var cmd = conn.CreateCommand();
            cmd.CommandText = "SELECT * FROM SGURecords ORDER BY Id DESC";

            using var r = cmd.ExecuteReader();

            while (r.Read())
            {
                list.Add(new SguRecord
                {
                    Id = r.GetInt32(0),
                    Thickness = r.GetString(1),
                    Color = r.GetString(2),
                    Result = r.GetDouble(3),
                    CreatedAt = r.GetString(4)
                });
            }

            return list;
        }

        // =====================================================
        // DGU
        // =====================================================
        public static void SaveDgu(string t1, string c1, string t2, string c2, string spacer, double result)
        {
            using var conn = new SqliteConnection(connStr);
            conn.Open();

            using var cmd = conn.CreateCommand();
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

        public static List<DguRecord> GetAllDgu()
        {
            var list = new List<DguRecord>();

            using var conn = new SqliteConnection(connStr);
            conn.Open();

            using var cmd = conn.CreateCommand();
            cmd.CommandText = "SELECT * FROM DGURecords ORDER BY Id DESC";

            using var r = cmd.ExecuteReader();

            while (r.Read())
            {
                list.Add(new DguRecord
                {
                    Id = r.GetInt32(0),
                    Thickness1 = r.GetString(1),
                    Color1 = r.GetString(2),
                    Thickness2 = r.GetString(3),
                    Color2 = r.GetString(4),
                    Spacer = r.GetString(5),
                    Result = r.GetDouble(6),
                    CreatedAt = r.GetString(7)
                });
            }

            return list;
        }

        // =====================================================
        // LAMINATION
        // =====================================================
        public static void SaveLamination(LaminationRecord r)
        {
            using var conn = new SqliteConnection(connStr);
            conn.Open();

            using var cmd = conn.CreateCommand();
            cmd.CommandText = @"
            INSERT INTO LaminationRecords
            (Thickness1, Color1, Thickness2, Color2, PVBType, Result, CreatedAt,
             Cutting, Tempering, IncludeCutting, IncludeTempering)
            VALUES
            ($t1,$c1,$t2,$c2,$p,$r,$d,$cut,$temp,$ic,$it);
            ";

            cmd.Parameters.AddWithValue("$t1", r.Thickness1 ?? "");
            cmd.Parameters.AddWithValue("$c1", r.Color1 ?? "");
            cmd.Parameters.AddWithValue("$t2", r.Thickness2 ?? "");
            cmd.Parameters.AddWithValue("$c2", r.Color2 ?? "");
            cmd.Parameters.AddWithValue("$p", r.PVBType ?? "");
            cmd.Parameters.AddWithValue("$r", r.Result);
            cmd.Parameters.AddWithValue("$d", r.CreatedAt ?? DateTime.Now.ToString("yyyy-MM-dd HH:mm"));

            cmd.Parameters.AddWithValue("$cut", r.Cutting);
            cmd.Parameters.AddWithValue("$temp", r.Tempering);
            cmd.Parameters.AddWithValue("$ic", r.IncludeCutting ? 1 : 0);
            cmd.Parameters.AddWithValue("$it", r.IncludeTempering ? 1 : 0);

            cmd.ExecuteNonQuery();
        }

        public static List<LaminationRecord> GetAllLamination()
        {
            var list = new List<LaminationRecord>();

            using var conn = new SqliteConnection(connStr);
            conn.Open();

            using var cmd = conn.CreateCommand();
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

        // =====================================================
        // SEARCH SYSTEM
        // =====================================================
        public static List<SguRecord> SearchSGU(string k)
        {
            var list = new List<SguRecord>();

            using var conn = new SqliteConnection(connStr);
            conn.Open();

            using var cmd = conn.CreateCommand();
            cmd.CommandText = "SELECT * FROM SGURecords WHERE Thickness LIKE $k OR Color LIKE $k ORDER BY Id DESC";
            cmd.Parameters.AddWithValue("$k", "%" + k + "%");

            using var r = cmd.ExecuteReader();

            while (r.Read())
            {
                list.Add(new SguRecord
                {
                    Id = r.GetInt32(0),
                    Thickness = r.GetString(1),
                    Color = r.GetString(2),
                    Result = r.GetDouble(3),
                    CreatedAt = r.GetString(4)
                });
            }

            return list;
        }

        public static List<DguRecord> SearchDGU(string k)
        {
            var list = new List<DguRecord>();

            using var conn = new SqliteConnection(connStr);
            conn.Open();

            using var cmd = conn.CreateCommand();
            cmd.CommandText = @"
            SELECT * FROM DGURecords
            WHERE Thickness1 LIKE $k OR Thickness2 LIKE $k OR Color1 LIKE $k OR Color2 LIKE $k
            ORDER BY Id DESC";
            cmd.Parameters.AddWithValue("$k", "%" + k + "%");

            using var r = cmd.ExecuteReader();

            while (r.Read())
            {
                list.Add(new DguRecord
                {
                    Id = r.GetInt32(0),
                    Thickness1 = r.GetString(1),
                    Color1 = r.GetString(2),
                    Thickness2 = r.GetString(3),
                    Color2 = r.GetString(4),
                    Spacer = r.GetString(5),
                    Result = r.GetDouble(6),
                    CreatedAt = r.GetString(7)
                });
            }

            return list;
        }
    }
}