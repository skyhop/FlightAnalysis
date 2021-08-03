using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace PersistentFlightContext.Migrations
{
    public partial class initial : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Flights",
                columns: table => new
                {
                    Id = table.Column<Guid>(nullable: false),
                    Aircraft = table.Column<string>(nullable: true),
                    LastSeen = table.Column<DateTime>(nullable: true),
                    StartTime = table.Column<DateTime>(nullable: true),
                    DepartureHeading = table.Column<short>(nullable: false),
                    DepartureLocation = table.Column<string>(nullable: true),
                    DepartureInfoFound = table.Column<bool>(nullable: true),
                    EndTime = table.Column<DateTime>(nullable: true),
                    ArrivalHeading = table.Column<short>(nullable: false),
                    ArrivalLocation = table.Column<string>(nullable: true),
                    ArrivalInfoFound = table.Column<bool>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Flights", x => x.Id);
                });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Flights");
        }
    }
}
