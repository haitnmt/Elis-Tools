﻿namespace Haihv.Elis.Tool.ChuyenDvhc.Settings;

public static class CacheThamSoDuLieu
{
    private const string RootKey = "ThamSo:DuLieu:";
    public static string ToBanDoCu => RootKey + "ToBanDoCu";
    public static string GhiChuToBanDo => RootKey + "GhiChuToBanDo";
    public static string GhiChuThuaDat => RootKey + "GhiChuThuaDat";
    public static string GhiChuGiayChungNhan => RootKey + "GhiChuGiayChungNhan";
}

public static class CacheThamSoDvhc
{
    private const string RootKey = "ThamSo:Dvhc:";
    public static string CapTinhTruoc => RootKey + "CapTinhTruoc";
    public static string CapTinhSau => RootKey + "CapTinhSau";
    public static string CapHuyenTruoc => RootKey + "CapHuyenTruoc";
    public static string CapHuyenSau => RootKey + "CapHuyenSau";
    public static string CapXaTruoc => RootKey + "CapXaTruoc";
    public static string CapXaSau => RootKey + "CapXaSau";
    public static string NgaySatNhap => RootKey + "NgaySatNhap";
    public static string TenDvhcSau => RootKey + "DvhcMoi";
}

public static class CacheThamSoBanDo
{
    private const string RootKey = "ThamSo:BanDo:";
    public static string ThamChieuToBanDo => RootKey + "ThamChieuToBanDo";
}