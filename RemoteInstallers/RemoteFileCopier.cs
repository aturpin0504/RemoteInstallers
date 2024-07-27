using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace RemoteInstallers
{
    public class RemoteFileCopier
    {
        public static async Task<CopyResult> CopyFileAsync(string sourceFilePath, string pcAddress, string destinationPath)
        {
            if (!File.Exists(sourceFilePath))
            {
                return new CopyResult { Success = false, Message = $"Source file does not exist: {sourceFilePath}" };
            }

            string remotePath = GetRemotePath(pcAddress, destinationPath);
            try
            {
                await Task.Run(() => File.Copy(sourceFilePath, remotePath, true));
                return new CopyResult { Success = true, Message = "File copied successfully" };
            }
            catch (Exception ex)
            {
                return new CopyResult { Success = false, Message = $"Failed to copy file: {ex.Message}" };
            }
        }

        public static async Task<CopyResult> CopyFilesAsync(List<string> sourceFilePaths, string pcAddress, string destinationPath)
        {
            List<Task> copyTasks = new List<Task>();

            foreach (string sourceFilePath in sourceFilePaths)
            {
                if (!File.Exists(sourceFilePath))
                {
                    return new CopyResult { Success = false, Message = $"Source file does not exist: {sourceFilePath}" };
                }

                string remotePath = GetRemotePath(pcAddress, destinationPath);
                string fileName = Path.GetFileName(sourceFilePath);
                string destinationFilePath = Path.Combine(remotePath, fileName);

                copyTasks.Add(Task.Run(() => File.Copy(sourceFilePath, destinationFilePath, true)));
            }

            try
            {
                await Task.WhenAll(copyTasks);
                return new CopyResult { Success = true, Message = "All files copied successfully" };
            }
            catch (Exception ex)
            {
                return new CopyResult { Success = false, Message = $"Failed to copy files: {ex.Message}" };
            }
        }

        public static async Task<CopyResult> CopyDirectoryAsync(string sourceDirectoryPath, string pcAddress, string destinationPath)
        {
            if (!Directory.Exists(sourceDirectoryPath))
            {
                return new CopyResult { Success = false, Message = $"Source directory does not exist: {sourceDirectoryPath}" };
            }

            string remotePath = GetRemotePath(pcAddress, destinationPath);

            if (!Directory.Exists(remotePath))
            {
                Directory.CreateDirectory(remotePath);
            }

            List<Task> copyTasks = new List<Task>();

            foreach (string sourceFilePath in Directory.GetFiles(sourceDirectoryPath, "*", SearchOption.AllDirectories))
            {
                if (!File.Exists(sourceFilePath))
                {
                    return new CopyResult { Success = false, Message = $"Source file does not exist: {sourceFilePath}" };
                }

                string relativePath = sourceFilePath.Substring(sourceDirectoryPath.Length + 1);
                string destinationFilePath = Path.Combine(remotePath, relativePath);

                string destinationDir = Path.GetDirectoryName(destinationFilePath);
                if (!Directory.Exists(destinationDir))
                {
                    Directory.CreateDirectory(destinationDir);
                }

                copyTasks.Add(Task.Run(() => File.Copy(sourceFilePath, destinationFilePath, true)));
            }

            try
            {
                await Task.WhenAll(copyTasks);
                return new CopyResult { Success = true, Message = "Directory copied successfully" };
            }
            catch (Exception ex)
            {
                return new CopyResult { Success = false, Message = $"Failed to copy directory: {ex.Message}" };
            }
        }

        private static string GetRemotePath(string pcAddress, string destinationPath)
        {
            return $@"\\{pcAddress}\{destinationPath[0]}${destinationPath.Substring(2)}";
        }

        public class CopyResult
        {
            public bool Success { get; set; }
            public string Message { get; set; }
        }
    }
}
