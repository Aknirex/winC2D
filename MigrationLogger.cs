using System;
using System.Collections.Generic;
using System.IO;

namespace winC2D
{
    public class MigrationLogEntry
    {
        public DateTime Time { get; set; }
        public string SoftwareName { get; set; }
        public string OldPath { get; set; }
        public string NewPath { get; set; }
        public string Status { get; set; } // Success/Fail
        public string Message { get; set; }
    }

    public static class MigrationLogger
    {
        private static readonly string logFile = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "migration.log");

        public static void Log(MigrationLogEntry entry)
        {
            string line = $"{entry.Time:yyyy-MM-dd HH:mm:ss}\t{entry.SoftwareName}\t{entry.OldPath}\t{entry.NewPath}\t{entry.Status}\t{entry.Message}";
            File.AppendAllLines(logFile, new[] { line });
        }

        public static List<MigrationLogEntry> ReadAll()
        {
            var list = new List<MigrationLogEntry>();
            if (!File.Exists(logFile)) return list;
            foreach (var line in File.ReadAllLines(logFile))
            {
                var parts = line.Split('\t');
                if (parts.Length >= 6)
                {
                    list.Add(new MigrationLogEntry
                    {
                        Time = DateTime.Parse(parts[0]),
                        SoftwareName = parts[1],
                        OldPath = parts[2],
                        NewPath = parts[3],
                        Status = parts[4],
                        Message = parts[5]
                    });
                }
            }
            return list;
        }
    }
}