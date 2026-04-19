using Microsoft.Data.Sqlite;
using System;
using System.Collections.Generic;
using ProGlassAutomation.Models;

namespace ProGlassAutomation.Data.Database
{
    public static class DbHelper
    {
        private static string connStr = "Data Source=glass.db";

        public static void Init()
        {
            using var conn = new SqliteConnection(connStr);
            conn.Open();

            var cmd = conn.CreateCommand();

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
            ";

            cmd.ExecuteNonQuery();
        }

        // =========================
        // 🔵 SGU (UNCHANGED - DO NOT TOUCH)
        // =========================

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

        public static List<SguRecord> GetAllFormatted()
        {
            var list = new List<SguRecord>();

            using var conn = new SqliteConnection(connStr);
            conn.Open();

            var cmd = conn.CreateCommand();
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

        // =========================
        // 🟢 DGU (NEW - SAFE)
        // =========================

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

        public static List<DguRecord> GetAllDgu()
        {
            var list = new List<DguRecord>();

            using var conn = new SqliteConnection(connStr);
            conn.Open();

            var cmd = conn.CreateCommand();
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
    }
}