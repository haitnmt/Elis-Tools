---------------------------------------------------------------------------------------------------------------------------------------------------
SET ANSI_NULLS ON
SET QUOTED_IDENTIFIER ON
CREATE TABLE [dbo].[DonViInGCN]([Ma] [int] NOT NULL,
    [KyHieu] [nvarchar](50) NOT NULL,
    [MaDinhDanh] [nvarchar](30) NOT NULL,
    [Ten] [nvarchar](500) NULL,
    [GhiChu] [nvarchar](500) NULL,
    CONSTRAINT [PK_DonViInGCN] PRIMARY KEY CLUSTERED ([Ma] ASC)WITH (PAD_INDEX = OFF,
                                                                   STATISTICS_NORECOMPUTE = OFF,
                                                                   IGNORE_DUP_KEY = OFF,
                                                                   ALLOW_ROW_LOCKS = ON,
                                                                   ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]) ON [PRIMARY] 

DROP procedure [dbo].[spLayTatCaDonViInGCN]
CREATE PROC [dbo].[spLayTatCaDonViInGCN] AS BEGIN
select *
from DonViInGCN END
INSERT [dbo].[DonViInGCN] ([Ma],
[KyHieu],
[MaDinhDanh],
[Ten],
[GhiChu])
VALUES (1,
    N'STNMT',
    N'H05.16    ',
    N'Sở Tài nguyên và Môi trường',
    N'')
INSERT [dbo].[DonViInGCN] ([Ma],
[KyHieu],
[MaDinhDanh],
[Ten],
[GhiChu])
VALUES (2,
    N'VPĐKT',
    N'H05.16.11',
    N'Văn phòng Đăng ký đất đai tỉnh Bắc Ninh',
    N'')
INSERT [dbo].[DonViInGCN] ([Ma],
[KyHieu],
[MaDinhDanh],
[Ten],
[GhiChu])
VALUES (3,
    N'CNBacNinh',
    N'H05.16.11.5',
    N'Chi nhánh Văn phòng Đăng ký Đất đai thành phố Bắc Ninh',
    N'')
INSERT [dbo].[DonViInGCN] ([Ma],
[KyHieu],
[MaDinhDanh],
[Ten],
[GhiChu])
VALUES (4,
    N'CNTuSon',
    N'H05.16.11.6',
    N'Chi nhánh Văn phòng đăng ký đất đai thành phố Từ Sơn',
    N'')
INSERT [dbo].[DonViInGCN] ([Ma],
[KyHieu],
[MaDinhDanh],
[Ten],
[GhiChu])
VALUES (5,
    N'CNTienDu',
    N'H05.16.11.7',
    N'Chi nhánh Văn phòng đăng ký đất đai huyện Tiên Du',
    NULL)
INSERT [dbo].[DonViInGCN] ([Ma],
[KyHieu],
[MaDinhDanh],
[Ten],
[GhiChu])
VALUES (6,
    N'CNYenPhong',
    N'H05.16.11.8',
    N'Chi nhánh Văn phòng đăng ký đất đai huyện Yên Phong',
    NULL)
INSERT [dbo].[DonViInGCN] ([Ma],
[KyHieu],
[MaDinhDanh],
[Ten],
[GhiChu])
VALUES (7,
    N'CNQueVo',
    N'H05.16.11.9',
    N'Chi nhánh Văn phòng đăng ký đất đai huyện Quế Võ',
    NULL)
INSERT [dbo].[DonViInGCN] ([Ma],
[KyHieu],
[MaDinhDanh],
[Ten],
[GhiChu])
VALUES (8,
    N'CNThuanThanh',
    N'H05.16.11.10',
    N'Chi nhánh Văn phòng đăng ký đất đai huyện Thuận Thành',
    NULL)
INSERT [dbo].[DonViInGCN] ([Ma],
[KyHieu],
[MaDinhDanh],
[Ten],
[GhiChu])
VALUES (9,
    N'CNGiaBinh',
    N'H05.16.11.11',
    N'Chi nhánh Văn phòng đăng ký đất đai Gia Bình',
    NULL)
INSERT [dbo].[DonViInGCN] ([Ma],
[KyHieu],
[MaDinhDanh],
[Ten],
[GhiChu])
VALUES (10,
    N'CNLuongTai',
    N'H05.16.11.12',
    N'Chi nhánh Văn phòng đăng ký đất đai huyện Lương Tài',
    NULL)