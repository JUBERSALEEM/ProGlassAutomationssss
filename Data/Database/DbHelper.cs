using Microsoft.Data.Sqlite;
using System;
using System.Collections.Generic;
using System.IO;
using ProGlassAutomation.Models;

namespace ProGlassAutomation.Data.Database
{
    public static class DbHelper
    {
        // ================= PATH CONFIG (PRODUCTION SAFE) =================
        private static readonly string DbPath =
            Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "ProGlassAutomation",
                "glass.db");

        private static readonly string ConnStr =
            $"Data Source={DbPath};Cache=Shared";

        private static readonly int LatestVersion = 5;

        // ================= CONNECTION =================
        private static SqliteConnection CreateConnection()
        {
            Directory.CreateDirectory(Path.GetDirectoryName(DbPath)!);
            return new SqliteConnection(ConnStr);
        }

        // ================= EXECUTION CORE =================
        private static void Execute(Action<SqliteConnection> action)
        {
            using var conn = CreateConnection();
            conn.Open();

            try
            {
                action(conn);
            }
            catch (Exception ex)
            {
                Log(ex);
                throw;
            }
        }

        private static T Execute<T>(Func<SqliteConnection, T> func)
        {
            using var conn = CreateConnection();
            conn.Open();

            try
            {
                return func(conn);
            }
            catch (Exception ex)
            {
                Log(ex);
                throw;
            }
        }

        // ================= LOGGING =================
        private static void Log(Exception ex)
        {
            try
            {
                string logPath = Path.Combine(Path.GetDirectoryName(DbPath)!, "db_log.txt");
                File.AppendAllText(logPath, $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] {ex.Message}\n{ex.StackTrace}\n\n");
            }
            catch { }
        }

        // ================= TEST CONNECTION =================
        public static bool TestConnection()
        {
            try
            {
                return Execute(conn =>
                {
                    using var cmd = conn.CreateCommand();
                    cmd.CommandText = "SELECT name FROM sqlite_master WHERE type='table'";
                    var reader = cmd.ExecuteReader();

                    var tables = new List<string>();
                    while (reader.Read())
                    {
                        tables.Add(reader.GetString(0));
                    }

                    System.Diagnostics.Debug.WriteLine($"[DbHelper] Tables found: {string.Join(", ", tables)}");
                    return true;
                });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[DbHelper] Connection Test Failed: {ex.Message}");
                Log(ex);
                return false;
            }
        }

        // ================= INIT =================
        public static void Init()
        {
            System.Diagnostics.Debug.WriteLine($"[DbHelper] Initializing database at: {DbPath}");

            // Ensure directory exists
            Directory.CreateDirectory(Path.GetDirectoryName(DbPath)!);

            Execute(conn =>
            {
                CreateVersionTable(conn);

                int current = GetVersion(conn);
                System.Diagnostics.Debug.WriteLine($"[DbHelper] Current version: {current}, Target version: {LatestVersion}");

                for (int v = current + 1; v <= LatestVersion; v++)
                {
                    System.Diagnostics.Debug.WriteLine($"[DbHelper] Applying migration v{v}...");
                    ApplyMigration(conn, v);
                    SetVersion(conn, v);
                    System.Diagnostics.Debug.WriteLine($"[DbHelper] Migration v{v} completed.");
                }

                EnsureLaminationColumns(conn);
                System.Diagnostics.Debug.WriteLine($"[DbHelper] Database initialization complete!");
            });
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

                case 4:
                    using (var cmd = conn.CreateCommand())
                    {
                        cmd.CommandText = @"
                        CREATE TABLE IF NOT EXISTS DailyWork (
                            Id INTEGER PRIMARY KEY AUTOINCREMENT,
                            Date TEXT,
                            UpdateDate TEXT,
                            Company TEXT,
                            PINumber TEXT,
                            CustomerReference TEXT,
                            TypeOfWork TEXT,
                            ProductionStatus TEXT,
                            DailyReportStatus TEXT,
                            Qty INTEGER,
                            SQM REAL,
                            Status TEXT,
                            Salesman TEXT,
                            Color TEXT,
                            Notes TEXT,
                            CreatedDate TEXT
                        );

                        CREATE TABLE IF NOT EXISTS Deliveries (
                            Id INTEGER PRIMARY KEY AUTOINCREMENT,
                            SourceId INTEGER,
                            Date TEXT,
                            Company TEXT,
                            PINumber TEXT,
                            CustomerReference TEXT,
                            TypeOfWork TEXT,
                            OrderQty INTEGER,
                            OrderSQM REAL,
                            Salesman TEXT,
                            Status TEXT,
                            Notes TEXT,
                            CreatedDate TEXT,
                            UpdatedDate TEXT
                        );

                        CREATE TABLE IF NOT EXISTS DeliveryItems (
                            Id INTEGER PRIMARY KEY AUTOINCREMENT,
                            OrderId INTEGER,
                            DeliveryDate TEXT,
                            DeliveredQty INTEGER,
                            DeliveredSQM REAL,
                            ReturnedQty INTEGER,
                            ReturnedSQM REAL,
                            Driver TEXT,
                            Vehicle TEXT,
                            Notes TEXT,
                            CreatedDate TEXT,
                            FOREIGN KEY (OrderId) REFERENCES Deliveries(Id)
                        );
                        ";
                        cmd.ExecuteNonQuery();
                    }
                    break;

                case 5:
                    using (var cmd = conn.CreateCommand())
                    {
                        cmd.CommandText = @"
                        CREATE TABLE IF NOT EXISTS SheetStore (
                            Id INTEGER PRIMARY KEY AUTOINCREMENT,
                            Category TEXT,
                            Thickness TEXT,
                            Color TEXT,
                            ColorHex TEXT,
                            Width INTEGER,
                            Height INTEGER,
                            SquareMeter REAL,
                            PurchasePrice REAL,
                            SellPrice REAL,
                            TotalStock INTEGER,
                            UsedSheets INTEGER,
                            BalanceSheets INTEGER,
                            IsActive INTEGER DEFAULT 1,
                            Supplier TEXT,
                            SupplierName TEXT,
                            Description TEXT,
                            CreatedDate TEXT,
                            LatestPurchaseDate TEXT
                        );

                        CREATE TABLE IF NOT EXISTS SheetPurchases (
                            Id INTEGER PRIMARY KEY AUTOINCREMENT,
                            SheetId INTEGER,
                            Quantity INTEGER,
                            UnitPrice REAL,
                            Supplier TEXT,
                            PurchasedOn TEXT,
                            Notes TEXT,
                            CreatedAt TEXT,
                            FOREIGN KEY (SheetId) REFERENCES SheetStore(Id)
                        );

                        CREATE TABLE IF NOT EXISTS SheetUsages (
                            Id INTEGER PRIMARY KEY AUTOINCREMENT,
                            SheetId INTEGER,
                            Quantity INTEGER,
                            Reason TEXT,
                            UsedOn TEXT,
                            CreatedAt TEXT,
                            FOREIGN KEY (SheetId) REFERENCES SheetStore(Id)
                        );
                        ";
                        cmd.ExecuteNonQuery();
                    }
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

        // ================= BACKUP =================
        public static void AutoBackup()
        {
            try
            {
                string folder = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.Desktop),
                    "GlassBackup");

                Directory.CreateDirectory(folder);

                string backupFile =
                    Path.Combine(folder, $"glass_backup_{DateTime.Now:yyyyMMdd_HHmmss}.db");

                File.Copy(DbPath, backupFile, true);
            }
            catch (Exception ex)
            {
                Log(ex);
            }
        }

        // ================= SGU =================
        public static void Save(string thickness, string color, double result)
        {
            Execute(conn =>
            {
                using var cmd = conn.CreateCommand();
                cmd.CommandText = @"
                INSERT INTO SGURecords (Thickness, Color, Result, CreatedAt)
                VALUES ($t, $c, $r, $d);";

                cmd.Parameters.AddWithValue("$t", thickness ?? "");
                cmd.Parameters.AddWithValue("$c", color ?? "");
                cmd.Parameters.AddWithValue("$r", result);
                cmd.Parameters.AddWithValue("$d", DateTime.Now.ToString("yyyy-MM-dd HH:mm"));

                cmd.ExecuteNonQuery();
            });
        }

        public static List<SguRecord> GetAllFormatted()
        {
            return Execute(conn =>
            {
                var list = new List<SguRecord>();

                using var cmd = conn.CreateCommand();
                cmd.CommandText = @"SELECT Id, Thickness, Color, Result, CreatedAt FROM SGURecords ORDER BY Id DESC";

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
            });
        }

        // ================= DGU =================
        public static void SaveDgu(string t1, string c1, string t2, string c2, string spacer, double result)
        {
            Execute(conn =>
            {
                using var cmd = conn.CreateCommand();
                cmd.CommandText = @"
                INSERT INTO DGURecords 
                (Thickness1, Color1, Thickness2, Color2, Spacer, Result, CreatedAt)
                VALUES ($t1,$c1,$t2,$c2,$s,$r,$d);";

                cmd.Parameters.AddWithValue("$t1", t1 ?? "");
                cmd.Parameters.AddWithValue("$c1", c1 ?? "");
                cmd.Parameters.AddWithValue("$t2", t2 ?? "");
                cmd.Parameters.AddWithValue("$c2", c2 ?? "");
                cmd.Parameters.AddWithValue("$s", spacer ?? "");
                cmd.Parameters.AddWithValue("$r", result);
                cmd.Parameters.AddWithValue("$d", DateTime.Now.ToString("yyyy-MM-dd HH:mm"));

                cmd.ExecuteNonQuery();
            });
        }

        public static List<DguRecord> GetAllDgu()
        {
            return Execute(conn =>
            {
                var list = new List<DguRecord>();

                using var cmd = conn.CreateCommand();
                cmd.CommandText = @"SELECT Id, Thickness1, Color1, Thickness2, Color2, Spacer, Result, CreatedAt FROM DGURecords ORDER BY Id DESC";

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
            });
        }

        // ================= LAMINATION =================
        public static void SaveLamination(LaminationRecord r)
        {
            Execute(conn =>
            {
                using var cmd = conn.CreateCommand();
                cmd.CommandText = @"
                INSERT INTO LaminationRecords
                (Thickness1, Color1, Thickness2, Color2, PVBType, Result, CreatedAt,
                 Cutting, Tempering, IncludeCutting, IncludeTempering)
                VALUES
                ($t1,$c1,$t2,$c2,$p,$r,$d,$cut,$temp,$ic,$it);";

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
            });
        }

        public static List<LaminationRecord> GetAllLamination()
        {
            return Execute(conn =>
            {
                var list = new List<LaminationRecord>();

                using var cmd = conn.CreateCommand();
                cmd.CommandText = @"SELECT * FROM LaminationRecords ORDER BY Id DESC";

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
                        CreatedAt = r.GetString(7),
                        Cutting = r.FieldCount > 8 && !r.IsDBNull(8) ? r.GetDouble(8) : 0,
                        Tempering = r.FieldCount > 9 && !r.IsDBNull(9) ? r.GetDouble(9) : 0,
                        IncludeCutting = r.FieldCount > 10 && !r.IsDBNull(10) && r.GetInt32(10) == 1,
                        IncludeTempering = r.FieldCount > 11 && !r.IsDBNull(11) && r.GetInt32(11) == 1
                    });
                }

                return list;
            });
        }

        // ================= SEARCH =================
        public static List<SguRecord> SearchSGU(string k)
        {
            return Execute(conn =>
            {
                var list = new List<SguRecord>();

                using var cmd = conn.CreateCommand();
                cmd.CommandText = @"SELECT * FROM SGURecords WHERE Thickness LIKE $k OR Color LIKE $k ORDER BY Id DESC";
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
            });
        }

        public static List<DguRecord> SearchDGU(string k)
        {
            return Execute(conn =>
            {
                var list = new List<DguRecord>();

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
            });
        }

        internal static void DeleteDgu(int id)
        {
            Execute(conn =>
            {
                using var cmd = conn.CreateCommand();
                cmd.CommandText = "DELETE FROM DGURecords WHERE Id = $id";
                cmd.Parameters.AddWithValue("$id", id);
                cmd.ExecuteNonQuery();
            });
        }

        internal static void DeleteSgu(int id)
        {
            Execute(conn =>
            {
                using var cmd = conn.CreateCommand();
                cmd.CommandText = "DELETE FROM SGURecords WHERE Id = $id";
                cmd.Parameters.AddWithValue("$id", id);
                cmd.ExecuteNonQuery();
            });
        }

        // ================= DAILY WORK =================
        public static void SaveDailyWork(DailyWork w)
        {
            Execute(conn =>
            {
                using var cmd = conn.CreateCommand();
                cmd.CommandText = @"
                INSERT INTO DailyWork (Date, UpdateDate, Company, PINumber, CustomerReference, TypeOfWork, ProductionStatus, DailyReportStatus, Qty, SQM, Status, Salesman, Color, Notes, CreatedDate)
                VALUES ($d, $ud, $c, $pi, $cr, $t, $ps, $drs, $q, $s, $st, $sm, $cl, $n, $cd);";

                cmd.Parameters.AddWithValue("$d", w.Date.ToString("yyyy-MM-dd"));
                cmd.Parameters.AddWithValue("$ud", w.UpdateDate.ToString("yyyy-MM-dd"));
                cmd.Parameters.AddWithValue("$c", w.Company ?? "");
                cmd.Parameters.AddWithValue("$pi", w.PINumber ?? "");
                cmd.Parameters.AddWithValue("$cr", w.CustomerReference ?? "");
                cmd.Parameters.AddWithValue("$t", w.TypeOfWork ?? "");
                cmd.Parameters.AddWithValue("$ps", w.ProductionStatus ?? "");
                cmd.Parameters.AddWithValue("$drs", w.DailyReportStatus ?? "");
                cmd.Parameters.AddWithValue("$q", w.Qty);
                cmd.Parameters.AddWithValue("$s", w.SQM);
                cmd.Parameters.AddWithValue("$st", w.Status ?? "");
                cmd.Parameters.AddWithValue("$sm", w.Salesman ?? "");
                cmd.Parameters.AddWithValue("$cl", w.Color ?? "");
                cmd.Parameters.AddWithValue("$n", w.Notes ?? "");
                cmd.Parameters.AddWithValue("$cd", w.CreatedDate.ToString("yyyy-MM-dd HH:mm"));

                cmd.ExecuteNonQuery();
            });
        }

        public static void UpdateDailyWork(DailyWork w)
        {
            Execute(conn =>
            {
                using var cmd = conn.CreateCommand();
                cmd.CommandText = @"
                UPDATE DailyWork SET 
                    Date = $d, UpdateDate = $ud, Company = $c, PINumber = $pi, 
                    CustomerReference = $cr, TypeOfWork = $t, ProductionStatus = $ps, 
                    DailyReportStatus = $drs, Qty = $q, SQM = $s, Status = $st, 
                    Salesman = $sm, Color = $cl, Notes = $n
                WHERE Id = $id;";

                cmd.Parameters.AddWithValue("$id", w.Id);
                cmd.Parameters.AddWithValue("$d", w.Date.ToString("yyyy-MM-dd"));
                cmd.Parameters.AddWithValue("$ud", w.UpdateDate.ToString("yyyy-MM-dd"));
                cmd.Parameters.AddWithValue("$c", w.Company ?? "");
                cmd.Parameters.AddWithValue("$pi", w.PINumber ?? "");
                cmd.Parameters.AddWithValue("$cr", w.CustomerReference ?? "");
                cmd.Parameters.AddWithValue("$t", w.TypeOfWork ?? "");
                cmd.Parameters.AddWithValue("$ps", w.ProductionStatus ?? "");
                cmd.Parameters.AddWithValue("$drs", w.DailyReportStatus ?? "");
                cmd.Parameters.AddWithValue("$q", w.Qty);
                cmd.Parameters.AddWithValue("$s", w.SQM);
                cmd.Parameters.AddWithValue("$st", w.Status ?? "");
                cmd.Parameters.AddWithValue("$sm", w.Salesman ?? "");
                cmd.Parameters.AddWithValue("$cl", w.Color ?? "");
                cmd.Parameters.AddWithValue("$n", w.Notes ?? "");

                cmd.ExecuteNonQuery();
            });
        }

        public static List<DailyWork> GetAllDailyWork()
        {
            return Execute(conn =>
            {
                var list = new List<DailyWork>();
                using var cmd = conn.CreateCommand();
                cmd.CommandText = "SELECT * FROM DailyWork ORDER BY Id DESC";
                using var r = cmd.ExecuteReader();
                while (r.Read())
                {
                    list.Add(new DailyWork
                    {
                        Id = r.GetInt32(0),
                        Date = DateTime.TryParse(r.GetString(1), out var d) ? d : DateTime.Today,
                        UpdateDate = DateTime.TryParse(r.GetString(2), out var ud) ? ud : DateTime.Today,
                        Company = r.IsDBNull(3) ? "" : r.GetString(3),
                        PINumber = r.IsDBNull(4) ? "" : r.GetString(4),
                        CustomerReference = r.IsDBNull(5) ? "" : r.GetString(5),
                        TypeOfWork = r.IsDBNull(6) ? "" : r.GetString(6),
                        ProductionStatus = r.IsDBNull(7) ? "" : r.GetString(7),
                        DailyReportStatus = r.IsDBNull(8) ? "" : r.GetString(8),
                        Qty = r.GetInt32(9),
                        SQM = r.GetDouble(10),
                        Status = r.IsDBNull(11) ? "" : r.GetString(11),
                        Salesman = r.IsDBNull(12) ? "" : r.GetString(12),
                        Color = r.IsDBNull(13) ? "" : r.GetString(13),
                        Notes = r.IsDBNull(14) ? "" : r.GetString(14),
                        CreatedDate = DateTime.TryParse(r.GetString(15), out var cd) ? cd : DateTime.Today
                    });
                }
                return list;
            });
        }

        public static void DeleteDailyWork(int id)
        {
            Execute(conn =>
            {
                using var cmd = conn.CreateCommand();
                cmd.CommandText = "DELETE FROM DailyWork WHERE Id = $id";
                cmd.Parameters.AddWithValue("$id", id);
                cmd.ExecuteNonQuery();
            });
        }

        // ================= DELIVERY =================
        public static void SaveDelivery(Delivery d)
        {
            Execute(conn =>
            {
                using var cmd = conn.CreateCommand();
                cmd.CommandText = @"
                INSERT INTO Deliveries (SourceId, Date, Company, PINumber, CustomerReference, TypeOfWork, OrderQty, OrderSQM, Salesman, Status, Notes, CreatedDate, UpdatedDate)
                VALUES ($sid, $d, $c, $pi, $cr, $t, $q, $s, $sm, $st, $n, $cd, $ud);";

                cmd.Parameters.AddWithValue("$sid", d.SourceId);
                cmd.Parameters.AddWithValue("$d", d.Date.ToString("yyyy-MM-dd"));
                cmd.Parameters.AddWithValue("$c", d.Company ?? "");
                cmd.Parameters.AddWithValue("$pi", d.PINumber ?? "");
                cmd.Parameters.AddWithValue("$cr", d.CustomerReference ?? "");
                cmd.Parameters.AddWithValue("$t", d.TypeOfWork ?? "");
                cmd.Parameters.AddWithValue("$q", d.OrderQty);
                cmd.Parameters.AddWithValue("$s", d.OrderSQM);
                cmd.Parameters.AddWithValue("$sm", d.Salesman ?? "");
                cmd.Parameters.AddWithValue("$st", d.Status ?? "");
                cmd.Parameters.AddWithValue("$n", d.Notes ?? "");
                cmd.Parameters.AddWithValue("$cd", d.CreatedDate.ToString("yyyy-MM-dd HH:mm"));
                cmd.Parameters.AddWithValue("$ud", d.UpdatedDate.ToString("yyyy-MM-dd HH:mm"));

                cmd.ExecuteNonQuery();
            });
        }

        public static void UpdateDelivery(Delivery d)
        {
            Execute(conn =>
            {
                using var cmd = conn.CreateCommand();
                cmd.CommandText = @"
                UPDATE Deliveries SET 
                    Date = $d, Company = $c, PINumber = $pi, CustomerReference = $cr, 
                    TypeOfWork = $t, OrderQty = $q, OrderSQM = $s, Salesman = $sm, 
                    Status = $st, Notes = $n, UpdatedDate = $ud
                WHERE Id = $id;";

                cmd.Parameters.AddWithValue("$id", d.Id);
                cmd.Parameters.AddWithValue("$d", d.Date.ToString("yyyy-MM-dd"));
                cmd.Parameters.AddWithValue("$c", d.Company ?? "");
                cmd.Parameters.AddWithValue("$pi", d.PINumber ?? "");
                cmd.Parameters.AddWithValue("$cr", d.CustomerReference ?? "");
                cmd.Parameters.AddWithValue("$t", d.TypeOfWork ?? "");
                cmd.Parameters.AddWithValue("$q", d.OrderQty);
                cmd.Parameters.AddWithValue("$s", d.OrderSQM);
                cmd.Parameters.AddWithValue("$sm", d.Salesman ?? "");
                cmd.Parameters.AddWithValue("$st", d.Status ?? "");
                cmd.Parameters.AddWithValue("$n", d.Notes ?? "");
                cmd.Parameters.AddWithValue("$ud", d.UpdatedDate.ToString("yyyy-MM-dd HH:mm"));

                cmd.ExecuteNonQuery();
            });
        }

        public static List<Delivery> GetAllDeliveries()
        {
            return Execute(conn =>
            {
                var list = new List<Delivery>();
                using var cmd = conn.CreateCommand();
                cmd.CommandText = "SELECT * FROM Deliveries ORDER BY Id DESC";
                using var r = cmd.ExecuteReader();
                while (r.Read())
                {
                    list.Add(new Delivery
                    {
                        Id = r.GetInt32(0),
                        SourceId = r.IsDBNull(1) ? 0 : r.GetInt32(1),
                        Date = DateTime.TryParse(r.GetString(2), out var d) ? d : DateTime.Today,
                        Company = r.IsDBNull(3) ? "" : r.GetString(3),
                        PINumber = r.IsDBNull(4) ? "" : r.GetString(4),
                        CustomerReference = r.IsDBNull(5) ? "" : r.GetString(5),
                        TypeOfWork = r.IsDBNull(6) ? "" : r.GetString(6),
                        OrderQty = r.GetInt32(7),
                        OrderSQM = r.GetDouble(8),
                        Salesman = r.IsDBNull(9) ? "" : r.GetString(9),
                        Status = r.IsDBNull(10) ? "" : r.GetString(10),
                        Notes = r.IsDBNull(11) ? "" : r.GetString(11),
                        CreatedDate = DateTime.TryParse(r.GetString(12), out var cd) ? cd : DateTime.Today,
                        UpdatedDate = DateTime.TryParse(r.GetString(13), out var ud) ? ud : DateTime.Today
                    });
                }
                return list;
            });
        }

        public static void DeleteDelivery(int id)
        {
            Execute(conn =>
            {
                using var cmd = conn.CreateCommand();
                cmd.CommandText = "DELETE FROM DeliveryItems WHERE OrderId = $id";
                cmd.Parameters.AddWithValue("$id", id);
                cmd.ExecuteNonQuery();

                cmd.CommandText = "DELETE FROM Deliveries WHERE Id = $id";
                cmd.ExecuteNonQuery();
            });
        }

        // ================= DELIVERY ITEMS =================
        public static void SaveDeliveryItem(DeliveryItem item)
        {
            Execute(conn =>
            {
                using var cmd = conn.CreateCommand();
                cmd.CommandText = @"
                INSERT INTO DeliveryItems (OrderId, DeliveryDate, DeliveredQty, DeliveredSQM, ReturnedQty, ReturnedSQM, Driver, Vehicle, Notes, CreatedDate)
                VALUES ($oid, $d, $dq, $ds, $rq, $rs, $dr, $v, $n, $cd);";

                cmd.Parameters.AddWithValue("$oid", item.OrderId);
                cmd.Parameters.AddWithValue("$d", item.DeliveryDate.ToString("yyyy-MM-dd"));
                cmd.Parameters.AddWithValue("$dq", item.DeliveredQty);
                cmd.Parameters.AddWithValue("$ds", item.DeliveredSQM);
                cmd.Parameters.AddWithValue("$rq", item.ReturnedQty);
                cmd.Parameters.AddWithValue("$rs", item.ReturnedSQM);
                cmd.Parameters.AddWithValue("$dr", item.Driver ?? "");
                cmd.Parameters.AddWithValue("$v", item.Vehicle ?? "");
                cmd.Parameters.AddWithValue("$n", item.Notes ?? "");
                cmd.Parameters.AddWithValue("$cd", item.CreatedDate.ToString("yyyy-MM-dd HH:mm"));

                cmd.ExecuteNonQuery();
            });
        }

        // ✅ NEW: UpdateDeliveryItem method
        public static void UpdateDeliveryItem(DeliveryItem item)
        {
            Execute(conn =>
            {
                using var cmd = conn.CreateCommand();
                cmd.CommandText = @"
                    UPDATE DeliveryItems SET 
                        DeliveryDate = $d, 
                        DeliveredQty = $dq, 
                        DeliveredSQM = $ds, 
                        ReturnedQty = $rq, 
                        ReturnedSQM = $rs, 
                        Driver = $dr, 
                        Vehicle = $v, 
                        Notes = $n
                    WHERE Id = $id;";

                cmd.Parameters.AddWithValue("$id", item.Id);
                cmd.Parameters.AddWithValue("$d", item.DeliveryDate.ToString("yyyy-MM-dd"));
                cmd.Parameters.AddWithValue("$dq", item.DeliveredQty);
                cmd.Parameters.AddWithValue("$ds", item.DeliveredSQM);
                cmd.Parameters.AddWithValue("$rq", item.ReturnedQty);
                cmd.Parameters.AddWithValue("$rs", item.ReturnedSQM);
                cmd.Parameters.AddWithValue("$dr", item.Driver ?? "");
                cmd.Parameters.AddWithValue("$v", item.Vehicle ?? "");
                cmd.Parameters.AddWithValue("$n", item.Notes ?? "");

                cmd.ExecuteNonQuery();
            });
        }

        public static List<DeliveryItem> GetDeliveryItems(int orderId)
        {
            return Execute(conn =>
            {
                var list = new List<DeliveryItem>();
                using var cmd = conn.CreateCommand();
                cmd.CommandText = "SELECT * FROM DeliveryItems WHERE OrderId = $oid ORDER BY Id DESC";
                cmd.Parameters.AddWithValue("$oid", orderId);
                using var r = cmd.ExecuteReader();
                while (r.Read())
                {
                    list.Add(new DeliveryItem
                    {
                        Id = r.GetInt32(0),
                        OrderId = r.GetInt32(1),
                        DeliveryDate = DateTime.TryParse(r.GetString(2), out var d) ? d : DateTime.Today,
                        DeliveredQty = r.GetInt32(3),
                        DeliveredSQM = r.GetDouble(4),
                        ReturnedQty = r.GetInt32(5),
                        ReturnedSQM = r.GetDouble(6),
                        Driver = r.IsDBNull(7) ? "" : r.GetString(7),
                        Vehicle = r.IsDBNull(8) ? "" : r.GetString(8),
                        Notes = r.IsDBNull(9) ? "" : r.GetString(9),
                        CreatedDate = DateTime.TryParse(r.GetString(10), out var cd) ? cd : DateTime.Today
                    });
                }
                return list;
            });
        }

        public static void DeleteDeliveryItem(int id)
        {
            Execute(conn =>
            {
                using var cmd = conn.CreateCommand();
                cmd.CommandText = "DELETE FROM DeliveryItems WHERE Id = $id";
                cmd.Parameters.AddWithValue("$id", id);
                cmd.ExecuteNonQuery();
            });
        }

        // ═══════════════════════════════════════════════════════════════
        // SHEET STORE - SHEETS
        // ═══════════════════════════════════════════════════════════════
        public static void SaveSheet(Sheet s)
        {
            Execute(conn =>
            {
                using var cmd = conn.CreateCommand();
                cmd.CommandText = @"
                INSERT INTO SheetStore (Category, Thickness, Color, ColorHex, Width, Height, SquareMeter, PurchasePrice, SellPrice, TotalStock, UsedSheets, BalanceSheets, IsActive, Supplier, SupplierName, Description, CreatedDate, LatestPurchaseDate)
                VALUES ($cat, $th, $col, $hex, $w, $h, $sqm, $pp, $sp, $ts, $us, $bs, $act, $sup, $supn, $desc, $cd, $lpd);";

                cmd.Parameters.AddWithValue("$cat", s.Category ?? "");
                cmd.Parameters.AddWithValue("$th", s.Thickness ?? "");
                cmd.Parameters.AddWithValue("$col", s.Color ?? "");
                cmd.Parameters.AddWithValue("$hex", s.ColorHex ?? "");
                cmd.Parameters.AddWithValue("$w", s.Width);
                cmd.Parameters.AddWithValue("$h", s.Height);
                cmd.Parameters.AddWithValue("$sqm", s.SquareMeter);
                cmd.Parameters.AddWithValue("$pp", s.PurchasePrice);
                cmd.Parameters.AddWithValue("$sp", s.SellPrice);
                cmd.Parameters.AddWithValue("$ts", s.TotalStock);
                cmd.Parameters.AddWithValue("$us", s.UsedSheets);
                cmd.Parameters.AddWithValue("$bs", s.BalanceSheets);
                cmd.Parameters.AddWithValue("$act", s.IsActive ? 1 : 0);
                cmd.Parameters.AddWithValue("$sup", s.Supplier ?? "");
                cmd.Parameters.AddWithValue("$supn", s.SupplierName ?? "");
                cmd.Parameters.AddWithValue("$desc", s.Description ?? "");
                cmd.Parameters.AddWithValue("$cd", s.CreatedDate.ToString("yyyy-MM-dd HH:mm"));
                cmd.Parameters.AddWithValue("$lpd", s.LatestPurchaseDate?.ToString("yyyy-MM-dd HH:mm") ?? "");

                cmd.ExecuteNonQuery();
            });
        }

        public static void UpdateSheet(Sheet s)
        {
            Execute(conn =>
            {
            using var cmd = conn.CreateCommand();
            cmd.CommandText = @"
                UPDATE SheetStore SET 
                    Category = $cat, Thickness = $th, Color = $col, ColorHex = $hex,
                    Width = $w, Height = $h, SquareMeter = $sqm, PurchasePrice = $pp,
                    SellPrice = $sp, TotalStock = $ts, UsedSheets = $us, BalanceSheets = $bs,
                    IsActive = $act, Supplier = $sup, SupplierName = $supn, Description = $desc,
                    LatestPurchaseDate = $lpd
                WHERE Id = $id;";

            cmd.Parameters.AddWithValue("$id", s.Id);
            cmd.Parameters.AddWithValue("$cat", s.Category ?? "");
            cmd.Parameters.AddWithValue("$th", s.Thickness ?? "");
            cmd.Parameters.AddWithValue("$col", s.Color ?? "");
            cmd.Parameters.AddWithValue("$hex", s.ColorHex ?? "");
            cmd.Parameters.AddWithValue("$w", s.Width);
            cmd.Parameters.AddWithValue("$h", s.Height);
            cmd.Parameters.AddWithValue("$sqm", s.SquareMeter);
            cmd.Parameters.AddWithValue("$pp", s.PurchasePrice);
            cmd.Parameters.AddWithValue("$sp", s.SellPrice);
            cmd.Parameters.AddWithValue("$ts", s.TotalStock);
            cmd.Parameters.AddWithValue("$us", s.UsedSheets);
            cmd.Parameters.AddWithValue("$bs", s.BalanceSheets);
            cmd.Parameters.AddWithValue("$act", s.IsActive ? 1 : 0);
                cmd.Parameters.AddWithValue("$supn", s.SupplierName ?? "");
                cmd.Parameters.AddWithValue("$desc", s.Description ?? "");
                cmd.Parameters.AddWithValue("$lpd", s.LatestPurchaseDate?.ToString("yyyy-MM-dd HH:mm") ?? "");

                cmd.ExecuteNonQuery();
            });
        }

        public static List<Sheet> GetAllSheets()
        {
            return Execute(conn =>
            {
                var list = new List<Sheet>();
                using var cmd = conn.CreateCommand();
                cmd.CommandText = "SELECT * FROM SheetStore ORDER BY Id DESC";
                using var r = cmd.ExecuteReader();
                while (r.Read())
                {
                    list.Add(new Sheet
                    {
                        Id = r.GetInt32(0),
                        Category = r.IsDBNull(1) ? "" : r.GetString(1),
                        Thickness = r.IsDBNull(2) ? "" : r.GetString(2),
                        Color = r.IsDBNull(3) ? "" : r.GetString(3),
                        ColorHex = r.IsDBNull(4) ? "" : r.GetString(4),
                        Width = r.GetInt32(5),
                        Height = r.GetInt32(6),
                        SquareMeter = r.GetDouble(7),
                        PurchasePrice = r.GetDecimal(8),
                        SellPrice = r.GetDecimal(9),
                        TotalStock = r.GetInt32(10),
                        UsedSheets = r.GetInt32(11),
                        BalanceSheets = r.GetInt32(12),
                        IsActive = r.GetInt32(13) == 1,
                        Supplier = r.IsDBNull(14) ? "" : r.GetString(14),
                        SupplierName = r.IsDBNull(15) ? "" : r.GetString(15),
                        Description = r.IsDBNull(16) ? "" : r.GetString(16),
                        CreatedDate = DateTime.TryParse(r.GetString(17), out var cd) ? cd : DateTime.Now,
                        LatestPurchaseDate = DateTime.TryParse(r.GetString(18), out var lpd) ? lpd : null
                    });
                }
                return list;
            });
        }

        public static void DeleteSheet(int id)
        {
            Execute(conn =>
            {
                using var cmd = conn.CreateCommand();
                cmd.CommandText = "DELETE FROM SheetUsages WHERE SheetId = $id";
                cmd.Parameters.AddWithValue("$id", id);
                cmd.ExecuteNonQuery();

                cmd.CommandText = "DELETE FROM SheetPurchases WHERE SheetId = $id";
                cmd.ExecuteNonQuery();

                cmd.CommandText = "DELETE FROM SheetStore WHERE Id = $id";
                cmd.ExecuteNonQuery();
            });
        }

        // ═══════════════════════════════════════════════════════════════
        // SHEET STORE - PURCHASES
        // ═══════════════════════════════════════════════════════════════
        public static void SaveSheetPurchase(SheetPurchase p)
        {
            Execute(conn =>
            {
                using var cmd = conn.CreateCommand();
                cmd.CommandText = @"
                INSERT INTO SheetPurchases (SheetId, Quantity, UnitPrice, Supplier, PurchasedOn, Notes, CreatedAt)
                VALUES ($sid, $q, $up, $sup, $pd, $nt, $cd);";

                cmd.Parameters.AddWithValue("$sid", p.SheetId);
                cmd.Parameters.AddWithValue("$q", p.Quantity);
                cmd.Parameters.AddWithValue("$up", p.UnitPrice);
                cmd.Parameters.AddWithValue("$sup", p.Supplier ?? "");
                cmd.Parameters.AddWithValue("$pd", p.PurchasedOn.ToString("yyyy-MM-dd"));
                cmd.Parameters.AddWithValue("$nt", p.Notes ?? "");
                cmd.Parameters.AddWithValue("$cd", p.CreatedAt.ToString("yyyy-MM-dd HH:mm"));

                cmd.ExecuteNonQuery();
            });
        }

        public static List<SheetPurchase> GetSheetPurchases(int sheetId)
        {
            return Execute(conn =>
            {
                var list = new List<SheetPurchase>();
                using var cmd = conn.CreateCommand();
                cmd.CommandText = "SELECT * FROM SheetPurchases WHERE SheetId = $sid ORDER BY Id DESC";
                cmd.Parameters.AddWithValue("$sid", sheetId);
                using var r = cmd.ExecuteReader();
                while (r.Read())
                {
                    list.Add(new SheetPurchase
                    {
                        Id = r.GetInt32(0),
                        SheetId = r.GetInt32(1),
                        Quantity = r.GetInt32(2),
                        UnitPrice = r.GetDecimal(3),
                        Supplier = r.IsDBNull(4) ? "" : r.GetString(4),
                        PurchasedOn = DateTime.TryParse(r.GetString(5), out var pd) ? pd : DateTime.Today,
                        Notes = r.IsDBNull(6) ? "" : r.GetString(6),
                        CreatedAt = DateTime.TryParse(r.GetString(7), out var cd) ? cd : DateTime.Now
                    });
                }
                return list;
            });
        }

        public static List<SheetPurchase> GetAllSheetPurchases()
        {
            return Execute(conn =>
            {
                var list = new List<SheetPurchase>();
                using var cmd = conn.CreateCommand();
                cmd.CommandText = "SELECT * FROM SheetPurchases ORDER BY Id DESC";
                using var r = cmd.ExecuteReader();
                while (r.Read())
                {
                    list.Add(new SheetPurchase
                    {
                        Id = r.GetInt32(0),
                        SheetId = r.GetInt32(1),
                        Quantity = r.GetInt32(2),
                        UnitPrice = r.GetDecimal(3),
                        Supplier = r.IsDBNull(4) ? "" : r.GetString(4),
                        PurchasedOn = DateTime.TryParse(r.GetString(5), out var pd) ? pd : DateTime.Today,
                        Notes = r.IsDBNull(6) ? "" : r.GetString(6),
                        CreatedAt = DateTime.TryParse(r.GetString(7), out var cd) ? cd : DateTime.Now
                    });
                }
                return list;
            });
        }

        // ═══════════════════════════════════════════════════════════════
        // SHEET STORE - USAGES
        // ═══════════════════════════════════════════════════════════════
        public static void SaveSheetUsage(SheetUsage u)
        {
            Execute(conn =>
            {
                using var cmd = conn.CreateCommand();
                cmd.CommandText = @"
                INSERT INTO SheetUsages (SheetId, Quantity, Reason, UsedOn, CreatedAt)
                VALUES ($sid, $q, $rs, $ud, $cd);";

                cmd.Parameters.AddWithValue("$sid", u.SheetId);
                cmd.Parameters.AddWithValue("$q", u.Quantity);
                cmd.Parameters.AddWithValue("$rs", u.Reason ?? "");
                cmd.Parameters.AddWithValue("$ud", u.UsedOn.ToString("yyyy-MM-dd"));
                cmd.Parameters.AddWithValue("$cd", u.CreatedAt.ToString("yyyy-MM-dd HH:mm"));

                cmd.ExecuteNonQuery();
            });
        }

        public static List<SheetUsage> GetSheetUsages(int sheetId)
        {
            return Execute(conn =>
            {
                var list = new List<SheetUsage>();
                using var cmd = conn.CreateCommand();
                cmd.CommandText = "SELECT * FROM SheetUsages WHERE SheetId = $sid ORDER BY Id DESC";
                cmd.Parameters.AddWithValue("$sid", sheetId);
                using var r = cmd.ExecuteReader();
                while (r.Read())
                {
                    list.Add(new SheetUsage
                    {
                        Id = r.GetInt32(0),
                        SheetId = r.GetInt32(1),
                        Quantity = r.GetInt32(2),
                        Reason = r.IsDBNull(3) ? "" : r.GetString(3),
                        UsedOn = DateTime.TryParse(r.GetString(4), out var ud) ? ud : DateTime.Today,
                        CreatedAt = DateTime.TryParse(r.GetString(5), out var cd) ? cd : DateTime.Now
                    });
                }
                return list;
            });
        }

        public static List<SheetUsage> GetAllSheetUsages()
        {
            return Execute(conn =>
            {
                var list = new List<SheetUsage>();
                using var cmd = conn.CreateCommand();
                cmd.CommandText = "SELECT * FROM SheetUsages ORDER BY Id DESC";
                using var r = cmd.ExecuteReader();
                while (r.Read())
                {
                    list.Add(new SheetUsage
                    {
                        Id = r.GetInt32(0),
                        SheetId = r.GetInt32(1),
                        Quantity = r.GetInt32(2),
                        Reason = r.IsDBNull(3) ? "" : r.GetString(3),
                        UsedOn = DateTime.TryParse(r.GetString(4), out var ud) ? ud : DateTime.Today,
                        CreatedAt = DateTime.TryParse(r.GetString(5), out var cd) ? cd : DateTime.Now
                    });
                }
                return list;
            });
        }
    }
}