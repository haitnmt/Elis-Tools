namespace Haihv.Elis.Tool.TraCuuGcn.Models;

public class ChuSuDungInfo(ChuSuDung chuSuDung, ChuSuDungQuanHe? chuSuDungQuanHe)
{
    public ChuSuDung ChuSuDung { get; set; } = chuSuDung;
    public ChuSuDungQuanHe? ChuSuDungQuanHe { get; set; } = chuSuDungQuanHe;
}