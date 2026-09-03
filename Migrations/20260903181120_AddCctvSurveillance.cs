using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ADHUNIK_BARI.Migrations
{
    /// <inheritdoc />
    public partial class AddCctvSurveillance : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
IF OBJECT_ID(N'[CctvCameras]', N'U') IS NULL
BEGIN
    CREATE TABLE [CctvCameras] (
        [CameraId] int NOT NULL IDENTITY,
        [CameraName] nvarchar(100) NOT NULL,
        [Location] nvarchar(100) NOT NULL,
        [StreamUrl] nvarchar(1000) NOT NULL,
        [Status] nvarchar(50) NOT NULL CONSTRAINT [DF_CctvCameras_Status] DEFAULT (N'Online'),
        [CreatedAt] datetime2 NOT NULL CONSTRAINT [DF_CctvCameras_CreatedAt] DEFAULT (SYSUTCDATETIME()),
        CONSTRAINT [PK_CctvCameras] PRIMARY KEY ([CameraId])
    );
END
");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CctvCameras");
        }
    }
}
