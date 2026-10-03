using System;
using System.IO;
using VapeShopPos.Data;

namespace VapeShopPos.Services
{
    public static class BackupService
    {
        /// <summary>
        /// Copies the live .db file to a timestamped file in a "Backups" folder next
        /// to the executable. Returns the full path of the created backup.
        /// </summary>
        public static string Backup()
        {
            string dbPath = Database.DatabasePath;
            if (string.IsNullOrEmpty(dbPath) || !File.Exists(dbPath))
                throw new FileNotFoundException("لم يتم العثور على ملف قاعدة البيانات.", dbPath);

            string backupDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Backups");
            Directory.CreateDirectory(backupDir);

            string stamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
            string name = Path.GetFileNameWithoutExtension(dbPath) + "_" + stamp +
                          Path.GetExtension(dbPath);
            string dest = Path.Combine(backupDir, name);

            File.Copy(dbPath, dest, overwrite: false);
            return dest;
        }
    }
}
