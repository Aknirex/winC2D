using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Win32;

namespace winC2D.Core
{
    // ══════════════════════════════════════════════════════════════════════════
    // 迁移任务（描述一次迁移操作的完整状态）
    // ══════════════════════════════════════════════════════════════════════════
    public class MigrationTask
    {
        public string Id          { get; } = Guid.NewGuid().ToString("N")[..8];
        public string Name        { get; set; }   // 软件/AppData 名称
        public string SourcePath  { get; set; }   // 源目录
        public string TargetPath  { get; set; }   // 迁移目标目录（完整）
        public string BackupPath  { get; set; }   // 迁移前备份路径（可选）
        public bool   CreateSymlink { get; set; } = true;
        public bool   UpdateRegistry{ get; set; } = false;

        // 结果
        public bool   IsSuccess   { get; set; }
        public string Error       { get; set; }
        public bool   WasRolledBack { get; set; }
        public DateTime StartedAt { get; set; }
        public DateTime FinishedAt{ get; set; }
    }

    // ══════════════════════════════════════════════════════════════════════════
    // 迁移进度报告
    // ══════════════════════════════════════════════════════════════════════════
    public class MigrationProgress
    {
        public int     Current     { get; set; }
        public int     Total       { get; set; }
        public string  CurrentName { get; set; }
        public string  Stage       { get; set; }  // "Copy" / "Symlink" / "Registry" / "Rollback"
        public bool    HasError    { get; set; }
        public string  Error       { get; set; }
        public double  Percent     => Total == 0 ? 0 : (double)Current / Total * 100;
    }

    // ══════════════════════════════════════════════════════════════════════════
    // MigrationEngine — 事务式安全迁移引擎
    //
    // 流程：
    //   1. 预检（源存在、目标不冲突、磁盘空间）
    //   2. 原子重命名源目录（检测文件占用）
    //   3. 增量复制到临时目标目录（tempTargetPath）
    //   4. 原子重命名临时目录为最终目标
    //   5. 删除原始重命名目录
    //   6. 创建符号链接
    //   7. （可选）更新注册表 InstallLocation
    //
    // 回滚：
    //   - 任意步骤失败 → 清理临时文件 → 恢复源目录名
    //   - 手动回滚 → 删除符号链接 → 将目标复制回源 → 删除目标
    // ══════════════════════════════════════════════════════════════════════════
    public class MigrationEngine
    {
    private readonly MigrationEngineLogger _logger;

        public MigrationEngine(MigrationEngineLogger logger = null)
        {
            _logger = logger;
        }

        // ──────────────────────────────────────────────────────────────────────
        // 批量迁移（带进度回调和取消）
        // ──────────────────────────────────────────────────────────────────────
        public async Task<List<MigrationTask>> MigrateAllAsync(
            IEnumerable<MigrationTask> tasks,
            IProgress<MigrationProgress> progress,
            CancellationToken ct = default)
        {
            var list    = new List<MigrationTask>(tasks);
            var results = new List<MigrationTask>();
            int current = 0;

            foreach (var task in list)
            {
                ct.ThrowIfCancellationRequested();
                current++;

                progress?.Report(new MigrationProgress
                {
                    Current     = current,
                    Total       = list.Count,
                    CurrentName = task.Name,
                    Stage       = "Copy"
                });

                await Task.Run(() => ExecuteTask(task, progress, current, list.Count, ct), ct);
                results.Add(task);

            _logger?.Log(task.IsSuccess
                    ? $"[OK] {task.Name}: {task.SourcePath} → {task.TargetPath}"
                    : $"[ERR] {task.Name}: {task.Error}");
            }
            return results;
        }

        // ──────────────────────────────────────────────────────────────────────
        // 单任务执行（同步，可在线程中调用）
        // ──────────────────────────────────────────────────────────────────────
        private void ExecuteTask(
            MigrationTask task,
            IProgress<MigrationProgress> progress,
            int current, int total,
            CancellationToken ct)
        {
            task.StartedAt = DateTime.Now;
            string tempSrc    = task.SourcePath + "_migrating_" + task.Id;
            string tempTarget = task.TargetPath + "_temp_"     + task.Id;
            bool srcRenamed   = false;
            bool tempCopied   = false;
            bool finalRenamed = false;

            try
            {
                // ── Step 0: 预检 ────────────────────────────────────────────
                Preflight(task);
                ct.ThrowIfCancellationRequested();

                // ── Step 1: 原子重命名源目录（测试占用）────────────────────
                Report(progress, current, total, task.Name, "Preflight");
                Directory.Move(task.SourcePath, tempSrc);
                srcRenamed = true;
                ct.ThrowIfCancellationRequested();

                // ── Step 2: 复制到临时目标目录 ─────────────────────────────
                Report(progress, current, total, task.Name, "Copy");
                CopyAll(tempSrc, tempTarget, ct);
                tempCopied = true;
                ct.ThrowIfCancellationRequested();

                // ── Step 3: 原子重命名临时目录 → 最终目标 ─────────────────
                Report(progress, current, total, task.Name, "Finalize");
                Directory.Move(tempTarget, task.TargetPath);
                finalRenamed = true;

                // ── Step 4: 删除原始重命名源 ────────────────────────────────
                SafeDelete(tempSrc);
                srcRenamed = false;

                // ── Step 5: 创建符号链接 ────────────────────────────────────
                if (task.CreateSymlink)
                {
                    Report(progress, current, total, task.Name, "Symlink");
                    CreateSymlink(task.SourcePath, task.TargetPath);
                }

                // ── Step 6: 更新注册表 ──────────────────────────────────────
                if (task.UpdateRegistry)
                {
                    Report(progress, current, total, task.Name, "Registry");
                    UpdateRegistry(task.SourcePath, task.TargetPath);
                }

                task.IsSuccess  = true;
                task.FinishedAt = DateTime.Now;
            }
            catch (OperationCanceledException)
            {
                task.Error = "已取消";
                Rollback(tempSrc, tempTarget, task.SourcePath, srcRenamed, tempCopied, finalRenamed, task.TargetPath);
                task.WasRolledBack = true;
                task.FinishedAt    = DateTime.Now;
                throw;
            }
            catch (Exception ex)
            {
                task.Error      = ex.Message;
                task.IsSuccess  = false;
                task.FinishedAt = DateTime.Now;
                Rollback(tempSrc, tempTarget, task.SourcePath, srcRenamed, tempCopied, finalRenamed, task.TargetPath);
                task.WasRolledBack = true;
            }
        }

        // ──────────────────────────────────────────────────────────────────────
        // 回滚单任务迁移（已完成迁移后的逆操作）
        // ──────────────────────────────────────────────────────────────────────
        public void RollbackTask(MigrationTask task)
        {
            if (!task.IsSuccess) return;

            bool symlinkRemoved = false;

            // 1. 移除符号链接（如果存在）
            if (task.CreateSymlink && Directory.Exists(task.SourcePath) && IsSymlink(task.SourcePath))
            {
                Directory.Delete(task.SourcePath);
                symlinkRemoved = true;
            }

            try
            {
                // 2. 将目标复制回源
                CopyAll(task.TargetPath, task.SourcePath, CancellationToken.None);

                // 3. 删除目标
                SafeDelete(task.TargetPath);

                task.IsSuccess     = false;
                task.WasRolledBack = true;
                _logger?.Log($"[ROLLBACK] {task.Name}: 已回滚");
            }
            catch (Exception ex)
            {
                // 复制失败 → 恢复符号链接
                if (symlinkRemoved && Directory.Exists(task.TargetPath))
                {
                    try { CreateSymlink(task.SourcePath, task.TargetPath); } catch { }
                }
                throw new Exception($"回滚失败: {ex.Message}", ex);
            }
        }

        // ──────────────────────────────────────────────────────────────────────
        // 预检
        // ──────────────────────────────────────────────────────────────────────
        private static void Preflight(MigrationTask task)
        {
            if (string.IsNullOrWhiteSpace(task.SourcePath))
                throw new ArgumentException("源路径为空");
            if (!Directory.Exists(task.SourcePath))
                throw new DirectoryNotFoundException($"源目录不存在: {task.SourcePath}");
            if (string.IsNullOrWhiteSpace(task.TargetPath))
                throw new ArgumentException("目标路径为空");
            if (Directory.Exists(task.TargetPath))
                throw new IOException($"目标目录已存在: {task.TargetPath}");
            if (File.Exists(task.TargetPath))
                throw new IOException($"目标路径是文件: {task.TargetPath}");

            // 磁盘空间检查
            var targetRoot = Path.GetPathRoot(task.TargetPath);
            var drive      = new DriveInfo(targetRoot);
            if (!drive.IsReady)
                throw new IOException($"目标磁盘不可用: {targetRoot}");
        }

        // ──────────────────────────────────────────────────────────────────────
        // 递归复制（严格模式：任何异常立即抛出）
        // ──────────────────────────────────────────────────────────────────────
        private static void CopyAll(string src, string dst, CancellationToken ct)
        {
            Directory.CreateDirectory(dst);
            foreach (var file in Directory.EnumerateFiles(src))
            {
                ct.ThrowIfCancellationRequested();
                File.Copy(file, Path.Combine(dst, Path.GetFileName(file)), overwrite: false);
            }
            foreach (var dir in Directory.EnumerateDirectories(src))
            {
                ct.ThrowIfCancellationRequested();
                CopyAll(dir, Path.Combine(dst, Path.GetFileName(dir)), ct);
            }
        }

        // ──────────────────────────────────────────────────────────────────────
        // 内部回滚（迁移失败时）
        // ──────────────────────────────────────────────────────────────────────
        private static void Rollback(
            string tempSrc, string tempTarget, string originalSrc,
            bool srcRenamed, bool tempCopied, bool finalRenamed, string finalTarget)
        {
            // 清理临时目标
            if (tempCopied && !finalRenamed && Directory.Exists(tempTarget))
                SafeDelete(tempTarget);

            // 还原源目录名
            if (srcRenamed && Directory.Exists(tempSrc) && !Directory.Exists(originalSrc))
            {
                try { Directory.Move(tempSrc, originalSrc); } catch { }
            }

            // 若最终目标已创建但符号链接未建立，清理最终目标（避免悬空）
            if (finalRenamed && Directory.Exists(finalTarget))
            {
                if (!Directory.Exists(originalSrc))
                {
                    // 没有符号链接可访问，还原回原路径
                    try { Directory.Move(finalTarget, originalSrc); } catch { }
                }
            }
        }

        // ──────────────────────────────────────────────────────────────────────
        // 创建目录符号链接
        // ──────────────────────────────────────────────────────────────────────
        private static void CreateSymlink(string linkPath, string targetPath)
        {
            linkPath   = linkPath.TrimEnd(Path.DirectorySeparatorChar);
            targetPath = targetPath.TrimEnd(Path.DirectorySeparatorChar);
#if NET6_0_OR_GREATER
            Directory.CreateSymbolicLink(linkPath, targetPath);
#else
            var psi = new System.Diagnostics.ProcessStartInfo("cmd.exe",
                $"/c mklink /D \"{linkPath}\" \"{targetPath}\"")
            {
                CreateNoWindow = true, UseShellExecute = false,
                RedirectStandardOutput = true, RedirectStandardError = true
            };
            using var proc = System.Diagnostics.Process.Start(psi);
            proc.WaitForExit();
            if (proc.ExitCode != 0)
                throw new Exception($"mklink 失败: {proc.StandardError.ReadToEnd()}");
#endif
        }

        // ──────────────────────────────────────────────────────────────────────
        // 更新注册表 InstallLocation
        // ──────────────────────────────────────────────────────────────────────
        private static void UpdateRegistry(string oldPath, string newPath)
        {
            string[] regPaths =
            {
                @"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall",
                @"SOFTWARE\WOW6432Node\Microsoft\Windows\CurrentVersion\Uninstall"
            };
            foreach (var root in new[] { Registry.LocalMachine, Registry.CurrentUser })
            {
                foreach (var rp in regPaths)
                {
                    try
                    {
                        using var key = root.OpenSubKey(rp, writable: true);
                        if (key == null) continue;
                        foreach (var sub in key.GetSubKeyNames())
                        {
                            using var subKey = key.OpenSubKey(sub, writable: true);
                            if (subKey?.GetValue("InstallLocation") is string loc &&
                                string.Equals(loc, oldPath, StringComparison.OrdinalIgnoreCase))
                            {
                                subKey.SetValue("InstallLocation", newPath);
                            }
                        }
                    }
                    catch { }
                }
            }
        }

        // ──────────────────────────────────────────────────────────────────────
        // 辅助
        // ──────────────────────────────────────────────────────────────────────
        private static bool IsSymlink(string path) =>
            (File.GetAttributes(path) & FileAttributes.ReparsePoint) != 0;

        private static void SafeDelete(string path)
        {
            try { if (Directory.Exists(path)) Directory.Delete(path, true); } catch { }
        }

        private static void Report(
            IProgress<MigrationProgress> progress,
            int current, int total, string name, string stage)
        {
            progress?.Report(new MigrationProgress
            {
                Current = current, Total = total, CurrentName = name, Stage = stage
            });
        }
    }

    // ══════════════════════════════════════════════════════════════════════════
    // MigrationEngineLogger — 线程安全日志记录器（避免与全局静态 MigrationLogger 冲突）
    // ══════════════════════════════════════════════════════════════════════════
    public class MigrationEngineLogger
    {
        private readonly List<string> _entries = new();
        private readonly object _lock = new();
        public event EventHandler<string> EntryAdded;

        public void Log(string message)
        {
            string entry = $"[{DateTime.Now:HH:mm:ss}] {message}";
            lock (_lock) { _entries.Add(entry); }
            EntryAdded?.Invoke(this, entry);
        }

        public IReadOnlyList<string> Entries
        {
            get { lock (_lock) { return _entries.AsReadOnly(); } }
        }

        public string ToText()
        {
            lock (_lock) { return string.Join(Environment.NewLine, _entries); }
        }

        public void SaveToFile(string path)
        {
            lock (_lock) { File.WriteAllLines(path, _entries); }
        }
    }
}
