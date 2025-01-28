using Haihv.Elis.Tool.TraCuuGcn.Models;
using LanguageExt.Common;

namespace Haihv.Elis.Tool.TraCuuGcn.Web_Api.Data;

public interface ISearchService
{
    Task<Result<(GiayChungNhan?, MaQrInfo?)>> GetResultAsync(string? query);
}