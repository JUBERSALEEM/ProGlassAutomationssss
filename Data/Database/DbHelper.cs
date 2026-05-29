// Data/Database/DbHelper.cs
using Microsoft.Data.Sqlite;
using ProGlassAutomation.Models;
using ProGlassAutomation.ViewModels;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;
using Newtonsoft.Json;

namespace ProGlassAutomation.Data.Database
{
    public static class DbHelper
    {
        private static readonly string DbPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "ProGlassAutomation", "glass.db");

        private static readonly string ConnStr = $"Data Source={DbPath};Cache=Shared";
        private static readonly int LatestVersion = 13;

        private static SqliteConnection CreateConnection()
        {
            Directory.CreateDirectory(Path.GetDirectoryName(DbPath)!);
            return new SqliteConnection(ConnStr);
        }

        private static void Execute(Action<SqliteConnection> action)
        {
            using var conn = CreateConnection();
            conn.Open();
            try { action(conn); }
            catch (Exception ex) { Log(ex); throw; }
        }

        private static T Execute<T>(Func<SqliteConnection, T> func)
        {
            using var conn = CreateConnection();
            conn.Open();
            try { return func(conn); }
            catch (Exception ex) { Log(ex); throw; }
        }

        private static void Log(Exception ex)
        {
            try
            {
                string logPath = Path.Combine(Path.GetDirectoryName(DbPath)!, "db_log.txt");
                File.AppendAllText(logPath, $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] {ex.Message}\n{ex.StackTrace}\n\n");
            }
            catch { }
        }

        public static bool TestConnection()
        {
            try
            {
                return Execute(conn =>
                {
                    using var cmd = conn.CreateCommand();
                    cmd.CommandText = "SELECT name FROM sqlite_master WHERE type='table'";
                    using var reader = cmd.ExecuteReader();
                    return true;
                });
            }
            catch { return false; }
        }

        public static void Init()
        {
            Directory.CreateDirectory(Path.GetDirectoryName(DbPath)!);
            Execute(conn =>
            {
                CreateVersionTable(conn);
                int current = GetVersion(conn);
                for (int v = current + 1; v <= LatestVersion; v++)
                {
                    ApplyMigration(conn, v);
                    SetVersion(conn, v);
                }
                EnsureLaminationColumns(conn);
            });
        }

        private static void CreateVersionTable(SqliteConnection conn)
        {
            using var cmd = conn.CreateCommand();
            cmd.CommandText = "CREATE TABLE IF NOT EXISTS DbVersion (Id INTEGER PRIMARY KEY, Version INTEGER)";
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
            cmd.CommandText = "INSERT OR REPLACE INTO DbVersion (Id, Version) VALUES (1, $v)";
            cmd.Parameters.AddWithValue("$v", version);
            cmd.ExecuteNonQuery();
        }

        private static void ApplyMigration(SqliteConnection conn, int version)
        {
            switch (version)
            {
                case 1:
                    using (var cmd = conn.CreateCommand())
                    {
                        cmd.CommandText = @"
CREATE TABLE IF NOT EXISTS SGURecords (Id INTEGER PRIMARY KEY AUTOINCREMENT, Thickness TEXT, Color TEXT, Result REAL, CreatedAt TEXT);
CREATE TABLE IF NOT EXISTS DGURecords (Id INTEGER PRIMARY KEY AUTOINCREMENT, Thickness1 TEXT, Color1 TEXT, Thickness2 TEXT, Color2 TEXT, Spacer TEXT, Result REAL, CreatedAt TEXT);
CREATE TABLE IF NOT EXISTS LaminationRecords (Id INTEGER PRIMARY KEY AUTOINCREMENT, Thickness1 TEXT, Color1 TEXT, Thickness2 TEXT, Color2 TEXT, PVBType TEXT, Result REAL, CreatedAt TEXT, Cutting REAL DEFAULT 0, Tempering REAL DEFAULT 0, IncludeCutting INTEGER DEFAULT 0, IncludeTempering INTEGER DEFAULT 0);";
                        cmd.ExecuteNonQuery();
                    }
                    break;

                case 2:
                    ExecuteSafeAlter(conn, "ALTER TABLE LaminationRecords ADD COLUMN Cutting REAL DEFAULT 0");
                    ExecuteSafeAlter(conn, "ALTER TABLE LaminationRecords ADD COLUMN Tempering REAL DEFAULT 0");
                    ExecuteSafeAlter(conn, "ALTER TABLE LaminationRecords ADD COLUMN IncludeCutting INTEGER DEFAULT 0");
                    ExecuteSafeAlter(conn, "ALTER TABLE LaminationRecords ADD COLUMN IncludeTempering INTEGER DEFAULT 0");
                    break;

                case 4:
                    using (var cmd = conn.CreateCommand())
                    {
                        cmd.CommandText = @"
CREATE TABLE IF NOT EXISTS DailyWork (Id INTEGER PRIMARY KEY AUTOINCREMENT, Date TEXT, UpdateDate TEXT, Company TEXT, PINumber TEXT, CustomerReference TEXT, TypeOfWork TEXT, ProductionStatus TEXT, DailyReportStatus TEXT, Qty INTEGER, SQM REAL, Status TEXT, Salesman TEXT, Color TEXT, Notes TEXT, CreatedDate TEXT);
CREATE TABLE IF NOT EXISTS Deliveries (Id INTEGER PRIMARY KEY AUTOINCREMENT, SourceId INTEGER, Date TEXT, Company TEXT, PINumber TEXT, CustomerReference TEXT, TypeOfWork TEXT, OrderQty INTEGER, OrderSQM REAL, Salesman TEXT, Status TEXT, Notes TEXT, CreatedDate TEXT, UpdatedDate TEXT);
CREATE TABLE IF NOT EXISTS DeliveryItems (Id INTEGER PRIMARY KEY AUTOINCREMENT, OrderId INTEGER, DeliveryDate TEXT, DeliveredQty INTEGER, DeliveredSQM REAL, ReturnedQty INTEGER, ReturnedSQM REAL, Driver TEXT, Vehicle TEXT, Notes TEXT, CreatedDate TEXT);";
                        cmd.ExecuteNonQuery();
                    }
                    break;

                case 5:
                    using (var cmd = conn.CreateCommand())
                    {
                        cmd.CommandText = @"
CREATE TABLE IF NOT EXISTS SheetStore (Id INTEGER PRIMARY KEY AUTOINCREMENT, Category TEXT, Thickness TEXT, Color TEXT, ColorHex TEXT, Width INTEGER, Height INTEGER, SquareMeter REAL, PurchasePrice REAL, SellPrice REAL, TotalStock INTEGER, UsedSheets INTEGER, BalanceSheets INTEGER, IsActive INTEGER DEFAULT 1, Supplier TEXT, SupplierName TEXT, Description TEXT, CreatedDate TEXT, LatestPurchaseDate TEXT);
CREATE TABLE IF NOT EXISTS SheetPurchases (Id INTEGER PRIMARY KEY AUTOINCREMENT, SheetId INTEGER, Quantity INTEGER, UnitPrice REAL, Supplier TEXT, PurchasedOn TEXT, Notes TEXT, CreatedAt TEXT);
CREATE TABLE IF NOT EXISTS SheetUsages (Id INTEGER PRIMARY KEY AUTOINCREMENT, SheetId INTEGER, Quantity INTEGER, Reason TEXT, UsedOn TEXT, CreatedAt TEXT);";
                        cmd.ExecuteNonQuery();
                    }
                    break;

                case 6:
                    using (var cmd = conn.CreateCommand())
                    {
                        cmd.CommandText = @"
CREATE TABLE IF NOT EXISTS CalculationLogs (Id INTEGER PRIMARY KEY AUTOINCREMENT, ModuleType TEXT NOT NULL, SQM REAL NOT NULL, Notes TEXT, CreatedDate TEXT NOT NULL);
CREATE INDEX IF NOT EXISTS idx_calc_module_date ON CalculationLogs(ModuleType, CreatedDate);
CREATE TABLE IF NOT EXISTS SystemMetrics (Id INTEGER PRIMARY KEY AUTOINCREMENT, MetricType TEXT NOT NULL, MetricValue REAL NOT NULL, CreatedDate TEXT NOT NULL);
CREATE INDEX IF NOT EXISTS idx_metrics_date ON SystemMetrics(CreatedDate);
CREATE TABLE IF NOT EXISTS ImportSessions (Id INTEGER PRIMARY KEY AUTOINCREMENT, SessionDateTime TEXT NOT NULL, Notes TEXT, ImportedCount INTEGER DEFAULT 0, UpdatedCount INTEGER DEFAULT 0, SkippedCount INTEGER DEFAULT 0);
CREATE TABLE IF NOT EXISTS ImportLogs (Id INTEGER PRIMARY KEY AUTOINCREMENT, SessionId INTEGER, ImportDateTime TEXT NOT NULL, PINumber TEXT, Company TEXT, ChangesJson TEXT);";
                        cmd.ExecuteNonQuery();
                    }
                    break;

                case 7:
                    using (var cmd = conn.CreateCommand())
                    {
                        cmd.CommandText = @"CREATE TABLE IF NOT EXISTS SGUHistory (Id INTEGER PRIMARY KEY AUTOINCREMENT, Category TEXT, Thickness TEXT, Color TEXT, SheetPrice REAL, Cutting REAL, TemperingCharge REAL, OtherCharges REAL, Wastage TEXT, ProfitMargin TEXT, EdgeWork TEXT, Drilling TEXT, Tempering TEXT, Coating TEXT, SurfaceTreatment TEXT, Cutout TEXT, Unit TEXT, Width INTEGER, Height INTEGER, Quantity INTEGER, TotalArea REAL, TotalPrice REAL, Result REAL, CustomNotes TEXT, CreatedAt TEXT);";
                        cmd.ExecuteNonQuery();
                    }
                    break;

                case 8:
                    using (var cmd = conn.CreateCommand())
                    {
                        cmd.CommandText = @"CREATE TABLE IF NOT EXISTS CustomerReferences (Id INTEGER PRIMARY KEY AUTOINCREMENT, CustomerReference TEXT NOT NULL UNIQUE, Company TEXT, CreatedAt TEXT); CREATE TABLE IF NOT EXISTS NotesSuggestions (Id INTEGER PRIMARY KEY AUTOINCREMENT, Note TEXT NOT NULL UNIQUE, UseCount INTEGER DEFAULT 1, LastUsedAt TEXT);";
                        cmd.ExecuteNonQuery();
                    }
                    break;

                case 9:
                    using (var cmd = conn.CreateCommand())
                    {
                        cmd.CommandText = @"
CREATE TABLE IF NOT EXISTS ProformaInvoices (Id INTEGER PRIMARY KEY AUTOINCREMENT, PINumber TEXT NOT NULL UNIQUE, ClientName TEXT, ClientTRN TEXT, ClientAddress TEXT, ProjectName TEXT, ProjectLocation TEXT, LPONumber TEXT, Attention TEXT, ContactNo TEXT, PIDate TEXT, ValidUntil TEXT, Status TEXT DEFAULT 'Draft', TotalAmount REAL DEFAULT 0, VATPercent REAL DEFAULT 5, VATAmount REAL DEFAULT 0, NetAmount REAL DEFAULT 0, CompanyName TEXT, CompanyTRN TEXT, CompanyLocation TEXT, CompanyPhone TEXT, Notes TEXT, CreatedDate TEXT, UpdatedDate TEXT);
CREATE TABLE IF NOT EXISTS ProformaInvoiceItems (Id INTEGER PRIMARY KEY AUTOINCREMENT, ProformaInvoiceId INTEGER NOT NULL, SrNo INTEGER, GlassRef TEXT, Width1 REAL, Height1 REAL, Width2 REAL, Height2 REAL, Qty INTEGER DEFAULT 1, SQM REAL DEFAULT 0, TotalSQM REAL DEFAULT 0, Price REAL DEFAULT 0, TotalPrice REAL DEFAULT 0, SurchargePercent REAL DEFAULT 0, SurchargeThreshold REAL DEFAULT 0, FOREIGN KEY (ProformaInvoiceId) REFERENCES ProformaInvoices(Id));
CREATE TABLE IF NOT EXISTS JobOrders (Id INTEGER PRIMARY KEY AUTOINCREMENT, JONumber TEXT NOT NULL UNIQUE, ProformaInvoiceId INTEGER, ClientName TEXT, ProjectName TEXT, ProjectLocation TEXT, JODate TEXT, RequiredDate TEXT, Status TEXT DEFAULT 'Pending', TotalQty INTEGER DEFAULT 0, ReleasedQty INTEGER DEFAULT 0, BalanceQty INTEGER DEFAULT 0, TotalAmount REAL DEFAULT 0, Notes TEXT, CreatedDate TEXT, UpdatedDate TEXT, FOREIGN KEY (ProformaInvoiceId) REFERENCES ProformaInvoices(Id));
CREATE TABLE IF NOT EXISTS JobOrderItems (Id INTEGER PRIMARY KEY AUTOINCREMENT, JobOrderId INTEGER NOT NULL, SrNo INTEGER, GlassRef TEXT, Width REAL, Height REAL, OrderedQty INTEGER DEFAULT 0, ReleasedQty INTEGER DEFAULT 0, BalanceQty INTEGER DEFAULT 0, Price REAL DEFAULT 0, TotalAmount REAL DEFAULT 0, FOREIGN KEY (JobOrderId) REFERENCES JobOrders(Id));
CREATE TABLE IF NOT EXISTS DeliveryOrders (Id INTEGER PRIMARY KEY AUTOINCREMENT, DONumber TEXT NOT NULL UNIQUE, JobOrderId INTEGER, ProformaInvoiceId INTEGER, ClientName TEXT, ProjectName TEXT, DODate TEXT, Status TEXT DEFAULT 'Pending', VehicleNumber TEXT, DriverName TEXT, Notes TEXT, TotalQty INTEGER DEFAULT 0, DeliveredQty INTEGER DEFAULT 0, CreatedDate TEXT, UpdatedDate TEXT, FOREIGN KEY (JobOrderId) REFERENCES JobOrders(Id), FOREIGN KEY (ProformaInvoiceId) REFERENCES ProformaInvoices(Id));
CREATE TABLE IF NOT EXISTS DeliveryOrderItems (Id INTEGER PRIMARY KEY AUTOINCREMENT, DeliveryOrderId INTEGER NOT NULL, SrNo INTEGER, GlassRef TEXT, Width REAL, Height REAL, DeliveredQty INTEGER DEFAULT 0, FOREIGN KEY (DeliveryOrderId) REFERENCES DeliveryOrders(Id));
CREATE TABLE IF NOT EXISTS TaxInvoices (Id INTEGER PRIMARY KEY AUTOINCREMENT, InvoiceNumber TEXT NOT NULL UNIQUE, ProformaInvoiceId INTEGER, JobOrderId INTEGER, DeliveryOrderId INTEGER, ClientName TEXT, ClientTRN TEXT, ClientAddress TEXT, InvoiceDate TEXT, DueDate TEXT, Status TEXT DEFAULT 'Pending', PaymentStatus TEXT DEFAULT 'Unpaid', SubTotal REAL DEFAULT 0, VATPercent REAL DEFAULT 5, VATAmount REAL DEFAULT 0, TotalAmount REAL DEFAULT 0, PaidAmount REAL DEFAULT 0, BalanceAmount REAL DEFAULT 0, Notes TEXT, CreatedDate TEXT, UpdatedDate TEXT, FOREIGN KEY (ProformaInvoiceId) REFERENCES ProformaInvoices(Id), FOREIGN KEY (JobOrderId) REFERENCES JobOrders(Id), FOREIGN KEY (DeliveryOrderId) REFERENCES DeliveryOrders(Id));
CREATE TABLE IF NOT EXISTS TaxInvoiceItems (Id INTEGER PRIMARY KEY AUTOINCREMENT, TaxInvoiceId INTEGER NOT NULL, SrNo INTEGER, Description TEXT, Qty INTEGER DEFAULT 0, UnitPrice REAL DEFAULT 0, TotalPrice REAL DEFAULT 0, FOREIGN KEY (TaxInvoiceId) REFERENCES TaxInvoices(Id));
CREATE INDEX IF NOT EXISTS idx_pi_date ON ProformaInvoices(PIDate);
CREATE INDEX IF NOT EXISTS idx_jo_date ON JobOrders(JODate);
CREATE INDEX IF NOT EXISTS idx_do_date ON DeliveryOrders(DODate);
CREATE INDEX IF NOT EXISTS idx_ti_date ON TaxInvoices(InvoiceDate);";
                        cmd.ExecuteNonQuery();
                    }
                    break;
                case 10:
                    // Ensure all tables exist (for databases that might be missing them)
                    using (var cmd = conn.CreateCommand())
                    {
                        cmd.CommandText = @"
CREATE TABLE IF NOT EXISTS JobOrders (Id INTEGER PRIMARY KEY AUTOINCREMENT, JONumber TEXT NOT NULL UNIQUE, ProformaInvoiceId INTEGER, ClientName TEXT, ProjectName TEXT, ProjectLocation TEXT, JODate TEXT, RequiredDate TEXT, Status TEXT DEFAULT 'Pending', TotalQty INTEGER DEFAULT 0, ReleasedQty INTEGER DEFAULT 0, BalanceQty INTEGER DEFAULT 0, TotalAmount REAL DEFAULT 0, Notes TEXT, CreatedDate TEXT, UpdatedDate TEXT);
CREATE TABLE IF NOT EXISTS JobOrderItems (Id INTEGER PRIMARY KEY AUTOINCREMENT, JobOrderId INTEGER NOT NULL, SrNo INTEGER, GlassRef TEXT, Width REAL, Height REAL, OrderedQty INTEGER DEFAULT 0, ReleasedQty INTEGER DEFAULT 0, BalanceQty INTEGER DEFAULT 0, Price REAL DEFAULT 0, TotalAmount REAL DEFAULT 0);";
                        cmd.ExecuteNonQuery();
                    }
                    break;

                case 11:
                    // Add new columns to JobOrders table for specifications JSON and additional fields
                    ExecuteSafeAlter(conn, "ALTER TABLE JobOrders ADD COLUMN ClientTRN TEXT");
                    ExecuteSafeAlter(conn, "ALTER TABLE JobOrders ADD COLUMN ClientAddress TEXT");
                    ExecuteSafeAlter(conn, "ALTER TABLE JobOrders ADD COLUMN ContactPerson TEXT");
                    ExecuteSafeAlter(conn, "ALTER TABLE JobOrders ADD COLUMN ContactNumber TEXT");
                    ExecuteSafeAlter(conn, "ALTER TABLE JobOrders ADD COLUMN LPONumber TEXT");
                    ExecuteSafeAlter(conn, "ALTER TABLE JobOrders ADD COLUMN VATAmount REAL DEFAULT 0");
                    ExecuteSafeAlter(conn, "ALTER TABLE JobOrders ADD COLUMN DiscountAmount REAL DEFAULT 0");
                    ExecuteSafeAlter(conn, "ALTER TABLE JobOrders ADD COLUMN NetAmount REAL DEFAULT 0");
                    ExecuteSafeAlter(conn, "ALTER TABLE JobOrders ADD COLUMN SpecificationsJson TEXT");
                    break;
                case 12:
                    // Ensure ProformaInvoices table exists (for databases that skipped migration 9)
                    using (var cmd = conn.CreateCommand())
                    {
                        cmd.CommandText = @"
CREATE TABLE IF NOT EXISTS ProformaInvoices (Id INTEGER PRIMARY KEY AUTOINCREMENT, PINumber TEXT NOT NULL UNIQUE, ClientName TEXT, ClientTRN TEXT, ClientAddress TEXT, ProjectName TEXT, ProjectLocation TEXT, LPONumber TEXT, Attention TEXT, ContactNo TEXT, PIDate TEXT, ValidUntil TEXT, Status TEXT DEFAULT 'Draft', TotalAmount REAL DEFAULT 0, VATPercent REAL DEFAULT 5, VATAmount REAL DEFAULT 0, NetAmount REAL DEFAULT 0, CompanyName TEXT, CompanyTRN TEXT, CompanyLocation TEXT, CompanyPhone TEXT, Notes TEXT, CreatedDate TEXT, UpdatedDate TEXT);
CREATE TABLE IF NOT EXISTS ProformaInvoiceItems (Id INTEGER PRIMARY KEY AUTOINCREMENT, ProformaInvoiceId INTEGER NOT NULL, SrNo INTEGER, GlassRef TEXT, Width1 REAL, Height1 REAL, Width2 REAL, Height2 REAL, Qty INTEGER DEFAULT 1, SQM REAL DEFAULT 0, TotalSQM REAL DEFAULT 0, Price REAL DEFAULT 0, TotalPrice REAL DEFAULT 0, SurchargePercent REAL DEFAULT 0, SurchargeThreshold REAL DEFAULT 0, FOREIGN KEY (ProformaInvoiceId) REFERENCES ProformaInvoices(Id));
CREATE INDEX IF NOT EXISTS idx_pi_date ON ProformaInvoices(PIDate);";
                        cmd.ExecuteNonQuery();
                    }
                    break;
                case 13:
                    ExecuteSafeAlter(conn, "ALTER TABLE ProformaInvoices ADD COLUMN Salesman TEXT");
                    ExecuteSafeAlter(conn, "ALTER TABLE ProformaInvoices ADD COLUMN JobOrderRef TEXT");
                    ExecuteSafeAlter(conn, "ALTER TABLE ProformaInvoices ADD COLUMN CustomerReference TEXT");
                    ExecuteSafeAlter(conn, "ALTER TABLE ProformaInvoices ADD COLUMN ProjectNo TEXT");
                    ExecuteSafeAlter(conn, "ALTER TABLE ProformaInvoices ADD COLUMN Color TEXT");
                    break;
            }
        }

        private static void ExecuteSafeAlter(SqliteConnection conn, string sql)
        {
            try { using var cmd = conn.CreateCommand(); cmd.CommandText = sql; cmd.ExecuteNonQuery(); } catch { }
        }

        private static void EnsureLaminationColumns(SqliteConnection conn)
        {
            ExecuteSafeAlter(conn, "ALTER TABLE LaminationRecords ADD COLUMN Cutting REAL DEFAULT 0");
            ExecuteSafeAlter(conn, "ALTER TABLE LaminationRecords ADD COLUMN Tempering REAL DEFAULT 0");
            ExecuteSafeAlter(conn, "ALTER TABLE LaminationRecords ADD COLUMN IncludeCutting INTEGER DEFAULT 0");
            ExecuteSafeAlter(conn, "ALTER TABLE LaminationRecords ADD COLUMN IncludeTempering INTEGER DEFAULT 0");
        }

        public static void AutoBackup()
        {
            try
            {
                string folder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), "GlassBackup");
                Directory.CreateDirectory(folder);
                string backupFile = Path.Combine(folder, $"glass_backup_{DateTime.Now:yyyyMMdd_HHmmss}.db");
                File.Copy(DbPath, backupFile, true);
            }
            catch (Exception ex) { Log(ex); }
        }

        // ═══════════════════════════════════════════════════
        // SGU METHODS
        // ═══════════════════════════════════════════════════

        public static void Save(string thickness, string color, double result)
        {
            Execute(conn =>
            {
                using var cmd = conn.CreateCommand();
                cmd.CommandText = "INSERT INTO SGURecords (Thickness, Color, Result, CreatedAt) VALUES ($t, $c, $r, $d)";
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
                cmd.CommandText = "SELECT Id, Thickness, Color, Result, CreatedAt FROM SGURecords ORDER BY Id DESC";
                using var r = cmd.ExecuteReader();
                while (r.Read())
                {
                    list.Add(new SguRecord { Id = r.GetInt32(0), Thickness = r.GetString(1), Color = r.GetString(2), Result = r.GetDouble(3), CreatedAt = r.GetString(4) });
                }
                return list;
            });
        }

        public static void SaveSguHistory(SguRecord r)
        {
            Execute(conn =>
            {
                using var cmd = conn.CreateCommand();
                cmd.CommandText = @"INSERT INTO SGUHistory (Category, Thickness, Color, SheetPrice, Cutting, TemperingCharge, OtherCharges, Wastage, ProfitMargin, EdgeWork, Drilling, Tempering, Coating, SurfaceTreatment, Cutout, Unit, Width, Height, Quantity, TotalArea, TotalPrice, Result, CustomNotes, CreatedAt)
VALUES ($cat, $th, $col, $sp, $cut, $temp, $other, $wast, $profit, $edge, $drill, $temp2, $coat, $surf, $cutout, $unit, $w, $h, $q, $area, $price, $result, $notes, $created)";
                cmd.Parameters.AddWithValue("$cat", r.Category ?? "");
                cmd.Parameters.AddWithValue("$th", r.Thickness ?? "");
                cmd.Parameters.AddWithValue("$col", r.Color ?? "");
                cmd.Parameters.AddWithValue("$sp", r.SheetPrice);
                cmd.Parameters.AddWithValue("$cut", r.Cutting);
                cmd.Parameters.AddWithValue("$temp", r.TemperingCharge);
                cmd.Parameters.AddWithValue("$other", r.OtherCharges);
                cmd.Parameters.AddWithValue("$wast", r.Wastage ?? "");
                cmd.Parameters.AddWithValue("$profit", r.ProfitMargin ?? "");
                cmd.Parameters.AddWithValue("$edge", r.EdgeWork ?? "");
                cmd.Parameters.AddWithValue("$drill", r.Drilling ?? "");
                cmd.Parameters.AddWithValue("$temp2", r.Tempering ?? "");
                cmd.Parameters.AddWithValue("$coat", r.Coating ?? "");
                cmd.Parameters.AddWithValue("$surf", r.SurfaceTreatment ?? "");
                cmd.Parameters.AddWithValue("$cutout", r.Cutout ?? "");
                cmd.Parameters.AddWithValue("$unit", r.Unit ?? "AED");
                cmd.Parameters.AddWithValue("$w", r.Width);
                cmd.Parameters.AddWithValue("$h", r.Height);
                cmd.Parameters.AddWithValue("$q", r.Quantity);
                cmd.Parameters.AddWithValue("$area", r.TotalArea);
                cmd.Parameters.AddWithValue("$price", r.TotalPrice);
                cmd.Parameters.AddWithValue("$result", r.Result);
                cmd.Parameters.AddWithValue("$notes", r.CustomNotes ?? "");
                cmd.Parameters.AddWithValue("$created", r.CreatedAt ?? DateTime.Now.ToString("yyyy-MM-dd HH:mm"));
                cmd.ExecuteNonQuery();
            });
        }

        public static List<SguRecord> GetAllSguHistory()
        {
            return Execute(conn =>
            {
                var list = new List<SguRecord>();
                using var cmd = conn.CreateCommand();
                cmd.CommandText = "SELECT * FROM SGUHistory ORDER BY Id DESC";
                using var r = cmd.ExecuteReader();
                while (r.Read())
                {
                    list.Add(new SguRecord
                    {
                        Id = r.GetInt32(0),
                        Category = r.IsDBNull(1) ? "" : r.GetString(1),
                        Thickness = r.IsDBNull(2) ? "" : r.GetString(2),
                        Color = r.IsDBNull(3) ? "" : r.GetString(3),
                        SheetPrice = r.GetDouble(4),
                        Cutting = r.GetDouble(5),
                        TemperingCharge = r.GetDouble(6),
                        OtherCharges = r.GetDouble(7),
                        Wastage = r.IsDBNull(8) ? "" : r.GetString(8),
                        ProfitMargin = r.IsDBNull(9) ? "" : r.GetString(9),
                        EdgeWork = r.IsDBNull(10) ? "" : r.GetString(10),
                        Drilling = r.IsDBNull(11) ? "" : r.GetString(11),
                        Tempering = r.IsDBNull(12) ? "" : r.GetString(12),
                        Coating = r.IsDBNull(13) ? "" : r.GetString(13),
                        SurfaceTreatment = r.IsDBNull(14) ? "" : r.GetString(14),
                        Cutout = r.IsDBNull(15) ? "" : r.GetString(15),
                        Unit = r.IsDBNull(16) ? "AED" : r.GetString(16),
                        Width = r.GetInt32(17),
                        Height = r.GetInt32(18),
                        Quantity = r.GetInt32(19),
                        TotalArea = r.GetDouble(20),
                        TotalPrice = r.GetDouble(21),
                        Result = r.GetDouble(22),
                        CustomNotes = r.IsDBNull(23) ? "" : r.GetString(23),
                        CreatedAt = r.IsDBNull(24) ? "" : r.GetString(24)
                    });
                }
                return list;
            });
        }

        public static void DeleteSguHistory(int id)
        {
            Execute(conn =>
            {
                using var cmd = conn.CreateCommand();
                cmd.CommandText = "DELETE FROM SGUHistory WHERE Id = $id";
                cmd.Parameters.AddWithValue("$id", id);
                cmd.ExecuteNonQuery();
            });
        }

        public static List<SguRecord> SearchSGU(string k)
        {
            return Execute(conn =>
            {
                var list = new List<SguRecord>();
                using var cmd = conn.CreateCommand();
                cmd.CommandText = "SELECT * FROM SGURecords WHERE Thickness LIKE $k OR Color LIKE $k ORDER BY Id DESC";
                cmd.Parameters.AddWithValue("$k", "%" + k + "%");
                using var r = cmd.ExecuteReader();
                while (r.Read())
                {
                    list.Add(new SguRecord { Id = r.GetInt32(0), Thickness = r.GetString(1), Color = r.GetString(2), Result = r.GetDouble(3), CreatedAt = r.GetString(4) });
                }
                return list;
            });
        }

        public static void DeleteSgu(int id)
        {
            Execute(conn =>
            {
                using var cmd = conn.CreateCommand();
                cmd.CommandText = "DELETE FROM SGURecords WHERE Id = $id";
                cmd.Parameters.AddWithValue("$id", id);
                cmd.ExecuteNonQuery();
            });
        }

        // ═══════════════════════════════════════════════════
        // DGU METHODS
        // ═══════════════════════════════════════════════════

        public static void SaveDgu(string t1, string c1, string t2, string c2, string spacer, double result)
        {
            Execute(conn =>
            {
                using var cmd = conn.CreateCommand();
                cmd.CommandText = "INSERT INTO DGURecords (Thickness1, Color1, Thickness2, Color2, Spacer, Result, CreatedAt) VALUES ($t1,$c1,$t2,$c2,$s,$r,$d)";
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
                cmd.CommandText = "SELECT Id, Thickness1, Color1, Thickness2, Color2, Spacer, Result, CreatedAt FROM DGURecords ORDER BY Id DESC";
                using var r = cmd.ExecuteReader();
                while (r.Read())
                {
                    list.Add(new DguRecord { Id = r.GetInt32(0), Thickness1 = r.GetString(1), Color1 = r.GetString(2), Thickness2 = r.GetString(3), Color2 = r.GetString(4), Spacer = r.GetString(5), Result = r.GetDouble(6), CreatedAt = r.GetString(7) });
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
                cmd.CommandText = "SELECT * FROM DGURecords WHERE Thickness1 LIKE $k OR Thickness2 LIKE $k OR Color1 LIKE $k OR Color2 LIKE $k ORDER BY Id DESC";
                cmd.Parameters.AddWithValue("$k", "%" + k + "%");
                using var r = cmd.ExecuteReader();
                while (r.Read())
                {
                    list.Add(new DguRecord { Id = r.GetInt32(0), Thickness1 = r.GetString(1), Color1 = r.GetString(2), Thickness2 = r.GetString(3), Color2 = r.GetString(4), Spacer = r.GetString(5), Result = r.GetDouble(6), CreatedAt = r.GetString(7) });
                }
                return list;
            });
        }

        public static void DeleteDgu(int id)
        {
            Execute(conn =>
            {
                using var cmd = conn.CreateCommand();
                cmd.CommandText = "DELETE FROM DGURecords WHERE Id = $id";
                cmd.Parameters.AddWithValue("$id", id);
                cmd.ExecuteNonQuery();
            });
        }

        // ═══════════════════════════════════════════════════
        // LAMINATION METHODS
        // ═══════════════════════════════════════════════════

        public static void SaveLamination(LaminationRecord r)
        {
            Execute(conn =>
            {
                using var cmd = conn.CreateCommand();
                cmd.CommandText = @"INSERT INTO LaminationRecords (Thickness1, Color1, Thickness2, Color2, PVBType, Result, CreatedAt, Cutting, Tempering, IncludeCutting, IncludeTempering)
VALUES ($t1,$c1,$t2,$c2,$p,$r,$d,$cut,$temp,$ic,$it)";
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

        // ═══════════════════════════════════════════════════
        // DAILY WORK METHODS
        // ═══════════════════════════════════════════════════

        public static void SaveDailyWork(DailyWork w)
        {
            Execute(conn =>
            {
                using var cmd = conn.CreateCommand();
                cmd.CommandText = @"INSERT INTO DailyWork (Date, UpdateDate, Company, PINumber, CustomerReference, TypeOfWork, ProductionStatus, DailyReportStatus, Qty, SQM, Status, Salesman, Color, Notes, CreatedDate)
VALUES ($d, $ud, $c, $pi, $cr, $t, $ps, $drs, $q, $s, $st, $sm, $cl, $n, $cd)";
                cmd.Parameters.AddWithValue("$d", w.Date.ToString("yyyy-MM-dd HH:mm:ss"));
                cmd.Parameters.AddWithValue("$ud", w.UpdateDate.ToString("yyyy-MM-dd HH:mm:ss"));
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
                if (!string.IsNullOrWhiteSpace(w.CustomerReference)) SaveCustomerReference(w.CustomerReference, w.Company);
            });
        }

        public static void UpdateDailyWork(DailyWork w)
        {
            Execute(conn =>
            {
                using var cmd = conn.CreateCommand();
                cmd.CommandText = @"UPDATE DailyWork SET Date = $d, UpdateDate = $ud, Company = $c, PINumber = $pi, CustomerReference = $cr, TypeOfWork = $t, ProductionStatus = $ps, DailyReportStatus = $drs, Qty = $q, SQM = $s, Status = $st, Salesman = $sm, Color = $cl, Notes = $n WHERE Id = $id";
                cmd.Parameters.AddWithValue("$id", w.Id);
                cmd.Parameters.AddWithValue("$d", w.Date.ToString("yyyy-MM-dd HH:mm:ss"));
                cmd.Parameters.AddWithValue("$ud", w.UpdateDate.ToString("yyyy-MM-dd HH:mm:ss"));
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

        // ═══════════════════════════════════════════════════
        // DELIVERY METHODS
        // ═══════════════════════════════════════════════════

        public static void SaveDelivery(Delivery d)
        {
            Execute(conn =>
            {
                using var cmd = conn.CreateCommand();
                cmd.CommandText = @"INSERT INTO Deliveries (SourceId, Date, Company, PINumber, CustomerReference, TypeOfWork, OrderQty, OrderSQM, Salesman, Status, Notes, CreatedDate, UpdatedDate)
VALUES ($sid, $d, $c, $pi, $cr, $t, $q, $s, $sm, $st, $n, $cd, $ud)";
                cmd.Parameters.AddWithValue("$sid", d.SourceId);
                cmd.Parameters.AddWithValue("$d", d.Date.ToString("yyyy-MM-dd HH:mm:ss"));
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
                cmd.CommandText = @"UPDATE Deliveries SET Date = $d, Company = $c, PINumber = $pi, CustomerReference = $cr, TypeOfWork = $t, OrderQty = $q, OrderSQM = $s, Salesman = $sm, Status = $st, Notes = $n, UpdatedDate = $ud WHERE Id = $id";
                cmd.Parameters.AddWithValue("$id", d.Id);
                cmd.Parameters.AddWithValue("$d", d.Date.ToString("yyyy-MM-dd HH:mm:ss"));
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

        // ═══════════════════════════════════════════════════
        // DELIVERY ITEMS
        // ═══════════════════════════════════════════════════

        public static void SaveDeliveryItem(DeliveryItem item)
        {
            Execute(conn =>
            {
                using var cmd = conn.CreateCommand();
                cmd.CommandText = @"INSERT INTO DeliveryItems (OrderId, DeliveryDate, DeliveredQty, DeliveredSQM, ReturnedQty, ReturnedSQM, Driver, Vehicle, Notes, CreatedDate)
VALUES ($oid, $d, $dq, $ds, $rq, $rs, $dr, $v, $n, $cd)";
                cmd.Parameters.AddWithValue("$oid", item.OrderId);
                cmd.Parameters.AddWithValue("$d", item.DeliveryDate.ToString("yyyy-MM-dd HH:mm:ss"));
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

        public static void UpdateDeliveryItem(DeliveryItem item)
        {
            Execute(conn =>
            {
                using var cmd = conn.CreateCommand();
                cmd.CommandText = @"UPDATE DeliveryItems SET DeliveryDate = $d, DeliveredQty = $dq, DeliveredSQM = $ds, ReturnedQty = $rq, ReturnedSQM = $rs, Driver = $dr, Vehicle = $v, Notes = $n WHERE Id = $id";
                cmd.Parameters.AddWithValue("$id", item.Id);
                cmd.Parameters.AddWithValue("$d", item.DeliveryDate.ToString("yyyy-MM-dd HH:mm:ss"));
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

        public static Delivery GetDeliveryById(int id)
        {
            return Execute(conn =>
            {
                using var cmd = conn.CreateCommand();
                cmd.CommandText = "SELECT * FROM Deliveries WHERE Id = $id";
                cmd.Parameters.AddWithValue("$id", id);
                using var r = cmd.ExecuteReader();
                if (r.Read())
                {
                    var delivery = new Delivery
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
                    };
                    delivery.DeliveryItems = new ObservableCollection<DeliveryItem>(GetDeliveryItems(delivery.Id));
                    return delivery;
                }
                return null;
            });
        }

        // ═══════════════════════════════════════════════════
        // SHEET STORE METHODS
        // ═══════════════════════════════════════════════════

        public static void SaveSheet(Sheet s)
        {
            Execute(conn =>
            {
                using var cmd = conn.CreateCommand();
                cmd.CommandText = @"INSERT INTO SheetStore (Category, Thickness, Color, ColorHex, Width, Height, SquareMeter, PurchasePrice, SellPrice, TotalStock, UsedSheets, BalanceSheets, IsActive, Supplier, SupplierName, Description, CreatedDate, LatestPurchaseDate)
VALUES ($cat, $th, $col, $hex, $w, $h, $sqm, $pp, $sp, $ts, $us, $bs, $act, $sup, $supn, $desc, $cd, $lpd)";
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
                cmd.CommandText = @"UPDATE SheetStore SET Category = $cat, Thickness = $th, Color = $col, ColorHex = $hex, Width = $w, Height = $h, SquareMeter = $sqm, PurchasePrice = $pp, SellPrice = $sp, TotalStock = $ts, UsedSheets = $us, BalanceSheets = $bs, IsActive = $act, Supplier = $sup, SupplierName = $supn, Description = $desc, LatestPurchaseDate = $lpd WHERE Id = $id";
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
                cmd.Parameters.AddWithValue("$sup", s.Supplier ?? "");
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
                        PurchasePrice = Convert.ToDecimal(r.GetDouble(8)),
                        SellPrice = Convert.ToDecimal(r.GetDouble(9)),
                        TotalStock = r.GetInt32(10),
                        UsedSheets = r.GetInt32(11),
                        BalanceSheets = r.GetInt32(12),
                        IsActive = r.GetInt32(13) == 1,
                        Supplier = r.IsDBNull(14) ? "" : r.GetString(14),
                        SupplierName = r.IsDBNull(15) ? "" : r.GetString(15),
                        Description = r.IsDBNull(16) ? "" : r.GetString(16),
                        CreatedDate = DateTime.TryParse(r.GetString(17), out var cd) ? cd : DateTime.Now,
                        LatestPurchaseDate = DateTime.TryParse(r.IsDBNull(18) ? "" : r.GetString(18), out var lpd) ? lpd : null
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

        public static void SaveSheetPurchase(SheetPurchase p)
        {
            Execute(conn =>
            {
                using var cmd = conn.CreateCommand();
                cmd.CommandText = @"INSERT INTO SheetPurchases (SheetId, Quantity, UnitPrice, Supplier, PurchasedOn, Notes, CreatedAt) VALUES ($sid, $q, $up, $sup, $pd, $nt, $cd)";
                cmd.Parameters.AddWithValue("$sid", p.SheetId);
                cmd.Parameters.AddWithValue("$q", p.Quantity);
                cmd.Parameters.AddWithValue("$up", p.UnitPrice);
                cmd.Parameters.AddWithValue("$sup", p.Supplier ?? "");
                cmd.Parameters.AddWithValue("$pd", p.PurchasedOn.ToString("yyyy-MM-dd HH:mm:ss"));
                cmd.Parameters.AddWithValue("$nt", p.Notes ?? "");
                cmd.Parameters.AddWithValue("$cd", p.CreatedAt.ToString("yyyy-MM-dd HH:mm"));
                cmd.ExecuteNonQuery();
                UpdateSheetStock(p.SheetId, p.Quantity);
            });
        }

        private static void UpdateSheetStock(int sheetId, int additionalQty)
        {
            Execute(conn =>
            {
                using var cmd = conn.CreateCommand();
                cmd.CommandText = @"UPDATE SheetStore SET TotalStock = TotalStock + $qty, BalanceSheets = BalanceSheets + $qty, LatestPurchaseDate = $date WHERE Id = $id";
                cmd.Parameters.AddWithValue("$id", sheetId);
                cmd.Parameters.AddWithValue("$qty", additionalQty);
                cmd.Parameters.AddWithValue("$date", DateTime.Now.ToString("yyyy-MM-dd HH:mm"));
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
                        UnitPrice = Convert.ToDecimal(r.GetDouble(3)),
                        Supplier = r.IsDBNull(4) ? "" : r.GetString(4),
                        PurchasedOn = DateTime.TryParse(r.GetString(5), out var pd) ? pd : DateTime.Today,
                        Notes = r.IsDBNull(6) ? "" : r.GetString(6),
                        CreatedAt = DateTime.TryParse(r.GetString(7), out var cd) ? cd : DateTime.Now
                    });
                }
                return list;
            });
        }

        public static void SaveSheetUsage(SheetUsage u)
        {
            Execute(conn =>
            {
                using var cmd = conn.CreateCommand();
                cmd.CommandText = @"INSERT INTO SheetUsages (SheetId, Quantity, Reason, UsedOn, CreatedAt) VALUES ($sid, $q, $rs, $ud, $cd)";
                cmd.Parameters.AddWithValue("$sid", u.SheetId);
                cmd.Parameters.AddWithValue("$q", u.Quantity);
                cmd.Parameters.AddWithValue("$rs", u.Reason ?? "");
                cmd.Parameters.AddWithValue("$ud", u.UsedOn.ToString("yyyy-MM-dd HH:mm:ss"));
                cmd.Parameters.AddWithValue("$cd", u.CreatedAt.ToString("yyyy-MM-dd HH:mm"));
                cmd.ExecuteNonQuery();
                DeductSheetStock(u.SheetId, u.Quantity);
            });
        }

        private static void DeductSheetStock(int sheetId, int usedQty)
        {
            Execute(conn =>
            {
                using var cmd = conn.CreateCommand();
                cmd.CommandText = @"UPDATE SheetStore SET UsedSheets = UsedSheets + $qty, BalanceSheets = BalanceSheets - $qty WHERE Id = $id AND BalanceSheets >= $qty";
                cmd.Parameters.AddWithValue("$id", sheetId);
                cmd.Parameters.AddWithValue("$qty", usedQty);
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

        // ═══════════════════════════════════════════════════
        // CUSTOMER REFERENCE & NOTES
        // ═══════════════════════════════════════════════════

        public static void SaveCustomerReference(string customerRef, string company)
        {
            if (string.IsNullOrWhiteSpace(customerRef)) return;
            Execute(conn =>
            {
                using var cmd = conn.CreateCommand();
                cmd.CommandText = @"INSERT INTO CustomerReferences (CustomerReference, Company, CreatedAt) VALUES ($ref, $company, $created) ON CONFLICT(CustomerReference) DO UPDATE SET Company = $company";
                cmd.Parameters.AddWithValue("$ref", customerRef);
                cmd.Parameters.AddWithValue("$company", company ?? "");
                cmd.Parameters.AddWithValue("$created", DateTime.Now.ToString("yyyy-MM-dd HH:mm"));
                cmd.ExecuteNonQuery();
            });
        }

        public static List<CustomerRefItem> GetAllCustomerReferences()
        {
            return Execute(conn =>
            {
                var list = new List<CustomerRefItem>();
                using var cmd = conn.CreateCommand();
                cmd.CommandText = "SELECT Id, CustomerReference, Company FROM CustomerReferences ORDER BY CustomerReference";
                using var r = cmd.ExecuteReader();
                while (r.Read())
                {
                    list.Add(new CustomerRefItem
                    {
                        Id = r.GetInt32(0),
                        CustomerReference = r.GetString(1),
                        Company = r.IsDBNull(2) ? "" : r.GetString(2)
                    });
                }
                return list;
            });
        }

        public static string GetCompanyByCustomerRef(string customerRef)
        {
            if (string.IsNullOrWhiteSpace(customerRef)) return "";
            return Execute(conn =>
            {
                using var cmd = conn.CreateCommand();
                cmd.CommandText = "SELECT Company FROM CustomerReferences WHERE CustomerReference = $ref";
                cmd.Parameters.AddWithValue("$ref", customerRef);
                var result = cmd.ExecuteScalar();
                return result?.ToString() ?? "";
            });
        }

        public static void SaveNoteSuggestion(string note)
        {
            if (string.IsNullOrWhiteSpace(note)) return;
            Execute(conn =>
            {
                using var cmd = conn.CreateCommand();
                cmd.CommandText = @"INSERT INTO NotesSuggestions (Note, UseCount, LastUsedAt) VALUES ($note, 1, $lastUsed) ON CONFLICT(Note) DO UPDATE SET UseCount = UseCount + 1, LastUsedAt = $lastUsed";
                cmd.Parameters.AddWithValue("$note", note);
                cmd.Parameters.AddWithValue("$lastUsed", DateTime.Now.ToString("yyyy-MM-dd HH:mm"));
                cmd.ExecuteNonQuery();
            });
        }

        public static List<string> GetAllNotes()
        {
            return Execute(conn =>
            {
                var list = new List<string>();
                using var cmd = conn.CreateCommand();
                cmd.CommandText = "SELECT Note FROM NotesSuggestions ORDER BY UseCount DESC, LastUsedAt DESC";
                using var r = cmd.ExecuteReader();
                while (r.Read()) list.Add(r.GetString(0));
                return list;
            });
        }

        public static bool IsDuplicateDailyWork(string customerRef, string piNumber, int excludeId = 0)
        {
            if (string.IsNullOrWhiteSpace(customerRef) || string.IsNullOrWhiteSpace(piNumber)) return false;
            return Execute(conn =>
            {
                using var cmd = conn.CreateCommand();
                if (excludeId > 0)
                {
                    cmd.CommandText = "SELECT COUNT(*) FROM DailyWork WHERE CustomerReference = $ref AND PINumber = $pi AND Id != $excludeId";
                    cmd.Parameters.AddWithValue("$excludeId", excludeId);
                }
                else
                {
                    cmd.CommandText = "SELECT COUNT(*) FROM DailyWork WHERE CustomerReference = $ref AND PINumber = $pi";
                }
                cmd.Parameters.AddWithValue("$ref", customerRef);
                cmd.Parameters.AddWithValue("$pi", piNumber);
                return Convert.ToInt32(cmd.ExecuteScalar()) > 0;
            });
        }

        // ═══════════════════════════════════════════════════
        // LOGGING & METRICS
        // ═══════════════════════════════════════════════════

        public static void LogCalculation(string moduleType, double sqm, string notes = "")
        {
            try
            {
                Execute(conn =>
                {
                    using var cmd = conn.CreateCommand();
                    cmd.CommandText = @"INSERT INTO CalculationLogs (ModuleType, SQM, Notes, CreatedDate) VALUES ($moduleType, $sqm, $notes, $createdDate)";
                    cmd.Parameters.AddWithValue("$moduleType", moduleType);
                    cmd.Parameters.AddWithValue("$sqm", sqm);
                    cmd.Parameters.AddWithValue("$notes", notes ?? "");
                    cmd.Parameters.AddWithValue("$createdDate", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
                    cmd.ExecuteNonQuery();
                });
            }
            catch (Exception ex) { System.Diagnostics.Debug.WriteLine($"[LogCalc] {ex.Message}"); }
        }

        public static void LogMetric(string metricType, double value)
        {
            try
            {
                Execute(conn =>
                {
                    using var cmd = conn.CreateCommand();
                    cmd.CommandText = @"INSERT INTO SystemMetrics (MetricType, MetricValue, CreatedDate) VALUES ($metricType, $value, $createdDate)";
                    cmd.Parameters.AddWithValue("$metricType", metricType);
                    cmd.Parameters.AddWithValue("$value", value);
                    cmd.Parameters.AddWithValue("$createdDate", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
                    cmd.ExecuteNonQuery();
                });
            }
            catch (Exception ex) { System.Diagnostics.Debug.WriteLine($"[LogMetric] {ex.Message}"); }
        }

        public static double GetTodayTotalSGU() => GetMetricSum("SGU", "today");
        public static double GetTodayTotalDGU() => GetMetricSum("DGU", "today");
        public static double GetTodayTotalLamination() => GetMetricSum("Lamination", "today");

        private static double GetMetricSum(string moduleType, string period)
        {
            try
            {
                return Execute(conn =>
                {
                    string dateFilter = period switch
                    {
                        "today" => "DATE(CreatedDate) = DATE('now', 'localtime')",
                        "week" => "DATE(CreatedDate) >= DATE('now', '-7 days')",
                        "month" => "DATE(CreatedDate) >= DATE('now', '-30 days')",
                        _ => "1=1"
                    };

                    using var cmd = conn.CreateCommand();
                    cmd.CommandText = $"SELECT COALESCE(SUM(SQM), 0) FROM CalculationLogs WHERE ModuleType = $moduleType AND {dateFilter}";
                    cmd.Parameters.AddWithValue("$moduleType", moduleType);
                    return Convert.ToDouble(cmd.ExecuteScalar());
                });
            }
            catch { return 0; }
        }

        public static int GetTodayCalculationCount(string moduleType)
        {
            try
            {
                return Execute(conn =>
                {
                    using var cmd = conn.CreateCommand();
                    cmd.CommandText = "SELECT COUNT(*) FROM CalculationLogs WHERE ModuleType = $moduleType AND DATE(CreatedDate) = DATE('now', 'localtime')";
                    cmd.Parameters.AddWithValue("$moduleType", moduleType);
                    return Convert.ToInt32(cmd.ExecuteScalar());
                });
            }
            catch { return 0; }
        }

        public static int GetCalculationCount(string moduleType, DateTime since, DateTime until)
        {
            try
            {
                return Execute(conn =>
                {
                    using var cmd = conn.CreateCommand();
                    cmd.CommandText = "SELECT COUNT(*) FROM CalculationLogs WHERE ModuleType = $moduleType AND datetime(CreatedDate) BETWEEN datetime($since) AND datetime($until)";
                    cmd.Parameters.AddWithValue("$moduleType", moduleType);
                    cmd.Parameters.AddWithValue("$since", since.ToString("yyyy-MM-dd HH:mm:ss"));
                    cmd.Parameters.AddWithValue("$until", until.ToString("yyyy-MM-dd HH:mm:ss"));
                    return Convert.ToInt32(cmd.ExecuteScalar());
                });
            }
            catch { return 0; }
        }

        public static DateTime GetModuleLastActivity(string moduleType)
        {
            try
            {
                return Execute(conn =>
                {
                    using var cmd = conn.CreateCommand();
                    cmd.CommandText = "SELECT MAX(datetime(CreatedDate)) FROM CalculationLogs WHERE ModuleType = $moduleType";
                    cmd.Parameters.AddWithValue("$moduleType", moduleType);
                    var result = cmd.ExecuteScalar();
                    if (result != null && result != DBNull.Value)
                    {
                        DateTime.TryParse(result.ToString(), out var date);
                        return date;
                    }
                    return DateTime.MinValue;
                });
            }
            catch { return DateTime.MinValue; }
        }

        public static List<double> GetUptimeRecords()
        {
            try
            {
                return Execute(conn =>
                {
                    var results = new List<double>();
                    using var cmd = conn.CreateCommand();
                    cmd.CommandText = "SELECT MetricValue FROM SystemMetrics WHERE MetricType = 'Uptime' AND datetime(CreatedDate) >= datetime('now', '-24 hours') ORDER BY CreatedDate DESC LIMIT 100";
                    using var r = cmd.ExecuteReader();
                    while (r.Read()) results.Add(Convert.ToDouble(r["MetricValue"]));
                    return results;
                });
            }
            catch { return new List<double> { 99.8 }; }
        }

        // ═══════════════════════════════════════════════════
        // IMPORT SESSIONS
        // ═══════════════════════════════════════════════════

        public static int CreateImportSession()
        {
            try
            {
                return Execute(conn =>
                {
                    using var cmd = conn.CreateCommand();
                    cmd.CommandText = "INSERT INTO ImportSessions (SessionDateTime, ImportedCount, UpdatedCount, SkippedCount) VALUES ($sessionDateTime, 0, 0, 0); SELECT last_insert_rowid();";
                    cmd.Parameters.AddWithValue("$sessionDateTime", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
                    return Convert.ToInt32(cmd.ExecuteScalar());
                });
            }
            catch { return 0; }
        }

        public static void UpdateImportSession(int sessionId, int imported, int updated, int skipped)
        {
            try
            {
                Execute(conn =>
                {
                    using var cmd = conn.CreateCommand();
                    cmd.CommandText = "UPDATE ImportSessions SET ImportedCount = $imported, UpdatedCount = $updated, SkippedCount = $skipped WHERE Id = $sessionId";
                    cmd.Parameters.AddWithValue("$sessionId", sessionId);
                    cmd.Parameters.AddWithValue("$imported", imported);
                    cmd.Parameters.AddWithValue("$updated", updated);
                    cmd.Parameters.AddWithValue("$skipped", skipped);
                    cmd.ExecuteNonQuery();
                });
            }
            catch (Exception ex) { System.Diagnostics.Debug.WriteLine($"[UpdateSession] {ex.Message}"); }
        }

        public static void SaveImportLog(int sessionId, string piNumber, string company, string changesJson)
        {
            try
            {
                Execute(conn =>
                {
                    using var cmd = conn.CreateCommand();
                    cmd.CommandText = @"INSERT INTO ImportLogs (SessionId, ImportDateTime, PINumber, Company, ChangesJson) VALUES ($sessionId, $importDateTime, $piNumber, $company, $changesJson)";
                    cmd.Parameters.AddWithValue("$sessionId", sessionId);
                    cmd.Parameters.AddWithValue("$importDateTime", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
                    cmd.Parameters.AddWithValue("$piNumber", piNumber ?? "");
                    cmd.Parameters.AddWithValue("$company", company ?? "");
                    cmd.Parameters.AddWithValue("$changesJson", changesJson ?? "");
                    cmd.ExecuteNonQuery();
                });
            }
            catch (Exception ex) { System.Diagnostics.Debug.WriteLine($"[SaveLog] {ex.Message}"); }
        }

        public static List<ImportSessionInfo> GetImportSessions()
        {
            try
            {
                return Execute(conn =>
                {
                    var list = new List<ImportSessionInfo>();
                    using var cmd = conn.CreateCommand();
                    cmd.CommandText = "SELECT Id, SessionDateTime, ImportedCount, UpdatedCount, SkippedCount, Notes FROM ImportSessions ORDER BY Id DESC LIMIT 50";
                    using var r = cmd.ExecuteReader();
                    while (r.Read())
                    {
                        list.Add(new ImportSessionInfo
                        {
                            Id = r.GetInt32(0),
                            SessionDateTime = DateTime.TryParse(r.GetString(1), out var dt) ? dt : DateTime.Now,
                            ImportedCount = r.GetInt32(2),
                            UpdatedCount = r.GetInt32(3),
                            SkippedCount = r.GetInt32(4),
                            Notes = r.IsDBNull(5) ? "" : r.GetString(5)
                        });
                    }
                    return list;
                });
            }
            catch { return new List<ImportSessionInfo>(); }
        }

        public static List<ImportLogInfo> GetImportLogs(int sessionId)
        {
            try
            {
                return Execute(conn =>
                {
                    var list = new List<ImportLogInfo>();
                    using var cmd = conn.CreateCommand();
                    cmd.CommandText = "SELECT Id, ImportDateTime, PINumber, Company, ChangesJson FROM ImportLogs WHERE SessionId = $sessionId ORDER BY Id DESC";
                    cmd.Parameters.AddWithValue("$sessionId", sessionId);
                    using var r = cmd.ExecuteReader();
                    while (r.Read())
                    {
                        list.Add(new ImportLogInfo
                        {
                            Id = r.GetInt32(0),
                            ImportDateTime = DateTime.TryParse(r.GetString(1), out var dt) ? dt : DateTime.Now,
                            PINumber = r.IsDBNull(2) ? "" : r.GetString(2),
                            Company = r.IsDBNull(3) ? "" : r.GetString(3),
                            ChangesJson = r.IsDBNull(4) ? "" : r.GetString(4)
                        });
                    }
                    return list;
                });
            }
            catch { return new List<ImportLogInfo>(); }
        }

        // ═══════════════════════════════════════════════════
        // CLEANUP & MAINTENANCE
        // ═══════════════════════════════════════════════════

        public static void CleanupOldLogs(int keepDays = 30)
        {
            try
            {
                Execute(conn =>
                {
                    using var cmd1 = conn.CreateCommand();
                    cmd1.CommandText = "DELETE FROM CalculationLogs WHERE datetime(CreatedDate) < datetime('now', '-$days days')";
                    cmd1.Parameters.AddWithValue("$days", keepDays);
                    cmd1.ExecuteNonQuery();

                    using var cmd2 = conn.CreateCommand();
                    cmd2.CommandText = "DELETE FROM SystemMetrics WHERE datetime(CreatedDate) < datetime('now', '-$days days')";
                    cmd2.Parameters.AddWithValue("$days", keepDays);
                    cmd2.ExecuteNonQuery();

                    using var cmd3 = conn.CreateCommand();
                    cmd3.CommandText = "DELETE FROM ImportLogs WHERE datetime(ImportDateTime) < datetime('now', '-$days days')";
                    cmd3.Parameters.AddWithValue("$days", keepDays);
                    cmd3.ExecuteNonQuery();
                });
            }
            catch (Exception ex) { System.Diagnostics.Debug.WriteLine($"[Cleanup] {ex.Message}"); }
        }

        public static string GetDatabaseStats()
        {
            try
            {
                return Execute(conn =>
                {
                    var stats = new StringBuilder();
                    string[] tables = { "SGURecords", "DGURecords", "LaminationRecords", "DailyWork", "Deliveries", "DeliveryItems", "SheetStore", "CalculationLogs", "SystemMetrics", "SGUHistory", "CustomerReferences", "NotesSuggestions", "ProformaInvoices", "JobOrders", "DeliveryOrders", "TaxInvoices" };

                    foreach (var table in tables)
                    {
                        using var cmd = conn.CreateCommand();
                        cmd.CommandText = $"SELECT COUNT(*) FROM {table}";
                        var count = Convert.ToInt32(cmd.ExecuteScalar());
                        stats.AppendLine($"{table}: {count} records");
                    }

                    var dbSize = new FileInfo(DbPath).Length;
                    stats.AppendLine($"Database size: {dbSize / (1024.0 * 1024.0):F2} MB");
                    return stats.ToString();
                });
            }
            catch (Exception ex) { return $"Error: {ex.Message}"; }
        }

        // ═══════════════════════════════════════════════════
        // OPTIONS DROPDOWNS
        // ═══════════════════════════════════════════════════

        public static List<string> GetAllTypeOfWorkOptions()
        {
            try
            {
                return Execute(conn =>
                {
                    var list = new List<string>();
                    using (var cmd = conn.CreateCommand())
                    {
                        cmd.CommandText = "SELECT DISTINCT TypeOfWork FROM Deliveries WHERE TypeOfWork IS NOT NULL AND TypeOfWork != '' ORDER BY TypeOfWork";
                        using var r = cmd.ExecuteReader();
                        while (r.Read()) { var v = r.GetString(0); if (!list.Contains(v)) list.Add(v); }
                    }
                    using (var cmd = conn.CreateCommand())
                    {
                        cmd.CommandText = "SELECT DISTINCT TypeOfWork FROM DailyWork WHERE TypeOfWork IS NOT NULL AND TypeOfWork != '' ORDER BY TypeOfWork";
                        using var r = cmd.ExecuteReader();
                        while (r.Read()) { var v = r.GetString(0); if (!list.Contains(v)) list.Add(v); }
                    }
                    return list;
                });
            }
            catch { return new List<string>(); }
        }

        public static List<string> GetAllDeliveryStatusOptions()
        {
            try
            {
                return Execute(conn =>
                {
                    var list = new List<string>();
                    using (var cmd = conn.CreateCommand())
                    {
                        cmd.CommandText = "SELECT DISTINCT Status FROM Deliveries WHERE Status IS NOT NULL AND Status != '' ORDER BY Status";
                        using var r = cmd.ExecuteReader();
                        while (r.Read()) { var v = r.GetString(0); if (!list.Contains(v)) list.Add(v); }
                    }
                    var defaults = new[] { "Pending", "Partially Delivered", "Completed", "Cancelled" };
                    foreach (var s in defaults) if (!list.Contains(s)) list.Add(s);
                    return list;
                });
            }
            catch { return new List<string> { "Pending", "Partially Delivered", "Completed", "Cancelled" }; }
        }

        public static List<string> GetAllSalesmanOptions()
        {
            try
            {
                return Execute(conn =>
                {
                    var list = new List<string>();
                    using (var cmd = conn.CreateCommand())
                    {
                        cmd.CommandText = "SELECT DISTINCT Salesman FROM Deliveries WHERE Salesman IS NOT NULL AND Salesman != '' ORDER BY Salesman";
                        using var r = cmd.ExecuteReader();
                        while (r.Read()) { var v = r.GetString(0); if (!list.Contains(v)) list.Add(v); }
                    }
                    using (var cmd = conn.CreateCommand())
                    {
                        cmd.CommandText = "SELECT DISTINCT Salesman FROM DailyWork WHERE Salesman IS NOT NULL AND Salesman != '' ORDER BY Salesman";
                        using var r = cmd.ExecuteReader();
                        while (r.Read()) { var v = r.GetString(0); if (!list.Contains(v)) list.Add(v); }
                    }
                    return list;
                });
            }
            catch { return new List<string>(); }
        }

        public static List<string> GetAllCompanyOptions()
        {
            try
            {
                return Execute(conn =>
                {
                    var list = new List<string>();
                    using (var cmd = conn.CreateCommand())
                    {
                        cmd.CommandText = "SELECT DISTINCT Company FROM Deliveries WHERE Company IS NOT NULL AND Company != '' ORDER BY Company";
                        using var r = cmd.ExecuteReader();
                        while (r.Read()) { var v = r.GetString(0); if (!list.Contains(v)) list.Add(v); }
                    }
                    using (var cmd = conn.CreateCommand())
                    {
                        cmd.CommandText = "SELECT DISTINCT Company FROM DailyWork WHERE Company IS NOT NULL AND Company != '' ORDER BY Company";
                        using var r = cmd.ExecuteReader();
                        while (r.Read()) { var v = r.GetString(0); if (!list.Contains(v)) list.Add(v); }
                    }
                    return list;
                });
            }
            catch { return new List<string>(); }
        }

        public static List<string> GetAllDriverOptions()
        {
            try
            {
                return Execute(conn =>
                {
                    var list = new List<string>();
                    using var cmd = conn.CreateCommand();
                    cmd.CommandText = "SELECT DISTINCT Driver FROM DeliveryItems WHERE Driver IS NOT NULL AND Driver != '' ORDER BY Driver";
                    using var r = cmd.ExecuteReader();
                    while (r.Read()) { var v = r.GetString(0); if (!list.Contains(v)) list.Add(v); }
                    return list;
                });
            }
            catch { return new List<string>(); }
        }

        public static List<string> GetAllVehicleOptions()
        {
            try
            {
                return Execute(conn =>
                {
                    var list = new List<string>();
                    using var cmd = conn.CreateCommand();
                    cmd.CommandText = "SELECT DISTINCT Vehicle FROM DeliveryItems WHERE Vehicle IS NOT NULL AND Vehicle != '' ORDER BY Vehicle";
                    using var r = cmd.ExecuteReader();
                    while (r.Read()) { var v = r.GetString(0); if (!list.Contains(v)) list.Add(v); }
                    return list;
                });
            }
            catch { return new List<string>(); }
        }

        public static List<string> GetAllColorOptions()
        {
            try
            {
                return Execute(conn =>
                {
                    var list = new List<string>();
                    using (var cmd = conn.CreateCommand())
                    {
                        cmd.CommandText = "SELECT DISTINCT Color FROM Deliveries WHERE Color IS NOT NULL AND Color != '' ORDER BY Color";
                        using var r = cmd.ExecuteReader();
                        while (r.Read()) { var v = r.GetString(0); if (!list.Contains(v)) list.Add(v); }
                    }
                    using (var cmd = conn.CreateCommand())
                    {
                        cmd.CommandText = "SELECT DISTINCT Color FROM DailyWork WHERE Color IS NOT NULL AND Color != '' ORDER BY Color";
                        using var r = cmd.ExecuteReader();
                        while (r.Read()) { var v = r.GetString(0); if (!list.Contains(v)) list.Add(v); }
                    }
                    return list;
                });
            }
            catch { return new List<string>(); }
        }

        // ═══════════════════════════════════════════════════
        // PROFROMA INVOICE METHODS
        // ═══════════════════════════════════════════════════

        public static void SaveProformaInvoice(ProformaInvoiceModel pi)
        {
            Execute(conn =>
            {
                using var checkCmd = conn.CreateCommand();
                checkCmd.CommandText = "SELECT Id FROM ProformaInvoices WHERE PINumber = $pi";
                checkCmd.Parameters.AddWithValue("$pi", pi.InvoiceNo);
                var existingId = checkCmd.ExecuteScalar();

                if (existingId != null)
                {
                    pi.Id = Convert.ToInt32(existingId);
                    using var cmd = conn.CreateCommand();
                    cmd.CommandText = @"UPDATE ProformaInvoices SET ClientName = $cn, ClientTRN = $ctr, ClientAddress = $ca, ProjectName = $pn, ProjectLocation = $pl, LPONumber = $lp, Attention = $att, ContactNo = $con, PIDate = $pd, ValidUntil = $vu, Status = $st, TotalAmount = $ta, VATPercent = $vp, VATAmount = $va, NetAmount = $na, CompanyName = $compName, CompanyTRN = $compTRN, CompanyLocation = $compLoc, CompanyPhone = $compPhone, Notes = $nt, Salesman = $sm, JobOrderRef = $joRef, CustomerReference = $custRef, ProjectNo = $projNo, Color = $clr, UpdatedDate = $ud WHERE Id = $id";
                    cmd.Parameters.AddWithValue("$id", pi.Id);
                    cmd.Parameters.AddWithValue("$cn", pi.CustomerName ?? "");
                    cmd.Parameters.AddWithValue("$ctr", pi.CustomerTRN ?? "");
                    cmd.Parameters.AddWithValue("$ca", pi.CustomerAddress ?? "");
                    cmd.Parameters.AddWithValue("$pn", pi.ProjectName ?? "");
                    cmd.Parameters.AddWithValue("$pl", pi.ProjectLocation ?? "");
                    cmd.Parameters.AddWithValue("$lp", pi.LPONo ?? "");
                    cmd.Parameters.AddWithValue("$att", pi.AttentionName ?? "");
                    cmd.Parameters.AddWithValue("$con", pi.ContactNo ?? "");
                    cmd.Parameters.AddWithValue("$pd", pi.InvoiceDate.ToString("yyyy-MM-dd HH:mm"));
                    cmd.Parameters.AddWithValue("$vu", pi.ValidUntil.ToString("yyyy-MM-dd HH:mm"));
                    cmd.Parameters.AddWithValue("$st", pi.Status ?? "Draft");
                    cmd.Parameters.AddWithValue("$ta", pi.TotalAmount);
                    cmd.Parameters.AddWithValue("$vp", 5);
                    cmd.Parameters.AddWithValue("$va", pi.VATAmount);
                    cmd.Parameters.AddWithValue("$na", pi.NetAmount);
                    cmd.Parameters.AddWithValue("$compName", pi.CompanyName ?? "");
                    cmd.Parameters.AddWithValue("$compTRN", pi.CompanyTRN ?? "");
                    cmd.Parameters.AddWithValue("$compLoc", pi.CompanyLocation ?? "");
                    cmd.Parameters.AddWithValue("$compPhone", pi.CompanyPhone ?? "");
                    cmd.Parameters.AddWithValue("$nt", pi.Notes ?? "");
                    cmd.Parameters.AddWithValue("$sm", pi.Salesman ?? "");
                    cmd.Parameters.AddWithValue("$joRef", pi.JobOrderRef ?? "");
                    cmd.Parameters.AddWithValue("$custRef", pi.CustomerReference ?? "");
                    cmd.Parameters.AddWithValue("$projNo", pi.ProjectNo ?? "");
                    cmd.Parameters.AddWithValue("$clr", pi.Color ?? "");
                    cmd.Parameters.AddWithValue("$ud", DateTime.Now.ToString("yyyy-MM-dd HH:mm"));
                    cmd.ExecuteNonQuery();

                    using var delCmd = conn.CreateCommand();
                    delCmd.CommandText = "DELETE FROM ProformaInvoiceItems WHERE ProformaInvoiceId = $id";
                    delCmd.Parameters.AddWithValue("$id", pi.Id);
                    delCmd.ExecuteNonQuery();
                }
                else
                {
                    using var cmd = conn.CreateCommand();
                    cmd.CommandText = @"INSERT INTO ProformaInvoices (PINumber, ClientName, ClientTRN, ClientAddress, ProjectName, ProjectLocation, LPONumber, Attention, ContactNo, PIDate, ValidUntil, Status, TotalAmount, VATPercent, VATAmount, NetAmount, CompanyName, CompanyTRN, CompanyLocation, CompanyPhone, Notes, Salesman, JobOrderRef, CustomerReference, ProjectNo, Color, CreatedDate, UpdatedDate)
                    VALUES ($pi, $cn, $ctr, $ca, $pn, $pl, $lp, $att, $con, $pd, $vu, $st, $ta, $vp, $va, $na, $compName, $compTRN, $compLoc, $compPhone, $nt, $sm, $joRef, $custRef, $projNo, $clr, $cd, $ud)";
                    cmd.Parameters.AddWithValue("$pi", pi.InvoiceNo);
                    cmd.Parameters.AddWithValue("$cn", pi.CustomerName ?? "");
                    cmd.Parameters.AddWithValue("$ctr", pi.CustomerTRN ?? "");
                    cmd.Parameters.AddWithValue("$ca", pi.CustomerAddress ?? "");
                    cmd.Parameters.AddWithValue("$pn", pi.ProjectName ?? "");
                    cmd.Parameters.AddWithValue("$pl", pi.ProjectLocation ?? "");
                    cmd.Parameters.AddWithValue("$lp", pi.LPONo ?? "");
                    cmd.Parameters.AddWithValue("$att", pi.AttentionName ?? "");
                    cmd.Parameters.AddWithValue("$con", pi.ContactNo ?? "");
                    cmd.Parameters.AddWithValue("$pd", pi.InvoiceDate.ToString("yyyy-MM-dd HH:mm"));
                    cmd.Parameters.AddWithValue("$vu", pi.ValidUntil.ToString("yyyy-MM-dd HH:mm"));
                    cmd.Parameters.AddWithValue("$st", pi.Status ?? "Draft");
                    cmd.Parameters.AddWithValue("$ta", pi.TotalAmount);
                    cmd.Parameters.AddWithValue("$vp", 5);
                    cmd.Parameters.AddWithValue("$va", pi.VATAmount);
                    cmd.Parameters.AddWithValue("$na", pi.NetAmount);
                    cmd.Parameters.AddWithValue("$compName", pi.CompanyName ?? "");
                    cmd.Parameters.AddWithValue("$compTRN", pi.CompanyTRN ?? "");
                    cmd.Parameters.AddWithValue("$compLoc", pi.CompanyLocation ?? "");
                    cmd.Parameters.AddWithValue("$compPhone", pi.CompanyPhone ?? "");
                    cmd.Parameters.AddWithValue("$nt", pi.Notes ?? "");
                    cmd.Parameters.AddWithValue("$sm", pi.Salesman ?? "");
                    cmd.Parameters.AddWithValue("$joRef", pi.JobOrderRef ?? "");
                    cmd.Parameters.AddWithValue("$custRef", pi.CustomerReference ?? "");
                    cmd.Parameters.AddWithValue("$projNo", pi.ProjectNo ?? "");
                    cmd.Parameters.AddWithValue("$clr", pi.Color ?? "");
                    cmd.Parameters.AddWithValue("$cd", DateTime.Now.ToString("yyyy-MM-dd HH:mm"));
                    cmd.Parameters.AddWithValue("$ud", DateTime.Now.ToString("yyyy-MM-dd HH:mm"));
                    cmd.ExecuteNonQuery();

                    using var idCmd = conn.CreateCommand();
                    idCmd.CommandText = "SELECT last_insert_rowid()";
                    pi.Id = Convert.ToInt32(idCmd.ExecuteScalar());
                }

                foreach (var item in pi.Items)
                {
                    using var itemCmd = conn.CreateCommand();
                    itemCmd.CommandText = @"INSERT INTO ProformaInvoiceItems (ProformaInvoiceId, SrNo, GlassRef, Width1, Height1, Width2, Height2, Qty, SQM, TotalSQM, Price, TotalPrice, SurchargePercent, SurchargeThreshold)
                    VALUES ($piId, $sr, $gr, $w1, $h1, $w2, $h2, $q, $sqm, $tsqm, $pr, $tp, $sp, $st)";
                    itemCmd.Parameters.AddWithValue("$piId", pi.Id);
                    itemCmd.Parameters.AddWithValue("$sr", item.SrNo);
                    itemCmd.Parameters.AddWithValue("$gr", item.GlassRef ?? "");
                    itemCmd.Parameters.AddWithValue("$w1", item.Width1);
                    itemCmd.Parameters.AddWithValue("$h1", item.Height1);
                    itemCmd.Parameters.AddWithValue("$w2", item.Width2);
                    itemCmd.Parameters.AddWithValue("$h2", item.Height2);
                    itemCmd.Parameters.AddWithValue("$q", item.Qty);
                    itemCmd.Parameters.AddWithValue("$sqm", item.SQM);
                    itemCmd.Parameters.AddWithValue("$tsqm", item.TotalSQM);
                    itemCmd.Parameters.AddWithValue("$pr", item.Price);
                    itemCmd.Parameters.AddWithValue("$tp", item.TotalPrice);
                    itemCmd.Parameters.AddWithValue("$sp", item.SurchargePercent);
                    itemCmd.Parameters.AddWithValue("$st", item.SurchargeThreshold);
                    itemCmd.ExecuteNonQuery();
                }
            });
        }

        public static List<ProformaInvoiceModel> GetAllProformaInvoices()
        {
            return Execute(conn =>
            {
                var list = new List<ProformaInvoiceModel>();
                using var cmd = conn.CreateCommand();
                cmd.CommandText = "SELECT * FROM ProformaInvoices ORDER BY Id DESC";
                using var r = cmd.ExecuteReader();
                while (r.Read())
                {
                    var pi = new ProformaInvoiceModel
                    {
                        Id = r.GetInt32(0),
                        InvoiceNo = r.IsDBNull(1) ? "" : r.GetString(1),
                        CustomerName = r.IsDBNull(2) ? "" : r.GetString(2),
                        CustomerTRN = r.IsDBNull(3) ? "" : r.GetString(3),
                        CustomerAddress = r.IsDBNull(4) ? "" : r.GetString(4),
                        ProjectName = r.IsDBNull(5) ? "" : r.GetString(5),
                        ProjectLocation = r.IsDBNull(6) ? "" : r.GetString(6),
                        LPONo = r.IsDBNull(7) ? "" : r.GetString(7),
                        AttentionName = r.IsDBNull(8) ? "" : r.GetString(8),
                        ContactNo = r.IsDBNull(9) ? "" : r.GetString(9),
                        InvoiceDate = DateTime.TryParse(r.IsDBNull(10) ? "" : r.GetString(10), out var pd) ? pd : DateTime.Now,
                        ValidUntil = DateTime.TryParse(r.IsDBNull(11) ? "" : r.GetString(11), out var vu) ? vu : DateTime.Now.AddDays(30),
                        Status = r.IsDBNull(12) ? "Draft" : r.GetString(12),
                        TotalAmount = r.GetDouble(13),
                        VATAmount = r.GetDouble(15),
                        NetAmount = r.GetDouble(16),
                        CompanyName = r.IsDBNull(17) ? "" : r.GetString(17),
                        CompanyTRN = r.IsDBNull(18) ? "" : r.GetString(18),
                        CompanyLocation = r.IsDBNull(19) ? "" : r.GetString(19),
                        CompanyPhone = r.IsDBNull(20) ? "" : r.GetString(20),
                        Notes = r.IsDBNull(21) ? "" : r.GetString(21),
                        Salesman = r.FieldCount > 22 && !r.IsDBNull(22) ? r.GetString(22) : "",
                        JobOrderRef = r.FieldCount > 23 && !r.IsDBNull(23) ? r.GetString(23) : "",
                        CustomerReference = r.FieldCount > 24 && !r.IsDBNull(24) ? r.GetString(24) : "",
                        ProjectNo = r.FieldCount > 25 && !r.IsDBNull(25) ? r.GetString(25) : "",
                        Color = r.FieldCount > 26 && !r.IsDBNull(26) ? r.GetString(26) : ""
                    };
                    pi.Items = GetProformaInvoiceItems(pi.Id, conn);
                    list.Add(pi);
                }
                return list;
            });
        }

        private static List<ProformaInvoiceItemModel> GetProformaInvoiceItems(int piId, SqliteConnection conn)
        {
            var items = new List<ProformaInvoiceItemModel>();
            using var cmd = conn.CreateCommand();
            cmd.CommandText = "SELECT * FROM ProformaInvoiceItems WHERE ProformaInvoiceId = $id ORDER BY SrNo";
            cmd.Parameters.AddWithValue("$id", piId);
            using var r = cmd.ExecuteReader();
            while (r.Read())
            {
                items.Add(new ProformaInvoiceItemModel
                {
                    Id = r.GetInt32(0),
                    SrNo = r.GetInt32(2),
                    GlassRef = r.IsDBNull(3) ? "" : r.GetString(3),
                    Width1 = r.GetDouble(4),
                    Height1 = r.GetDouble(5),
                    Width2 = r.GetDouble(6),
                    Height2 = r.GetDouble(7),
                    Qty = r.GetInt32(8),
                    SQM = r.GetDouble(9),
                    TotalSQM = r.GetDouble(10),
                    Price = r.GetDouble(11),
                    TotalPrice = r.GetDouble(12),
                    SurchargePercent = r.GetDouble(13),
                    SurchargeThreshold = r.GetDouble(14)
                });
            }
            return items;
        }

        public static void DeleteProformaInvoice(int id)
        {
            Execute(conn =>
            {
                using var cmd = conn.CreateCommand();
                cmd.CommandText = "DELETE FROM ProformaInvoiceItems WHERE ProformaInvoiceId = $id";
                cmd.Parameters.AddWithValue("$id", id);
                cmd.ExecuteNonQuery();
                cmd.CommandText = "DELETE FROM ProformaInvoices WHERE Id = $id";
                cmd.ExecuteNonQuery();
            });
        }

        // ═══════════════════════════════════════════════════
        // JOB ORDER METHODS
        // ═══════════════════════════════════════════════════

        public static void SaveJobOrder(JobOrderModel jo)
        {
            Execute(conn =>
            {
                using var checkCmd = conn.CreateCommand();
                checkCmd.CommandText = "SELECT Id FROM JobOrders WHERE JONumber = $jo";
                checkCmd.Parameters.AddWithValue("$jo", jo.JONumber);
                var existingId = checkCmd.ExecuteScalar();

                if (existingId != null)
                {
                    jo.Id = Convert.ToInt32(existingId);
                    using var cmd = conn.CreateCommand();
                    cmd.CommandText = @"UPDATE JobOrders SET ProformaInvoiceId = $piId, ClientName = $cn, ClientTRN = $ctr, ClientAddress = $ca, ContactPerson = $cp, ContactNumber = $cno, ProjectName = $pn, ProjectLocation = $pl, LPONumber = $lp, JODate = $jd, RequiredDate = $rd, Status = $st, TotalQty = $tq, ReleasedQty = $rq, BalanceQty = $bq, TotalAmount = $ta, VATAmount = $va, DiscountAmount = $da, NetAmount = $na, Notes = $nt, SpecificationsJson = $specsJson, UpdatedDate = $ud WHERE Id = $id";
                    cmd.Parameters.AddWithValue("$id", jo.Id);
                    cmd.Parameters.AddWithValue("$piId", jo.ProformaInvoiceId > 0 ? jo.ProformaInvoiceId : DBNull.Value);
                    cmd.Parameters.AddWithValue("$cn", jo.ClientName ?? "");
                    cmd.Parameters.AddWithValue("$ctr", jo.ClientTRN ?? "");
                    cmd.Parameters.AddWithValue("$ca", jo.ClientAddress ?? "");
                    cmd.Parameters.AddWithValue("$cp", jo.ContactPerson ?? "");
                    cmd.Parameters.AddWithValue("$cno", jo.ContactNumber ?? "");
                    cmd.Parameters.AddWithValue("$pn", jo.ProjectName ?? "");
                    cmd.Parameters.AddWithValue("$pl", jo.ProjectLocation ?? "");
                    cmd.Parameters.AddWithValue("$lp", jo.LPONumber ?? "");
                    cmd.Parameters.AddWithValue("$jd", jo.JODate.ToString("yyyy-MM-dd HH:mm:ss"));
                    cmd.Parameters.AddWithValue("$rd", jo.RequiredDate.ToString("yyyy-MM-dd HH:mm:ss"));
                    cmd.Parameters.AddWithValue("$st", jo.Status ?? "Pending");
                    cmd.Parameters.AddWithValue("$tq", jo.TotalQty);
                    cmd.Parameters.AddWithValue("$rq", jo.ReleasedQty);
                    cmd.Parameters.AddWithValue("$bq", jo.BalanceQty);
                    cmd.Parameters.AddWithValue("$ta", jo.TotalAmount);
                    cmd.Parameters.AddWithValue("$va", jo.VATAmount);
                    cmd.Parameters.AddWithValue("$da", jo.DiscountAmount);
                    cmd.Parameters.AddWithValue("$na", jo.NetAmount);
                    cmd.Parameters.AddWithValue("$nt", jo.Notes ?? "");
                    cmd.Parameters.AddWithValue("$specsJson", jo.SpecificationsJson ?? "");
                    cmd.Parameters.AddWithValue("$ud", DateTime.Now.ToString("yyyy-MM-dd HH:mm"));
                    cmd.ExecuteNonQuery();

                    using var delCmd = conn.CreateCommand();
                    delCmd.CommandText = "DELETE FROM JobOrderItems WHERE JobOrderId = $id";
                    delCmd.Parameters.AddWithValue("$id", jo.Id);
                    delCmd.ExecuteNonQuery();
                }
                else
                {
                    using var cmd = conn.CreateCommand();
                    cmd.CommandText = @"INSERT INTO JobOrders (JONumber, ProformaInvoiceId, ClientName, ClientTRN, ClientAddress, ContactPerson, ContactNumber, ProjectName, ProjectLocation, LPONumber, JODate, RequiredDate, Status, TotalQty, ReleasedQty, BalanceQty, TotalAmount, VATAmount, DiscountAmount, NetAmount, Notes, SpecificationsJson, CreatedDate, UpdatedDate)
                    VALUES ($jo, $piId, $cn, $ctr, $ca, $cp, $cno, $pn, $pl, $lp, $jd, $rd, $st, $tq, $rq, $bq, $ta, $va, $da, $na, $nt, $specsJson, $cd, $ud)";
                    // Note: PINumber is derived from ProformaInvoiceId via JOIN, not stored directly
                    cmd.Parameters.AddWithValue("$jo", jo.JONumber);
                    cmd.Parameters.AddWithValue("$piId", jo.ProformaInvoiceId > 0 ? jo.ProformaInvoiceId : DBNull.Value);
                    cmd.Parameters.AddWithValue("$cn", jo.ClientName ?? "");
                    cmd.Parameters.AddWithValue("$ctr", jo.ClientTRN ?? "");
                    cmd.Parameters.AddWithValue("$ca", jo.ClientAddress ?? "");
                    cmd.Parameters.AddWithValue("$cp", jo.ContactPerson ?? "");
                    cmd.Parameters.AddWithValue("$cno", jo.ContactNumber ?? "");
                    cmd.Parameters.AddWithValue("$pn", jo.ProjectName ?? "");
                    cmd.Parameters.AddWithValue("$pl", jo.ProjectLocation ?? "");
                    cmd.Parameters.AddWithValue("$lp", jo.LPONumber ?? "");
                    cmd.Parameters.AddWithValue("$jd", jo.JODate.ToString("yyyy-MM-dd HH:mm:ss"));
                    cmd.Parameters.AddWithValue("$rd", jo.RequiredDate.ToString("yyyy-MM-dd HH:mm:ss"));
                    cmd.Parameters.AddWithValue("$st", jo.Status ?? "Pending");
                    cmd.Parameters.AddWithValue("$tq", jo.TotalQty);
                    cmd.Parameters.AddWithValue("$rq", jo.ReleasedQty);
                    cmd.Parameters.AddWithValue("$bq", jo.BalanceQty);
                    cmd.Parameters.AddWithValue("$ta", jo.TotalAmount);
                    cmd.Parameters.AddWithValue("$va", jo.VATAmount);
                    cmd.Parameters.AddWithValue("$da", jo.DiscountAmount);
                    cmd.Parameters.AddWithValue("$na", jo.NetAmount);
                    cmd.Parameters.AddWithValue("$nt", jo.Notes ?? "");
                    cmd.Parameters.AddWithValue("$specsJson", jo.SpecificationsJson ?? "");
                    cmd.Parameters.AddWithValue("$cd", DateTime.Now.ToString("yyyy-MM-dd HH:mm"));
                    cmd.Parameters.AddWithValue("$ud", DateTime.Now.ToString("yyyy-MM-dd HH:mm"));
                    cmd.ExecuteNonQuery();

                    using var idCmd = conn.CreateCommand();
                    idCmd.CommandText = "SELECT last_insert_rowid()";
                    jo.Id = Convert.ToInt32(idCmd.ExecuteScalar());
                }

                foreach (var item in jo.Items)
                {
                    using var itemCmd = conn.CreateCommand();
                    itemCmd.CommandText = @"INSERT INTO JobOrderItems (JobOrderId, SrNo, GlassRef, Width, Height, OrderedQty, ReleasedQty, BalanceQty, Price, TotalAmount)
                    VALUES ($joId, $sr, $gr, $w, $h, $oq, $rq, $bq, $pr, $ta)";
                    itemCmd.Parameters.AddWithValue("$joId", jo.Id);
                    itemCmd.Parameters.AddWithValue("$sr", item.SrNo);
                    itemCmd.Parameters.AddWithValue("$gr", item.GlassRef ?? "");
                    itemCmd.Parameters.AddWithValue("$w", item.Width);
                    itemCmd.Parameters.AddWithValue("$h", item.Height);
                    itemCmd.Parameters.AddWithValue("$oq", item.OrderedQty);
                    itemCmd.Parameters.AddWithValue("$rq", item.ReleasedQty);
                    itemCmd.Parameters.AddWithValue("$bq", item.BalanceQty);
                    itemCmd.Parameters.AddWithValue("$pr", item.Price);
                    itemCmd.Parameters.AddWithValue("$ta", item.TotalAmount);
                    itemCmd.ExecuteNonQuery();
                }
            });
        }

        public static List<JobOrderModel> GetAllJobOrders()
        {
            return Execute(conn =>
            {
                var list = new List<JobOrderModel>();
                using var cmd = conn.CreateCommand();
                cmd.CommandText = @"SELECT 
                    j.Id, j.JONumber, j.ProformaInvoiceId, j.ClientName, j.ProjectName, 
                    j.ProjectLocation, j.JODate, j.RequiredDate, j.Status, j.TotalQty, 
                    j.ReleasedQty, j.BalanceQty, j.TotalAmount, j.Notes, j.ClientTRN, 
                    j.ClientAddress, j.ContactPerson, j.ContactNumber, j.LPONumber, 
                    j.VATAmount, j.DiscountAmount, j.NetAmount, j.SpecificationsJson,
                    COALESCE(p.PINumber, '') AS PINumber
                    FROM JobOrders j
                    LEFT JOIN ProformaInvoices p ON j.ProformaInvoiceId = p.Id
                    ORDER BY j.Id DESC";
                using var r = cmd.ExecuteReader();
                while (r.Read())
                {
                    var jo = new JobOrderModel
                    {
                        Id = r.GetInt32(0),
                        JONumber = r.IsDBNull(1) ? "" : r.GetString(1),
                        ProformaInvoiceId = r.IsDBNull(2) ? 0 : r.GetInt32(2),
                        ClientName = r.IsDBNull(3) ? "" : r.GetString(3),
                        ProjectName = r.IsDBNull(4) ? "" : r.GetString(4),
                        ProjectLocation = r.IsDBNull(5) ? "" : r.GetString(5),
                        JODate = DateTime.TryParse(r.IsDBNull(6) ? "" : r.GetString(6), out var jd) ? jd : DateTime.Now,
                        RequiredDate = DateTime.TryParse(r.IsDBNull(7) ? "" : r.GetString(7), out var rd) ? rd : DateTime.Now.AddDays(7),
                        Status = r.IsDBNull(8) ? "Pending" : r.GetString(8),
                        TotalQty = r.GetInt32(9),
                        ReleasedQty = r.GetInt32(10),
                        BalanceQty = r.GetInt32(11),
                        TotalAmount = r.GetDouble(12),
                        Notes = r.IsDBNull(13) ? "" : r.GetString(13),
                        ClientTRN = r.IsDBNull(14) ? "" : r.GetString(14),
                        ClientAddress = r.IsDBNull(15) ? "" : r.GetString(15),
                        ContactPerson = r.IsDBNull(16) ? "" : r.GetString(16),
                        ContactNumber = r.IsDBNull(17) ? "" : r.GetString(17),
                        LPONumber = r.IsDBNull(18) ? "" : r.GetString(18),
                        VATAmount = r.IsDBNull(19) ? 0 : r.GetDouble(19),
                        DiscountAmount = r.IsDBNull(20) ? 0 : r.GetDouble(20),
                        NetAmount = r.IsDBNull(21) ? 0 : r.GetDouble(21),
                        SpecificationsJson = r.IsDBNull(22) ? "" : r.GetString(22),
                        PINumber = r.IsDBNull(23) ? "" : r.GetString(23)
                    };
                    jo.Items = GetJobOrderItems(jo.Id, conn);
                    list.Add(jo);
                }
                return list;
            });
        }

        public static List<JobOrderItemModel> GetJobOrderItems(int joId, SqliteConnection? existingConnection = null)
        {
            if (existingConnection != null)
            {
                // Use existing connection (internal use)
                var items = new List<JobOrderItemModel>();
                using var cmd = existingConnection.CreateCommand();
                cmd.CommandText = "SELECT * FROM JobOrderItems WHERE JobOrderId = $id ORDER BY SrNo";
                cmd.Parameters.AddWithValue("$id", joId);
                using var r = cmd.ExecuteReader();
                while (r.Read())
                {
                    items.Add(new JobOrderItemModel
                    {
                        Id = r.GetInt32(0),
                        SrNo = r.GetInt32(2),
                        GlassRef = r.IsDBNull(3) ? "" : r.GetString(3),
                        Width = r.GetDouble(4),
                        Height = r.GetDouble(5),
                        OrderedQty = r.GetInt32(6),
                        ReleasedQty = r.GetInt32(7),
                        BalanceQty = r.GetInt32(8),
                        Price = r.GetDouble(9),
                        TotalAmount = r.GetDouble(10)
                    });
                }
                return items;
            }

            // Create new connection for public access
            return Execute(conn => GetJobOrderItems(joId, conn));
        }

        public static void DeleteJobOrder(int id)
        {
            Execute(conn =>
            {
                using var cmd = conn.CreateCommand();
                cmd.CommandText = @"UPDATE JobOrders SET ProformaInvoiceId = $piId, ClientName = $cn, ClientTRN = $ctr, ClientAddress = $ca, ContactPerson = $cp, ContactNumber = $cno, ProjectName = $pn, ProjectLocation = $pl, LPONumber = $lp, JODate = $jd, RequiredDate = $rd, Status = $st, TotalQty = $tq, ReleasedQty = $rq, BalanceQty = $bq, TotalAmount = $ta, VATAmount = $va, DiscountAmount = $da, NetAmount = $na, Notes = $nt, SpecificationsJson = $specsJson, UpdatedDate = $ud WHERE Id = $id";
                // Note: PINumber is derived from ProformaInvoiceId via JOIN, not stored directly
                cmd.Parameters.AddWithValue("$id", id);
                cmd.ExecuteNonQuery();
                cmd.CommandText = "DELETE FROM JobOrders WHERE Id = $id";
                cmd.ExecuteNonQuery();
            });
        }

        // ═══════════════════════════════════════════════════
        // DELIVERY ORDER METHODS
        // ═══════════════════════════════════════════════════

        public static void SaveDeliveryOrder(DeliveryOrderModel d)
        {
            Execute(conn =>
            {
                using var checkCmd = conn.CreateCommand();
                checkCmd.CommandText = "SELECT Id FROM DeliveryOrders WHERE DONumber = $do";
                checkCmd.Parameters.AddWithValue("$do", d.DONumber);
                var existingId = checkCmd.ExecuteScalar();

                if (existingId != null)
                {
                    d.Id = Convert.ToInt32(existingId);
                    using var cmd = conn.CreateCommand();
                    cmd.CommandText = @"UPDATE DeliveryOrders SET JobOrderId = $joId, ProformaInvoiceId = $piId, ClientName = $cn, ProjectName = $pn, DODate = $dd, Status = $st, VehicleNumber = $vn, DriverName = $dr, Notes = $nt, TotalQty = $tq, DeliveredQty = $dq, UpdatedDate = $ud WHERE Id = $id";
                    cmd.Parameters.AddWithValue("$id", d.Id);
                    cmd.Parameters.AddWithValue("$joId", d.JobOrderId > 0 ? d.JobOrderId : DBNull.Value);
                    cmd.Parameters.AddWithValue("$piId", d.ProformaInvoiceId > 0 ? d.ProformaInvoiceId : DBNull.Value);
                    cmd.Parameters.AddWithValue("$cn", d.ClientName ?? "");
                    cmd.Parameters.AddWithValue("$pn", d.ProjectName ?? "");
                    cmd.Parameters.AddWithValue("$dd", d.DODate.ToString("yyyy-MM-dd HH:mm:ss"));
                    cmd.Parameters.AddWithValue("$st", d.Status ?? "Pending");
                    cmd.Parameters.AddWithValue("$vn", d.VehicleNumber ?? "");
                    cmd.Parameters.AddWithValue("$dr", d.DriverName ?? "");
                    cmd.Parameters.AddWithValue("$nt", d.Notes ?? "");
                    cmd.Parameters.AddWithValue("$tq", d.TotalQty);
                    cmd.Parameters.AddWithValue("$dq", d.DeliveredQty);
                    cmd.Parameters.AddWithValue("$ud", DateTime.Now.ToString("yyyy-MM-dd HH:mm"));
                    cmd.ExecuteNonQuery();

                    using var delCmd = conn.CreateCommand();
                    delCmd.CommandText = "DELETE FROM DeliveryOrderItems WHERE DeliveryOrderId = $id";
                    delCmd.Parameters.AddWithValue("$id", d.Id);
                    delCmd.ExecuteNonQuery();
                }
                else
                {
                    using var cmd = conn.CreateCommand();
                    cmd.CommandText = @"INSERT INTO DeliveryOrders (DONumber, JobOrderId, ProformaInvoiceId, ClientName, ProjectName, DODate, Status, VehicleNumber, DriverName, Notes, TotalQty, DeliveredQty, CreatedDate, UpdatedDate)
                    VALUES ($do, $joId, $piId, $cn, $pn, $dd, $st, $vn, $dr, $nt, $tq, $dq, $cd, $ud)";
                    cmd.Parameters.AddWithValue("$do", d.DONumber);
                    cmd.Parameters.AddWithValue("$joId", d.JobOrderId > 0 ? d.JobOrderId : DBNull.Value);
                    cmd.Parameters.AddWithValue("$piId", d.ProformaInvoiceId > 0 ? d.ProformaInvoiceId : DBNull.Value);
                    cmd.Parameters.AddWithValue("$cn", d.ClientName ?? "");
                    cmd.Parameters.AddWithValue("$pn", d.ProjectName ?? "");
                    cmd.Parameters.AddWithValue("$dd", d.DODate.ToString("yyyy-MM-dd HH:mm:ss"));
                    cmd.Parameters.AddWithValue("$st", d.Status ?? "Pending");
                    cmd.Parameters.AddWithValue("$vn", d.VehicleNumber ?? "");
                    cmd.Parameters.AddWithValue("$dr", d.DriverName ?? "");
                    cmd.Parameters.AddWithValue("$nt", d.Notes ?? "");
                    cmd.Parameters.AddWithValue("$tq", d.TotalQty);
                    cmd.Parameters.AddWithValue("$dq", d.DeliveredQty);
                    cmd.Parameters.AddWithValue("$cd", DateTime.Now.ToString("yyyy-MM-dd HH:mm"));
                    cmd.Parameters.AddWithValue("$ud", DateTime.Now.ToString("yyyy-MM-dd HH:mm"));
                    cmd.ExecuteNonQuery();

                    using var idCmd = conn.CreateCommand();
                    idCmd.CommandText = "SELECT last_insert_rowid()";
                    d.Id = Convert.ToInt32(idCmd.ExecuteScalar());
                }

                foreach (var item in d.Items)
                {
                    using var itemCmd = conn.CreateCommand();
                    itemCmd.CommandText = @"INSERT INTO DeliveryOrderItems (DeliveryOrderId, SrNo, GlassRef, Width, Height, DeliveredQty)
                    VALUES ($doId, $sr, $gr, $w, $h, $dq)";
                    itemCmd.Parameters.AddWithValue("$doId", d.Id);
                    itemCmd.Parameters.AddWithValue("$sr", item.SrNo);
                    itemCmd.Parameters.AddWithValue("$gr", item.GlassRef ?? "");
                    itemCmd.Parameters.AddWithValue("$w", item.Width);
                    itemCmd.Parameters.AddWithValue("$h", item.Height);
                    itemCmd.Parameters.AddWithValue("$dq", item.DeliveredQty);
                    itemCmd.ExecuteNonQuery();
                }
            });
        }

        public static List<DeliveryOrderModel> GetAllDeliveryOrders()
        {
            return Execute(conn =>
            {
                var list = new List<DeliveryOrderModel>();
                using var cmd = conn.CreateCommand();
                cmd.CommandText = "SELECT * FROM DeliveryOrders ORDER BY Id DESC";
                using var r = cmd.ExecuteReader();
                while (r.Read())
                {
                    var d = new DeliveryOrderModel
                    {
                        Id = r.GetInt32(0),
                        DONumber = r.IsDBNull(1) ? "" : r.GetString(1),
                        JobOrderId = r.IsDBNull(2) ? 0 : r.GetInt32(2),
                        ProformaInvoiceId = r.IsDBNull(3) ? 0 : r.GetInt32(3),
                        ClientName = r.IsDBNull(4) ? "" : r.GetString(4),
                        ProjectName = r.IsDBNull(5) ? "" : r.GetString(5),
                        DODate = DateTime.TryParse(r.IsDBNull(6) ? "" : r.GetString(6), out var dd) ? dd : DateTime.Now,
                        Status = r.IsDBNull(7) ? "Pending" : r.GetString(7),
                        VehicleNumber = r.IsDBNull(8) ? "" : r.GetString(8),
                        DriverName = r.IsDBNull(9) ? "" : r.GetString(9),
                        Notes = r.IsDBNull(10) ? "" : r.GetString(10),
                        TotalQty = r.GetInt32(11),
                        DeliveredQty = r.GetInt32(12)
                    };
                    d.Items = GetDeliveryOrderItems(d.Id, conn);
                    list.Add(d);
                }
                return list;
            });
        }

        private static List<DeliveryOrderItemModel> GetDeliveryOrderItems(int doId, SqliteConnection conn)
        {
            var items = new List<DeliveryOrderItemModel>();
            using var cmd = conn.CreateCommand();
            cmd.CommandText = "SELECT * FROM DeliveryOrderItems WHERE DeliveryOrderId = $id ORDER BY SrNo";
            cmd.Parameters.AddWithValue("$id", doId);
            using var r = cmd.ExecuteReader();
            while (r.Read())
            {
                items.Add(new DeliveryOrderItemModel
                {
                    Id = r.GetInt32(0),
                    SrNo = r.GetInt32(2),
                    GlassRef = r.IsDBNull(3) ? "" : r.GetString(3),
                    Width = r.GetDouble(4),
                    Height = r.GetDouble(5),
                    DeliveredQty = r.GetInt32(6)
                });
            }
            return items;
        }

        public static void DeleteDeliveryOrder(int id)
        {
            Execute(conn =>
            {
                using var cmd = conn.CreateCommand();
                cmd.CommandText = "DELETE FROM DeliveryOrderItems WHERE DeliveryOrderId = $id";
                cmd.Parameters.AddWithValue("$id", id);
                cmd.ExecuteNonQuery();
                cmd.CommandText = "DELETE FROM DeliveryOrders WHERE Id = $id";
                cmd.ExecuteNonQuery();
            });
        }

        // ═══════════════════════════════════════════════════
        // TAX INVOICE METHODS
        // ═══════════════════════════════════════════════════

        public static void SaveTaxInvoice(TaxInvoiceModel ti)
        {
            Execute(conn =>
            {
                using var checkCmd = conn.CreateCommand();
                checkCmd.CommandText = "SELECT Id FROM TaxInvoices WHERE InvoiceNumber = $inv";
                checkCmd.Parameters.AddWithValue("$inv", ti.InvoiceNumber);
                var existingId = checkCmd.ExecuteScalar();

                if (existingId != null)
                {
                    ti.Id = Convert.ToInt32(existingId);
                    using var cmd = conn.CreateCommand();
                    cmd.CommandText = @"UPDATE TaxInvoices SET ProformaInvoiceId = $piId, JobOrderId = $joId, DeliveryOrderId = $doId, ClientName = $cn, ClientTRN = $ctr, ClientAddress = $ca, InvoiceDate = $idate, DueDate = $dd, Status = $st, PaymentStatus = $ps, SubTotal = $sub, VATPercent = $vp, VATAmount = $va, TotalAmount = $ta, PaidAmount = $pa, BalanceAmount = $ba, Notes = $nt, UpdatedDate = $ud WHERE Id = $id";
                    cmd.Parameters.AddWithValue("$id", ti.Id);
                    cmd.Parameters.AddWithValue("$piId", ti.ProformaInvoiceId > 0 ? ti.ProformaInvoiceId : DBNull.Value);
                    cmd.Parameters.AddWithValue("$joId", ti.JobOrderId > 0 ? ti.JobOrderId : DBNull.Value);
                    cmd.Parameters.AddWithValue("$doId", ti.DeliveryOrderId > 0 ? ti.DeliveryOrderId : DBNull.Value);
                    cmd.Parameters.AddWithValue("$cn", ti.ClientName ?? "");
                    cmd.Parameters.AddWithValue("$ctr", ti.ClientTRN ?? "");
                    cmd.Parameters.AddWithValue("$ca", ti.ClientAddress ?? "");
                    cmd.Parameters.AddWithValue("$idate", ti.InvoiceDate.ToString("yyyy-MM-dd HH:mm:ss"));
                    cmd.Parameters.AddWithValue("$dd", ti.DueDate.ToString("yyyy-MM-dd HH:mm:ss"));
                    cmd.Parameters.AddWithValue("$st", ti.Status ?? "Pending");
                    cmd.Parameters.AddWithValue("$ps", ti.PaymentStatus ?? "Unpaid");
                    cmd.Parameters.AddWithValue("$sub", ti.SubTotal);
                    cmd.Parameters.AddWithValue("$vp", ti.VATPercent);
                    cmd.Parameters.AddWithValue("$va", ti.VATAmount);
                    cmd.Parameters.AddWithValue("$ta", ti.TotalAmount);
                    cmd.Parameters.AddWithValue("$pa", ti.PaidAmount);
                    cmd.Parameters.AddWithValue("$ba", ti.BalanceAmount);
                    cmd.Parameters.AddWithValue("$nt", ti.Notes ?? "");
                    cmd.Parameters.AddWithValue("$ud", DateTime.Now.ToString("yyyy-MM-dd HH:mm"));
                    cmd.ExecuteNonQuery();

                    using var delCmd = conn.CreateCommand();
                    delCmd.CommandText = "DELETE FROM TaxInvoiceItems WHERE TaxInvoiceId = $id";
                    delCmd.Parameters.AddWithValue("$id", ti.Id);
                    delCmd.ExecuteNonQuery();
                }
                else
                {
                    using var cmd = conn.CreateCommand();
                    cmd.CommandText = @"INSERT INTO TaxInvoices (InvoiceNumber, ProformaInvoiceId, JobOrderId, DeliveryOrderId, ClientName, ClientTRN, ClientAddress, InvoiceDate, DueDate, Status, PaymentStatus, SubTotal, VATPercent, VATAmount, TotalAmount, PaidAmount, BalanceAmount, Notes, CreatedDate, UpdatedDate)
VALUES ($inv, $piId, $joId, $doId, $cn, $ctr, $ca, $idate, $dd, $st, $ps, $sub, $vp, $va, $ta, $pa, $ba, $nt, $cd, $ud)";
                    cmd.Parameters.AddWithValue("$inv", ti.InvoiceNumber);
                    cmd.Parameters.AddWithValue("$piId", ti.ProformaInvoiceId > 0 ? ti.ProformaInvoiceId : DBNull.Value);
                    cmd.Parameters.AddWithValue("$joId", ti.JobOrderId > 0 ? ti.JobOrderId : DBNull.Value);
                    cmd.Parameters.AddWithValue("$doId", ti.DeliveryOrderId > 0 ? ti.DeliveryOrderId : DBNull.Value);
                    cmd.Parameters.AddWithValue("$cn", ti.ClientName ?? "");
                    cmd.Parameters.AddWithValue("$ctr", ti.ClientTRN ?? "");
                    cmd.Parameters.AddWithValue("$ca", ti.ClientAddress ?? "");
                    cmd.Parameters.AddWithValue("$idate", ti.InvoiceDate.ToString("yyyy-MM-dd HH:mm:ss"));
                    cmd.Parameters.AddWithValue("$dd", ti.DueDate.ToString("yyyy-MM-dd HH:mm:ss"));
                    cmd.Parameters.AddWithValue("$st", ti.Status ?? "Pending");
                    cmd.Parameters.AddWithValue("$ps", ti.PaymentStatus ?? "Unpaid");
                    cmd.Parameters.AddWithValue("$sub", ti.SubTotal);
                    cmd.Parameters.AddWithValue("$vp", ti.VATPercent);
                    cmd.Parameters.AddWithValue("$va", ti.VATAmount);
                    cmd.Parameters.AddWithValue("$ta", ti.TotalAmount);
                    cmd.Parameters.AddWithValue("$pa", ti.PaidAmount);
                    cmd.Parameters.AddWithValue("$ba", ti.BalanceAmount);
                    cmd.Parameters.AddWithValue("$nt", ti.Notes ?? "");
                    cmd.Parameters.AddWithValue("$cd", DateTime.Now.ToString("yyyy-MM-dd HH:mm"));
                    cmd.Parameters.AddWithValue("$ud", DateTime.Now.ToString("yyyy-MM-dd HH:mm"));
                    cmd.ExecuteNonQuery();

                    using var idCmd = conn.CreateCommand();
                    idCmd.CommandText = "SELECT last_insert_rowid()";
                    ti.Id = Convert.ToInt32(idCmd.ExecuteScalar());
                }

                foreach (var item in ti.Items)
                {
                    using var itemCmd = conn.CreateCommand();
                    itemCmd.CommandText = @"INSERT INTO TaxInvoiceItems (TaxInvoiceId, SrNo, Description, Qty, UnitPrice, TotalPrice)
            VALUES ($tiId, $sr, $desc, $q, $up, $tp)";
                    itemCmd.Parameters.AddWithValue("$tiId", ti.Id);
                    itemCmd.Parameters.AddWithValue("$sr", item.SrNo);
                    itemCmd.Parameters.AddWithValue("$desc", item.Description ?? "");
                    itemCmd.Parameters.AddWithValue("$q", item.Qty);
                    itemCmd.Parameters.AddWithValue("$up", item.UnitPrice);
                    itemCmd.Parameters.AddWithValue("$tp", item.TotalPrice);
                    itemCmd.ExecuteNonQuery();
                }
            });
        }

        public static List<TaxInvoiceModel> GetAllTaxInvoices()
        {
            return Execute(conn =>
            {
                var list = new List<TaxInvoiceModel>();
                using var cmd = conn.CreateCommand();
                cmd.CommandText = "SELECT * FROM TaxInvoices ORDER BY Id DESC";
                using var r = cmd.ExecuteReader();
                while (r.Read())
                {
                    var ti = new TaxInvoiceModel
                    {
                        Id = r.GetInt32(0),
                        InvoiceNumber = r.IsDBNull(1) ? "" : r.GetString(1),
                        ProformaInvoiceId = r.IsDBNull(2) ? 0 : r.GetInt32(2),
                        JobOrderId = r.IsDBNull(3) ? 0 : r.GetInt32(3),
                        DeliveryOrderId = r.IsDBNull(4) ? 0 : r.GetInt32(4),
                        ClientName = r.IsDBNull(5) ? "" : r.GetString(5),
                        ClientTRN = r.IsDBNull(6) ? "" : r.GetString(6),
                        ClientAddress = r.IsDBNull(7) ? "" : r.GetString(7),
                        InvoiceDate = DateTime.TryParse(r.IsDBNull(8) ? "" : r.GetString(8), out var idate) ? idate : DateTime.Now,
                        DueDate = DateTime.TryParse(r.IsDBNull(9) ? "" : r.GetString(9), out var dd) ? dd : DateTime.Now.AddDays(30),
                        Status = r.IsDBNull(10) ? "Pending" : r.GetString(10),
                        PaymentStatus = r.IsDBNull(11) ? "Unpaid" : r.GetString(11),
                        SubTotal = r.GetDouble(12),
                        VATPercent = r.GetDouble(13),
                        VATAmount = r.GetDouble(14),
                        TotalAmount = r.GetDouble(15),
                        PaidAmount = r.GetDouble(16),
                        BalanceAmount = r.GetDouble(17),
                        Notes = r.IsDBNull(18) ? "" : r.GetString(18)
                    };
                    ti.Items = GetTaxInvoiceItems(ti.Id, conn);
                    list.Add(ti);
                }
                return list;
            });
        }

        private static List<TaxInvoiceItemModel> GetTaxInvoiceItems(int tiId, SqliteConnection conn)
        {
            var items = new List<TaxInvoiceItemModel>();
            using var cmd = conn.CreateCommand();
            cmd.CommandText = "SELECT * FROM TaxInvoiceItems WHERE TaxInvoiceId = $id ORDER BY SrNo";
            cmd.Parameters.AddWithValue("$id", tiId);
            using var r = cmd.ExecuteReader();
            while (r.Read())
            {
                items.Add(new TaxInvoiceItemModel
                {
                    Id = r.GetInt32(0),
                    SrNo = r.GetInt32(2),
                    Description = r.IsDBNull(3) ? "" : r.GetString(3),
                    Qty = r.GetInt32(4),
                    UnitPrice = r.GetDouble(5),
                    TotalPrice = r.GetDouble(6)
                });
            }
            return items;
        }

        public static void DeleteTaxInvoice(int id)
        {
            Execute(conn =>
            {
                using var cmd = conn.CreateCommand();
                cmd.CommandText = "DELETE FROM TaxInvoiceItems WHERE TaxInvoiceId = $id";
                cmd.Parameters.AddWithValue("$id", id);
                cmd.ExecuteNonQuery();
                cmd.CommandText = "DELETE FROM TaxInvoices WHERE Id = $id";
                cmd.ExecuteNonQuery();
            });
        }

        // ═══════════════════════════════════════════════════
        // DASHBOARD STATS
        // ═══════════════════════════════════════════════════

        public static DashboardStats GetDashboardStats()
        {
            var stats = new DashboardStats();
            try
            {
                return Execute(conn =>
                {
                    using (var cmd = conn.CreateCommand())
                    {
                        cmd.CommandText = "SELECT COUNT(*) FROM ProformaInvoices";
                        stats.TotalPI = Convert.ToInt32(cmd.ExecuteScalar());
                    }
                    using (var cmd = conn.CreateCommand())
                    {
                        cmd.CommandText = "SELECT COUNT(*) FROM ProformaInvoices WHERE Status = 'Draft'";
                        stats.PendingPI = Convert.ToInt32(cmd.ExecuteScalar());
                    }
                    using (var cmd = conn.CreateCommand())
                    {
                        cmd.CommandText = "SELECT COALESCE(SUM(NetAmount), 0) FROM ProformaInvoices";
                        stats.TotalPIValue = Convert.ToDouble(cmd.ExecuteScalar());
                    }
                    using (var cmd = conn.CreateCommand())
                    {
                        cmd.CommandText = "SELECT COUNT(*) FROM JobOrders";
                        stats.TotalJO = Convert.ToInt32(cmd.ExecuteScalar());
                    }
                    using (var cmd = conn.CreateCommand())
                    {
                        cmd.CommandText = "SELECT COUNT(*) FROM JobOrders WHERE Status = 'Pending'";
                        stats.PendingJO = Convert.ToInt32(cmd.ExecuteScalar());
                    }
                    using (var cmd = conn.CreateCommand())
                    {
                        cmd.CommandText = "SELECT COALESCE(SUM(BalanceQty), 0) FROM JobOrders";
                        stats.TotalBalanceQty = Convert.ToInt32(cmd.ExecuteScalar());
                    }
                    using (var cmd = conn.CreateCommand())
                    {
                        cmd.CommandText = "SELECT COUNT(*) FROM DeliveryOrders";
                        stats.TotalDO = Convert.ToInt32(cmd.ExecuteScalar());
                    }
                    using (var cmd = conn.CreateCommand())
                    {
                        cmd.CommandText = "SELECT COUNT(*) FROM DeliveryOrders WHERE Status = 'Pending'";
                        stats.PendingDO = Convert.ToInt32(cmd.ExecuteScalar());
                    }
                    using (var cmd = conn.CreateCommand())
                    {
                        cmd.CommandText = "SELECT COUNT(*) FROM TaxInvoices";
                        stats.TotalTaxInvoice = Convert.ToInt32(cmd.ExecuteScalar());
                    }
                    using (var cmd = conn.CreateCommand())
                    {
                        cmd.CommandText = "SELECT COALESCE(SUM(TotalAmount), 0) FROM TaxInvoices";
                        stats.TotalTaxInvoiceValue = Convert.ToDouble(cmd.ExecuteScalar());
                    }
                    using (var cmd = conn.CreateCommand())
                    {
                        cmd.CommandText = "SELECT COALESCE(SUM(PaidAmount), 0) FROM TaxInvoices";
                        stats.TotalPaidAmount = Convert.ToDouble(cmd.ExecuteScalar());
                    }
                    using (var cmd = conn.CreateCommand())
                    {
                        cmd.CommandText = "SELECT COALESCE(SUM(BalanceAmount), 0) FROM TaxInvoices";
                        stats.TotalOutstanding = Convert.ToDouble(cmd.ExecuteScalar());
                    }
                    return stats;
                });
            }
            catch { return new DashboardStats(); }
        }

        // ═══════════════════════════════════════════════════
        // NUMBER GENERATORS
        // ═══════════════════════════════════════════════════

        public static string GenerateNextPINumber()
        {
            return Execute(conn =>
            {
                var year = DateTime.Now.Year;
                var prefix = $"PI-{year}-";
                using var cmd = conn.CreateCommand();
                cmd.CommandText = "SELECT MAX(PINumber) FROM ProformaInvoices WHERE PINumber LIKE $prefix";
                cmd.Parameters.AddWithValue("$prefix", prefix + "%");
                var last = cmd.ExecuteScalar()?.ToString();
                if (!string.IsNullOrEmpty(last))
                {
                    var parts = last.Split('-');
                    if (parts.Length >= 3 && int.TryParse(parts[2], out var num))
                        return $"{prefix}{(num + 1):D4}";
                }
                return $"{prefix}0001";
            });
        }

        public static string GenerateNextJONumber()
        {
            return Execute(conn =>
            {
                var year = DateTime.Now.Year;
                var prefix = $"JO-{year}-";
                using var cmd = conn.CreateCommand();
                cmd.CommandText = "SELECT MAX(JONumber) FROM JobOrders WHERE JONumber LIKE $prefix";
                cmd.Parameters.AddWithValue("$prefix", prefix + "%");
                var last = cmd.ExecuteScalar()?.ToString();
                if (!string.IsNullOrEmpty(last))
                {
                    var parts = last.Split('-');
                    if (parts.Length >= 3 && int.TryParse(parts[2], out var num))
                        return $"{prefix}{(num + 1):D4}";
                }
                return $"{prefix}0001";
            });
        }

        public static string GenerateNextDONumber()
        {
            return Execute(conn =>
            {
                var year = DateTime.Now.Year;
                var prefix = $"DO-{year}-";
                using var cmd = conn.CreateCommand();
                cmd.CommandText = "SELECT MAX(DONumber) FROM DeliveryOrders WHERE DONumber LIKE $prefix";
                cmd.Parameters.AddWithValue("$prefix", prefix + "%");
                var last = cmd.ExecuteScalar()?.ToString();
                if (!string.IsNullOrEmpty(last))
                {
                    var parts = last.Split('-');
                    if (parts.Length >= 3 && int.TryParse(parts[2], out var num))
                        return $"{prefix}{(num + 1):D4}";
                }
                return $"{prefix}0001";
            });
        }

        public static string GenerateNextTaxInvoiceNumber()
        {
            return Execute(conn =>
            {
                var year = DateTime.Now.Year;
                var prefix = $"TI-{year}-";
                using var cmd = conn.CreateCommand();
                cmd.CommandText = "SELECT MAX(InvoiceNumber) FROM TaxInvoices WHERE InvoiceNumber LIKE $prefix";
                cmd.Parameters.AddWithValue("$prefix", prefix + "%");
                var last = cmd.ExecuteScalar()?.ToString();
                if (!string.IsNullOrEmpty(last))
                {
                    var parts = last.Split('-');
                    if (parts.Length >= 3 && int.TryParse(parts[2], out var num))
                        return $"{prefix}{(num + 1):D4}";
                }
                return $"{prefix}0001";
            });
        }

        // ═══════════════════════════════════════════════════
        // HELPER CLASSES
        // ═══════════════════════════════════════════════════

        public class DashboardStats
        {
            public int TotalPI { get; set; }
            public int PendingPI { get; set; }
            public double TotalPIValue { get; set; }
            public int TotalJO { get; set; }
            public int PendingJO { get; set; }
            public int TotalBalanceQty { get; set; }
            public int TotalDO { get; set; }
            public int PendingDO { get; set; }
            public int TotalTaxInvoice { get; set; }
            public double TotalTaxInvoiceValue { get; set; }
            public double TotalPaidAmount { get; set; }
            public double TotalOutstanding { get; set; }
            public int TotalProduction { get; set; }
            public double Efficiency { get; set; }
            public double AverageUptime { get; set; }
        }

        public class ImportSessionInfo
        {
            public int Id { get; set; }
            public DateTime SessionDateTime { get; set; }
            public int ImportedCount { get; set; }
            public int UpdatedCount { get; set; }
            public int SkippedCount { get; set; }
            public string Notes { get; set; }
        }

        public class ImportLogInfo
        {
            public int Id { get; set; }
            public DateTime ImportDateTime { get; set; }
            public string PINumber { get; set; }
            public string Company { get; set; }
            public string ChangesJson { get; set; }
        }

        public class CustomerRefItem
        {
            public int Id { get; set; }
            public string CustomerReference { get; set; }
            public string Company { get; set; }
        }

        public class ImportSession
        {
            public int Id { get; set; }
            public DateTime SessionDateTime { get; set; }
            public string Notes { get; set; }
            public int ImportedCount { get; set; }
            public int UpdatedCount { get; set; }
            public int SkippedCount { get; set; }
            public ObservableCollection<ImportLog> Entries { get; set; } = new ObservableCollection<ImportLog>();
        }

        public class ImportLog
        {
            public int Id { get; set; }
            public DateTime ImportDateTime { get; set; }
            public string PINumber { get; set; }
            public string Company { get; set; }
            public ObservableCollection<ImportLogItem> Changes { get; set; } = new ObservableCollection<ImportLogItem>();
        }

        public class ImportLogItem
        {
            public string FieldName { get; set; }
            public string OldValue { get; set; }
            public string NewValue { get; set; }
        }

        // ═══════════════════════════════════════════════════
        // LIVE DATA SERVICE METHODS
        // ═══════════════════════════════════════════════════

        public static int GetTodayTotalProduction()
        {
            try
            {
                return Execute(conn =>
                {
                    using var cmd = conn.CreateCommand();
                    cmd.CommandText = "SELECT COALESCE(SUM(Qty), 0) FROM DailyWork WHERE DATE(Date) = DATE('now', 'localtime') AND ProductionStatus = 'Completed'";
                    return Convert.ToInt32(cmd.ExecuteScalar());
                });
            }
            catch { return 0; }
        }

        public static int GetTodayCompletedOrders()
        {
            try
            {
                return Execute(conn =>
                {
                    using var cmd = conn.CreateCommand();
                    cmd.CommandText = "SELECT COUNT(*) FROM DailyWork WHERE DATE(Date) = DATE('now', 'localtime') AND Status = 'Completed'";
                    return Convert.ToInt32(cmd.ExecuteScalar());
                });
            }
            catch { return 0; }
        }

        public static int GetTodayTotalOrders()
        {
            try
            {
                return Execute(conn =>
                {
                    using var cmd = conn.CreateCommand();
                    cmd.CommandText = "SELECT COUNT(*) FROM DailyWork WHERE DATE(Date) = DATE('now', 'localtime')";
                    return Convert.ToInt32(cmd.ExecuteScalar());
                });
            }
            catch { return 0; }
        }

        // ═══════════════════════════════════════════════════
        // BALANCE SERVICE METHODS
        // ═══════════════════════════════════════════════════

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
                        UnitPrice = Convert.ToDecimal(r.GetDouble(3)),
                        Supplier = r.IsDBNull(4) ? "" : r.GetString(4),
                        PurchasedOn = DateTime.TryParse(r.GetString(5), out var pd) ? pd : DateTime.Today,
                        Notes = r.IsDBNull(6) ? "" : r.GetString(6),
                        CreatedAt = DateTime.TryParse(r.GetString(7), out var cd) ? cd : DateTime.Now
                    });
                }
                return list;
            });
        }
    }

    // ═══════════════════════════════════════════════════
    // MODEL CLASSES
    // ═══════════════════════════════════════════════════

    public class SguRecord
    {
        public int Id { get; set; }
        public string Category { get; set; } = "";
        public string Thickness { get; set; } = "";
        public string Color { get; set; } = "";
        public double SheetPrice { get; set; }
        public double Cutting { get; set; }
        public double TemperingCharge { get; set; }
        public double OtherCharges { get; set; }
        public string Wastage { get; set; } = "";
        public string ProfitMargin { get; set; } = "";
        public string EdgeWork { get; set; } = "";
        public string Drilling { get; set; } = "";
        public string Tempering { get; set; } = "";
        public string Coating { get; set; } = "";
        public string SurfaceTreatment { get; set; } = "";
        public string Cutout { get; set; } = "";
        public string Unit { get; set; } = "AED";
        public int Width { get; set; }
        public int Height { get; set; }
        public int Quantity { get; set; }
        public double TotalArea { get; set; }
        public double TotalPrice { get; set; }
        public double Result { get; set; }
        public string CustomNotes { get; set; } = "";
        public string CreatedAt { get; set; } = "";
    }

    public class DguRecord
    {
        public int Id { get; set; }
        public string Thickness1 { get; set; } = "";
        public string Color1 { get; set; } = "";
        public string Thickness2 { get; set; } = "";
        public string Color2 { get; set; } = "";
        public string Spacer { get; set; } = "";
        public double Result { get; set; }
        public string CreatedAt { get; set; } = "";
    }

    public class LaminationRecord
    {
        public int Id { get; set; }
        public string Thickness1 { get; set; } = "";
        public string Color1 { get; set; } = "";
        public string Thickness2 { get; set; } = "";
        public string Color2 { get; set; } = "";
        public string PVBType { get; set; } = "";
        public double Result { get; set; }
        public string CreatedAt { get; set; } = "";
        public double Cutting { get; set; }
        public double Tempering { get; set; }
        public bool IncludeCutting { get; set; }
        public bool IncludeTempering { get; set; }
    }

    public class DailyWork
    {
        public int Id { get; set; }
        public DateTime Date { get; set; } = DateTime.Today;
        public DateTime UpdateDate { get; set; } = DateTime.Today;
        public string Company { get; set; } = "";
        public string PINumber { get; set; } = "";
        public string CustomerReference { get; set; } = "";
        public string TypeOfWork { get; set; } = "";
        public string ProductionStatus { get; set; } = "";
        public string DailyReportStatus { get; set; } = "";
        public int Qty { get; set; }
        public double SQM { get; set; }
        public string Status { get; set; } = "";
        public string Salesman { get; set; } = "";
        public string Color { get; set; } = "";
        public string Notes { get; set; } = "";
        public DateTime CreatedDate { get; set; } = DateTime.Now;

        // Clone method for copying records
        public DailyWork Clone()
        {
            return new DailyWork
            {
                Id = this.Id,
                Date = this.Date,
                UpdateDate = this.UpdateDate,
                Company = this.Company,
                PINumber = this.PINumber,
                CustomerReference = this.CustomerReference,
                TypeOfWork = this.TypeOfWork,
                ProductionStatus = this.ProductionStatus,
                DailyReportStatus = this.DailyReportStatus,
                Qty = this.Qty,
                SQM = this.SQM,
                Status = this.Status,
                Salesman = this.Salesman,
                Color = this.Color,
                Notes = this.Notes,
                CreatedDate = this.CreatedDate
            };
        }
    }

    public class Delivery : ViewModelBase
    {
        private int _id;
        private int _sourceId;
        private DateTime _date = DateTime.Today;
        private string _company = "";
        private string _piNumber = "";
        private string _customerReference = "";
        private string _typeOfWork = "";
        private int _orderQty;
        private double _orderSQM;
        private string _salesman = "";
        private string _color = "";
        private string _productionStatus = "";
        private string _status = "Pending";
        private string _notes = "";
        private DateTime _createdDate = DateTime.Now;
        private DateTime _updatedDate = DateTime.Now;
        private ObservableCollection<DeliveryItem> _deliveryItems = new ObservableCollection<DeliveryItem>();

        public int Id
        {
            get => _id;
            set { _id = value; OnPropertyChanged(nameof(Id)); }
        }

        public int SourceId
        {
            get => _sourceId;
            set { _sourceId = value; OnPropertyChanged(nameof(SourceId)); }
        }

        public DateTime Date
        {
            get => _date;
            set { _date = value; OnPropertyChanged(nameof(Date)); }
        }

        public string Company
        {
            get => _company;
            set { _company = value ?? ""; OnPropertyChanged(nameof(Company)); }
        }

        public string PINumber
        {
            get => _piNumber;
            set { _piNumber = value ?? ""; OnPropertyChanged(nameof(PINumber)); }
        }

        public string CustomerReference
        {
            get => _customerReference;
            set { _customerReference = value ?? ""; OnPropertyChanged(nameof(CustomerReference)); }
        }

        public string TypeOfWork
        {
            get => _typeOfWork;
            set { _typeOfWork = value ?? ""; OnPropertyChanged(nameof(TypeOfWork)); }
        }

        public int OrderQty
        {
            get => _orderQty;
            set { _orderQty = value; OnPropertyChanged(nameof(OrderQty)); OnPropertyChanged(nameof(Balance)); OnPropertyChanged(nameof(BalanceSQM)); }
        }

        public double OrderSQM
        {
            get => _orderSQM;
            set { _orderSQM = value; OnPropertyChanged(nameof(OrderSQM)); OnPropertyChanged(nameof(BalanceSQM)); }
        }

        public string Salesman
        {
            get => _salesman;
            set { _salesman = value ?? ""; OnPropertyChanged(nameof(Salesman)); }
        }

        public string Color
        {
            get => _color;
            set { _color = value ?? ""; OnPropertyChanged(nameof(Color)); }
        }

        public string ProductionStatus
        {
            get => _productionStatus;
            set { _productionStatus = value ?? ""; OnPropertyChanged(nameof(ProductionStatus)); }
        }

        public string Status
        {
            get => _status;
            set { _status = value ?? ""; OnPropertyChanged(nameof(Status)); }
        }

        public string Notes
        {
            get => _notes;
            set { _notes = value ?? ""; OnPropertyChanged(nameof(Notes)); }
        }

        public DateTime CreatedDate
        {
            get => _createdDate;
            set { _createdDate = value; OnPropertyChanged(nameof(CreatedDate)); }
        }

        public DateTime UpdatedDate
        {
            get => _updatedDate;
            set { _updatedDate = value; OnPropertyChanged(nameof(UpdatedDate)); }
        }

        public ObservableCollection<DeliveryItem> DeliveryItems
        {
            get => _deliveryItems;
            set { _deliveryItems = value; OnPropertyChanged(nameof(DeliveryItems)); }
        }

        // Computed Properties
        public int TotalDelivered => DeliveryItems?.Sum(x => x.DeliveredQty) ?? 0;
        public int TotalReturned => DeliveryItems?.Sum(x => x.ReturnedQty) ?? 0;
        public int Balance => OrderQty - TotalDelivered + TotalReturned;
        public double BalanceSQM => Math.Round(OrderSQM - (TotalDelivered > 0 ? (double)TotalDelivered / OrderQty * OrderSQM : 0) + (TotalReturned > 0 ? (double)TotalReturned / OrderQty * OrderSQM : 0), 2);
    }

    public class DeliveryItem
    {
        public int Id { get; set; }
        public int OrderId { get; set; }
        public DateTime DeliveryDate { get; set; } = DateTime.Today;
        public int DeliveredQty { get; set; }
        public double DeliveredSQM { get; set; }
        public int ReturnedQty { get; set; }
        public double ReturnedSQM { get; set; }
        public string Driver { get; set; } = "";
        public string Vehicle { get; set; } = "";
        public string Notes { get; set; } = "";
        public DateTime CreatedDate { get; set; } = DateTime.Now;
    }

    public class Sheet
    {
        public int Id { get; set; }
        public string Category { get; set; } = "";
        public string Thickness { get; set; } = "";
        public string Color { get; set; } = "";
        public string ColorHex { get; set; } = "";
        public int Width { get; set; }
        public int Height { get; set; }
        public double SquareMeter { get; set; }
        public decimal PurchasePrice { get; set; }
        public decimal SellPrice { get; set; }
        public int TotalStock { get; set; }
        public int UsedSheets { get; set; }
        public int BalanceSheets { get; set; }
        public bool IsActive { get; set; } = true;
        public string Supplier { get; set; } = "";
        public string SupplierName { get; set; } = "";
        public string Description { get; set; } = "";
        public DateTime CreatedDate { get; set; } = DateTime.Now;
        public DateTime? LatestPurchaseDate { get; set; }
    }

    public class SheetPurchase
    {
        public int Id { get; set; }
        public int SheetId { get; set; }
        public int Quantity { get; set; }
        public decimal UnitPrice { get; set; }
        public string Supplier { get; set; } = "";
        public DateTime PurchasedOn { get; set; } = DateTime.Today;
        public string Notes { get; set; } = "";
        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }

    public class SheetUsage
    {
        public int Id { get; set; }
        public int SheetId { get; set; }
        public int Quantity { get; set; }
        public string Reason { get; set; } = "";
        public DateTime UsedOn { get; set; } = DateTime.Today;
        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }

    // Proforma Invoice Models
    public class ProformaInvoiceModel
    {
        public int Id { get; set; }
        public string InvoiceNo { get; set; } = "";
        public string CustomerName { get; set; } = "";
        public string CustomerTRN { get; set; } = "";
        public string CustomerAddress { get; set; } = "";
        public string ProjectName { get; set; } = "";
        public string ProjectLocation { get; set; } = "";
        public string LPONo { get; set; } = "";
        public string AttentionName { get; set; } = "";
        public string ContactNo { get; set; } = "";
        public DateTime InvoiceDate { get; set; } = DateTime.Now;
        public DateTime ValidUntil { get; set; } = DateTime.Now.AddDays(30);
        public string Status { get; set; } = "Draft";
        public double TotalAmount { get; set; }
        public double VATAmount { get; set; }
        public double NetAmount { get; set; }
        public string CompanyName { get; set; } = "";
        public string CompanyTRN { get; set; } = "";
        public string CompanyLocation { get; set; } = "";
        public string CompanyPhone { get; set; } = "";
        public string Notes { get; set; } = "";
        public string Salesman { get; set; } = "";

        // ✅ NEW: Missing properties
        public string JobOrderRef { get; set; } = "";
        public string CustomerReference { get; set; } = "";
        public string ProjectNo { get; set; } = "";
        public string Color { get; set; } = "";
        public bool IsConvertedToJobOrder { get; set; } = false;

        // ✅ NEW: Computed properties
        public double GrandTotal => NetAmount;
        public double TotalSQM => Items?.Sum(x => x.TotalSQM) ?? 0;
        public int TotalQty => Items?.Sum(x => x.Qty) ?? 0;

        public List<ProformaInvoiceItemModel> Items { get; set; } = new();
    }

    public class ProformaInvoiceItemModel
    {
        public int Id { get; set; }
        public int SrNo { get; set; } = 1;
        public string GlassRef { get; set; } = "";
        public double Width1 { get; set; }
        public double Height1 { get; set; }
        public double Width2 { get; set; }
        public double Height2 { get; set; }
        public int Qty { get; set; } = 1;
        public double SQM { get; set; }
        public double TotalSQM { get; set; }
        public double Price { get; set; }
        public double TotalPrice { get; set; }
        public double SurchargePercent { get; set; }
        public double SurchargeThreshold { get; set; }
    }

    // Job Order Models
    public class JobOrderModel
    {
        public int Id { get; set; }
        public string JONumber { get; set; } = "";
        public int ProformaInvoiceId { get; set; }
        public string PINumber { get; set; } = "";
        public string ClientName { get; set; } = "";
        public string ClientTRN { get; set; } = "";
        public string ClientAddress { get; set; } = "";
        public string ContactPerson { get; set; } = "";
        public string ContactNumber { get; set; } = "";
        public string ProjectName { get; set; } = "";
        public string ProjectLocation { get; set; } = "";
        public string LPONumber { get; set; } = "";
        public DateTime JODate { get; set; } = DateTime.Now;
        public DateTime RequiredDate { get; set; } = DateTime.Now.AddDays(7);
        public string Status { get; set; } = "Pending";
        public int TotalQty { get; set; }
        public int ReleasedQty { get; set; }
        public int BalanceQty { get; set; }
        public double TotalAmount { get; set; }
        public double VATAmount { get; set; }
        public double DiscountAmount { get; set; }
        public double NetAmount { get; set; }
        public string Notes { get; set; } = "";
        public string SpecificationsJson { get; set; } = "";
        public List<JobOrderItemModel> Items { get; set; } = new();
    }

    public class JobOrderItemModel
    {
        public int Id { get; set; }
        public int SrNo { get; set; } = 1;
        public string GlassRef { get; set; } = "";
        public double Width { get; set; }
        public double Height { get; set; }
        public int OrderedQty { get; set; }
        public int ReleasedQty { get; set; }
        public int BalanceQty { get; set; }
        public double Price { get; set; }
        public double TotalAmount { get; set; }
    }

    // Delivery Order Models
    public class DeliveryOrderModel
    {
        public int Id { get; set; }
        public string DONumber { get; set; } = "";
        public int JobOrderId { get; set; }
        public int ProformaInvoiceId { get; set; }
        public string ClientName { get; set; } = "";
        public string ProjectName { get; set; } = "";
        public DateTime DODate { get; set; } = DateTime.Now;
        public string Status { get; set; } = "Pending";
        public string VehicleNumber { get; set; } = "";
        public string DriverName { get; set; } = "";
        public string Notes { get; set; } = "";
        public int TotalQty { get; set; }
        public int DeliveredQty { get; set; }
        public List<DeliveryOrderItemModel> Items { get; set; } = new();
    }

    public class DeliveryOrderItemModel
    {
        public int Id { get; set; }
        public int SrNo { get; set; } = 1;
        public string GlassRef { get; set; } = "";
        public double Width { get; set; }
        public double Height { get; set; }
        public int DeliveredQty { get; set; }
    }

    // Tax Invoice Models
    public class TaxInvoiceModel
    {
        public int Id { get; set; }
        public string InvoiceNumber { get; set; } = "";
        public int ProformaInvoiceId { get; set; }
        public int JobOrderId { get; set; }
        public int DeliveryOrderId { get; set; }
        public string ClientName { get; set; } = "";
        public string ClientTRN { get; set; } = "";
        public string ClientAddress { get; set; } = "";
        public DateTime InvoiceDate { get; set; } = DateTime.Now;
        public DateTime DueDate { get; set; } = DateTime.Now.AddDays(30);
        public string Status { get; set; } = "Pending";
        public string PaymentStatus { get; set; } = "Unpaid";
        public double SubTotal { get; set; }
        public double VATPercent { get; set; } = 5;
        public double VATAmount { get; set; }
        public double TotalAmount { get; set; }
        public double PaidAmount { get; set; }
        public double BalanceAmount { get; set; }
        public string Notes { get; set; } = "";
        public List<TaxInvoiceItemModel> Items { get; set; } = new();
    }

    public class TaxInvoiceItemModel
    {
        public int Id { get; set; }
        public int SrNo { get; set; } = 1;
        public string Description { get; set; } = "";
        public int Qty { get; set; }
        public double UnitPrice { get; set; }
        public double TotalPrice { get; set; }
    }
}