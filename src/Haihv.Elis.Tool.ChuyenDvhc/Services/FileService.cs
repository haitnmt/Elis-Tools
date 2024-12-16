namespace Haihv.Elis.Tool.ChuyenDvhc.Services;

public class FileService : IFileService
{
    public async Task<string> ReadFileAsync(string filePath)
    {
        try
        {
            if (!File.Exists(filePath))
            {
                Console.WriteLine("File not found, creating new file");
                return string.Empty;
            }

            var stream = File.OpenRead(filePath);
            using var reader = new StreamReader(stream);
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
            
            await using var stream = File.Exists(filePath) ? 
                File.OpenWrite(filePath) :
                File.Create(filePath);
            await using var writer = new StreamWriter(stream);
            await writer.WriteAsync(content);
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