using System;
using System.Collections.Generic;
using System.Data.SQLite;
using VapeShopPos.Data;
using VapeShopPos.Models;

namespace VapeShopPos.Services
{
    public static class UserService
    {
        public static User Authenticate(string username, string password)
        {
            if (string.IsNullOrWhiteSpace(username)) return null;
            User u = GetByUsername(username.Trim());
            if (u == null) return null;
            return PasswordHasher.Verify(password, u.PasswordHash) ? u : null;
        }

        public static User GetByUsername(string username)
        {
            using (var conn = Database.GetConnection())
            using (var cmd = new SQLiteCommand(
                "SELECT id, username, password_hash, role FROM users WHERE username=@u LIMIT 1;", conn))
            {
                cmd.Parameters.AddWithValue("@u", username);
                using (var r = cmd.ExecuteReader())
                    return r.Read() ? ReadUser(r) : null;
            }
        }

        public static List<User> GetUsers()
        {
            var list = new List<User>();
            using (var conn = Database.GetConnection())
            using (var cmd = new SQLiteCommand(
                "SELECT id, username, password_hash, role FROM users ORDER BY username;", conn))
            using (var r = cmd.ExecuteReader())
                while (r.Read())
                    list.Add(ReadUser(r));
            return list;
        }

        public static long AddUser(string username, string password, string role)
        {
            using (var conn = Database.GetConnection())
            {
                Database.Execute(conn,
                    "INSERT INTO users (username, password_hash, role) VALUES (@u, @h, @r);",
                    p =>
                    {
                        p.AddWithValue("@u", username.Trim());
                        p.AddWithValue("@h", PasswordHasher.Hash(password));
                        p.AddWithValue("@r", role);
                    });
                return conn.LastInsertRowId;
            }
        }

        /// <summary>Updates username/role, and the password only if a new one is given.</summary>
        public static void UpdateUser(long id, string username, string role, string newPasswordOrNull)
        {
            using (var conn = Database.GetConnection())
            {
                Database.Execute(conn,
                    "UPDATE users SET username=@u, role=@r WHERE id=@id;",
                    p =>
                    {
                        p.AddWithValue("@u", username.Trim());
                        p.AddWithValue("@r", role);
                        p.AddWithValue("@id", id);
                    });

                if (!string.IsNullOrEmpty(newPasswordOrNull))
                    Database.Execute(conn,
                        "UPDATE users SET password_hash=@h WHERE id=@id;",
                        p =>
                        {
                            p.AddWithValue("@h", PasswordHasher.Hash(newPasswordOrNull));
                            p.AddWithValue("@id", id);
                        });
            }
        }

        public static void DeleteUser(long id)
        {
            using (var conn = Database.GetConnection())
                Database.Execute(conn, "DELETE FROM users WHERE id=@id;",
                    p => p.AddWithValue("@id", id));
        }

        public static int CountManagers()
        {
            using (var conn = Database.GetConnection())
            using (var cmd = new SQLiteCommand(
                "SELECT COUNT(*) FROM users WHERE role=@r;", conn))
            {
                cmd.Parameters.AddWithValue("@r", Roles.Manager);
                return Convert.ToInt32(cmd.ExecuteScalar());
            }
        }

        private static User ReadUser(SQLiteDataReader r)
        {
            return new User
            {
                Id = r.GetInt64(0),
                Username = r.GetString(1),
                PasswordHash = r.GetString(2),
                Role = r.GetString(3)
            };
        }
    }
}
