using Microsoft.Data.Sqlite;
using System;
using System.Collections.Generic;
using ProGlassAutomation.ViewModels.SGU;

namespace ProGlassAutomation.Data.Database
{
    public static class DbHelper
    {
        static string connStr = "Data Source=sgu.db";

        public static void Init()
        {
            using var conn = new SqliteConnection(connStr);
            conn.Open();

            var cmd = conn.CreateCommand();
            cmd.CommandText =
            @"
            CREATE TABLE IF NOT EXISTS SGURecords (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                Thickness TEXT,
                Color TEXT,
                Result REAL,
                CreatedAt TEXT
            );
            ";
            cmd.ExecuteNonQuery();
        }

        public static void Save(string thickness, string color, double result)
        {
            using var conn = new SqliteConnection(connStr);
            conn.Open();

            var cmd = conn.CreateCommand();
            cmd.CommandText =
            @"
            INSERT INTO SGURecords (Thickness, Color, Result, CreatedAt)
            VALUES ($t, $c, $r, $d);
            ";

            cmd.Parameters.AddWithValue("$t", thickness);
            cmd.Parameters.AddWithValue("$c", color);
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
                var record = new SguRecord
                {
                    Thickness = r.GetString(1),
                    Color = r.GetString(2),
                    Result = r.GetDouble(3),
                    CreatedAt = r.GetString(4)
                };

                record.DisplayText =
                    $"{record.Thickness} {record.Color} SGU Last Price - {record.Result:0.00}   |   {record.CreatedAt}";

                list.Add(record);
            }

            return list;
        }
    }
}