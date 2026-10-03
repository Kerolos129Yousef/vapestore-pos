using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.Globalization;
using VapeShopPos.Data;
using VapeShopPos.Models;

namespace VapeShopPos.Services
{
    /// <summary>
    /// Manual drawer cash movements (deposit/withdraw), e.g. for money-transfer
    /// services. These adjust the expected cash of the shift they belong to.
    /// </summary>
    public static class CashMovementService
    {
        public static CashMovement Record(long shiftId, long userId, bool isDeposit, decimal amount, string service)
        {
            using (var conn = Database.GetConnection())
            {
                Database.Execute(conn,
                    @"INSERT INTO cash_movements (datetime, direction, amount, service, shift_id, user_id)
                      VALUES (@dt, @dir, @amt, @svc, @shift, @uid);",
                    p =>
                    {
                        p.AddWithValue("@dt", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
                        p.AddWithValue("@dir", isDeposit ? CashDirections.In : CashDirections.Out);
                        p.AddWithValue("@amt", amount);
                        p.AddWithValue("@svc", string.IsNullOrWhiteSpace(service) ? (object)DBNull.Value : service.Trim());
                        p.AddWithValue("@shift", shiftId);
                        p.AddWithValue("@uid", userId);
                    });
                return GetById(conn.LastInsertRowId);
            }
        }

        public static CashMovement GetById(long id)
        {
            using (var conn = Database.GetConnection())
            using (var cmd = new SQLiteCommand(
                @"SELECT c.id, c.datetime, c.direction, c.amount, c.service, c.shift_id, c.user_id, u.username
                  FROM cash_movements c LEFT JOIN users u ON u.id = c.user_id
                  WHERE c.id=@id;", conn))
            {
                cmd.Parameters.AddWithValue("@id", id);
                using (var r = cmd.ExecuteReader())
                    return r.Read() ? Read(r) : null;
            }
        }

        /// <summary>Net cash effect of all movements in a shift: deposits minus withdrawals.</summary>
        public static decimal GetShiftNet(long shiftId)
        {
            using (var conn = Database.GetConnection())
            using (var cmd = new SQLiteCommand(
                @"SELECT COALESCE(SUM(CASE WHEN direction='in' THEN amount ELSE -amount END),0)
                  FROM cash_movements WHERE shift_id=@id;", conn))
            {
                cmd.Parameters.AddWithValue("@id", shiftId);
                return Convert.ToDecimal(cmd.ExecuteScalar());
            }
        }

        public static decimal GetShiftTotal(long shiftId, bool deposits)
        {
            using (var conn = Database.GetConnection())
            using (var cmd = new SQLiteCommand(
                @"SELECT COALESCE(SUM(amount),0) FROM cash_movements WHERE shift_id=@id AND direction=@dir;", conn))
            {
                cmd.Parameters.AddWithValue("@id", shiftId);
                cmd.Parameters.AddWithValue("@dir", deposits ? CashDirections.In : CashDirections.Out);
                return Convert.ToDecimal(cmd.ExecuteScalar());
            }
        }

        public static List<CashMovement> GetShiftMovements(long shiftId)
        {
            var list = new List<CashMovement>();
            using (var conn = Database.GetConnection())
            using (var cmd = new SQLiteCommand(
                @"SELECT c.id, c.datetime, c.direction, c.amount, c.service, c.shift_id, c.user_id, u.username
                  FROM cash_movements c LEFT JOIN users u ON u.id = c.user_id
                  WHERE c.shift_id=@id ORDER BY c.id DESC;", conn))
            {
                cmd.Parameters.AddWithValue("@id", shiftId);
                using (var r = cmd.ExecuteReader())
                    while (r.Read())
                        list.Add(Read(r));
            }
            return list;
        }

        private static CashMovement Read(SQLiteDataReader r)
        {
            return new CashMovement
            {
                Id = r.GetInt64(0),
                DateTime = DateTime.Parse(r.GetString(1), CultureInfo.InvariantCulture),
                Direction = r.GetString(2),
                Amount = Convert.ToDecimal(r.GetValue(3)),
                Service = r.IsDBNull(4) ? "" : r.GetString(4),
                ShiftId = r.GetInt64(5),
                UserId = r.GetInt64(6),
                UserName = r.IsDBNull(7) ? "-" : r.GetString(7)
            };
        }
    }
}
