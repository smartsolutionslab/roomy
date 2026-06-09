using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SmartSolutionsLab.Roomy.Organization.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "companies",
                columns: table => new
                {
                    identifier = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_companies", x => x.identifier);
                });

            migrationBuilder.CreateTable(
                name: "offices",
                columns: table => new
                {
                    identifier = table.Column<Guid>(type: "uuid", nullable: false),
                    company_identifier = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "text", nullable: false),
                    location = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_offices", x => x.identifier);
                });

            migrationBuilder.CreateTable(
                name: "rooms",
                columns: table => new
                {
                    identifier = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "text", nullable: false),
                    capacity = table.Column<int>(type: "integer", nullable: false),
                    office_identifier = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_rooms", x => x.identifier);
                    table.ForeignKey(
                        name: "fk_rooms_offices_office_identifier",
                        column: x => x.office_identifier,
                        principalTable: "offices",
                        principalColumn: "identifier",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ux_offices_company_identifier_name",
                table: "offices",
                columns: new[] { "company_identifier", "name" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ux_rooms_office_identifier_name",
                table: "rooms",
                columns: new[] { "office_identifier", "name" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "companies");

            migrationBuilder.DropTable(
                name: "rooms");

            migrationBuilder.DropTable(
                name: "offices");
        }
    }
}
