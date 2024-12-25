namespace Haihv.Elis.Tool.ChuyenDvhc.Services;

public interface IFileService
{
    Task<string> ReadFileAsync(string filePath);
    Task WriteFileAsync(string filePath, string content);
    Task<bool> CreateFileAsync(string filePath);
    Task<bool> DeleteFileAsync(string filePath);
    Task<bool> FileExistsAsync(string filePath);
}