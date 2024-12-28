using System.Text;

namespace Haihv.Elis.Tool.ChuyenDvhc.Services;

public class FileService : IFileService
{
    public async Task<string> ReadFileAsync(string filePath)
    {
        try
        {
            if (!File.Exists(filePath))
            {
                Console.WriteLine("File not found!");
                return string.Empty;
            }

            // Sử dụng using để đảm bảo giải phóng tài nguyên
            await using var fileStream = new FileStream(
                filePath,
                FileMode.Open,
                FileAccess.Read,
                FileShare.Read,
                bufferSize: 4096,
                useAsync: true);

            // Sử dụng UTF8 encoding để đọc chính xác
            using var reader = new StreamReader(
                fileStream,
                new UTF8Encoding(encoderShouldEmitUTF8Identifier: false),
                detectEncodingFromByteOrderMarks: true,
                bufferSize: 4096,
                leaveOpen: false);

            return await reader.ReadToEndAsync();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error reading file: {ex.Message}");
            return string.Empty;
        }
    }

    public async Task WriteFileAsync(string filePath, string content)
    {
        try
        {
            // Đảm bảo directory tồn tại
            var directory = Path.GetDirectoryName(filePath);
            if (!string.IsNullOrEmpty(directory))
            {
                Directory.CreateDirectory(directory);
            }

            // Sử dụng FileMode.Create để ghi đè file cũ nếu tồn tại
            await using var fileStream = new FileStream(
                filePath,
                FileMode.Create,
                FileAccess.Write,
                FileShare.None,
                bufferSize: 4096,
                useAsync: true);

            // Sử dụng UTF8 encoding không có BOM
            await using var writer = new StreamWriter(
                fileStream,
                new UTF8Encoding(encoderShouldEmitUTF8Identifier: false),
                bufferSize: 4096,
                leaveOpen: false);
            await writer.WriteAsync(content);
            await writer.FlushAsync(); // Đảm bảo dữ liệu được ghi xuống disk
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error writing file: {ex.Message}");
            throw;
        }
    }

    public async Task<bool> CreateFileAsync(string filePath)
    {
        try
        {
            if (File.Exists(filePath)) return false;
            await using var file = File.Create(filePath);
            return true;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error creating file: {ex.Message}");
            throw;
        }
    }

    public Task<bool> DeleteFileAsync(string filePath)
    {
        try
        {
            if (!File.Exists(filePath)) return Task.FromResult(false);
            File.Delete(filePath);
            return Task.FromResult(true);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error deleting file: {ex.Message}");
            throw;
        }
    }

    public Task<bool> FileExistsAsync(string filePath)
    {
        return Task.FromResult(File.Exists(filePath));
    }
}