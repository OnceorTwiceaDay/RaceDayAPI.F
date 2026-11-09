using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using RaceDay.Api.Data;

#nullable disable

namespace RaceDay.Api.Migrations;

[DbContext(typeof(RaceDayDbContext))]
[Migration("20261109190000_AddPart3Media")]
public partial class AddPart3Media : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<string>("ProfileImageUrl", "Users", type: "nvarchar(300)", maxLength: 300, nullable: true);
        migrationBuilder.AddColumn<string>("BannerImageUrl", "Events", type: "nvarchar(300)", maxLength: 300, nullable: true);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(name: "ProfileImageUrl", table: "Users");
        migrationBuilder.DropColumn(name: "BannerImageUrl", table: "Events");
    }
}
