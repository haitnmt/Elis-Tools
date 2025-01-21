namespace Haihv.Elis.Tool.TraCuuGcn.Models;

public record ChuSuDung(string Ten, string? GiayTo, string? DiaChi, string? QuocTich);

public record ChuSuDungQuanHe(string Ten, string? GiayTo, string? DiaChi, string QuocTich);

public class ChuSuDungInfo(ChuSuDung chuSuDung, ChuSuDungQuanHe? chuSuDungQuanHe)
{
    public ChuSuDung ChuSuDung { get; set; } = chuSuDung;
    public ChuSuDungQuanHe? ChuSuDungQuanHe { get; set; } = chuSuDungQuanHe;
}