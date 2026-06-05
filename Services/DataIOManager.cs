using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace AutoTool.Services
{
    public static class DataIOManager
    {
        /// <summary>
        /// Hàm ghi file Log chuẩn
        /// </summary>
        public static void SaveLogToFile(string folderPath, string fileName, string content)
        {
            try
            {
                if (!Directory.Exists(folderPath))
                    Directory.CreateDirectory(folderPath);

                string filePath = Path.Combine(folderPath, fileName);
                File.AppendAllText(filePath, $"[{DateTime.Now:HH:mm:ss}] {content}\r\n", Encoding.UTF8);
            }
            catch { }
        }

        /// <summary>
        /// Hàm xuất dữ liệu ra file CSV
        /// </summary>
        public static bool ExportToCsv(string targetFolderPath, string fileName, List<string> headers, List<List<string>> rowData)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(targetFolderPath))
                    targetFolderPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Export");

                if (!Directory.Exists(targetFolderPath))
                    Directory.CreateDirectory(targetFolderPath);

                string fullFilePath = Path.Combine(targetFolderPath, fileName);
                var sb = new StringBuilder();

                // Ghi Headers
                sb.AppendLine(string.Join(",", headers.ConvertAll(EscapeCsvValue)));

                // Ghi Rows
                foreach (var row in rowData)
                {
                    sb.AppendLine(string.Join(",", row.ConvertAll(EscapeCsvValue)));
                }

                // Dùng UTF8 BOM để Excel hiển thị tiếng Việt chuẩn
                File.WriteAllText(fullFilePath, sb.ToString(), new UTF8Encoding(true));
                return true;
            }
            catch
            {
                return false;
            }
        }

        private static string EscapeCsvValue(string value)
        {
            if (string.IsNullOrEmpty(value)) return "";

            if (value.Contains(",") || value.Contains("\"") || value.Contains("\n") || value.Contains("\r"))
            {
                value = value.Replace("\"", "\"\"");
                return $"\"{value}\"";
            }
            return value;
        }
    }
}