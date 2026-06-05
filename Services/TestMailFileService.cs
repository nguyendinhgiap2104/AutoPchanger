using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using AutoTool.Models;

namespace AutoTool.Services
{
    public class TestMailFileService
    {
        private readonly object _lock = new object();

        public bool TryGetNextMail(
            string filePath,
            Action<string> log,
            out TestMailAccount account)
        {
            account = null;

            lock (_lock)
            {
                try
                {
                    if (string.IsNullOrWhiteSpace(filePath))
                    {
                        log?.Invoke("❌ Chưa chọn file mail test.");
                        return false;
                    }

                    if (!File.Exists(filePath))
                    {
                        log?.Invoke($"❌ File mail test không tồn tại: {filePath}");
                        return false;
                    }

                    List<string> lines = File.ReadAllLines(filePath)
                        .Select(x => x.Trim())
                        .Where(x => !string.IsNullOrWhiteSpace(x))
                        .Where(x => !x.StartsWith("#"))
                        .ToList();

                    if (lines.Count == 0)
                    {
                        log?.Invoke("❌ File mail test đã hết mail.");
                        return false;
                    }

                    string selectedLine = lines[0];
                    string[] parts = selectedLine.Split('|');

                    if (parts.Length < 2)
                    {
                        log?.Invoke($"❌ Dòng mail sai format: {selectedLine}");
                        return false;
                    }

                    account = new TestMailAccount
                    {
                        Email = parts[0].Trim(),
                        Password = parts[1].Trim(),
                        RecoveryEmail = parts.Length >= 3 ? parts[2].Trim() : ""
                    };

                    if (!account.IsValid())
                    {
                        log?.Invoke($"❌ Mail test không hợp lệ: {selectedLine}");
                        return false;
                    }

                    // Xóa dòng đã lấy khỏi file gốc để tránh dùng lại.
                    lines.RemoveAt(0);
                    File.WriteAllLines(filePath, lines);

                    // Lưu lại lịch sử mail đã dùng.
                    string usedFilePath = Path.Combine(
                        Path.GetDirectoryName(filePath),
                        Path.GetFileNameWithoutExtension(filePath) + "_used.txt"
                    );

                    File.AppendAllText(
                        usedFilePath,
                        selectedLine + Environment.NewLine
                    );

                    log?.Invoke($"✅ Đã lấy mail test từ file: {account.Email}");
                    return true;
                }
                catch (Exception ex)
                {
                    log?.Invoke($"❌ Lỗi đọc file mail test: {ex.Message}");
                    return false;
                }
            }
        }
    }
}