using Microsoft.EntityFrameworkCore.Migrations;

namespace evi_app.Data.Migrations
{
    public partial class changeduploadedcert : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "FilePath",
                table: "UploadedCertificates",
                type: "nvarchar(max)",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "FilePath",
                table: "UploadedCertificates");
        }
    }
}
