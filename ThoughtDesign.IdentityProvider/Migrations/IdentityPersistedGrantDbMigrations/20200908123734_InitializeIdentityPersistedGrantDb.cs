using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace ThoughtDesign.IdentityProvider.Migrations.IdentityPersistedGrantDbMigrations
{
    public partial class InitializeIdentityPersistedGrantDb : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "device_code",
                columns: table => new
                {
                    user_code = table.Column<string>(maxLength: 200, nullable: false),
                    device_code = table.Column<string>(maxLength: 200, nullable: false),
                    subject_id = table.Column<string>(maxLength: 200, nullable: true),
                    session_id = table.Column<string>(maxLength: 100, nullable: true),
                    client_id = table.Column<string>(maxLength: 200, nullable: false),
                    description = table.Column<string>(maxLength: 200, nullable: true),
                    creation_time = table.Column<DateTime>(nullable: false),
                    expiration = table.Column<DateTime>(nullable: false),
                    data = table.Column<string>(maxLength: 50000, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("device_code_pkey", x => x.user_code);
                });

            migrationBuilder.CreateTable(
                name: "persisted_grant",
                columns: table => new
                {
                    key = table.Column<string>(maxLength: 200, nullable: false),
                    type = table.Column<string>(maxLength: 50, nullable: false),
                    subject_id = table.Column<string>(maxLength: 200, nullable: true),
                    session_id = table.Column<string>(maxLength: 100, nullable: true),
                    client_id = table.Column<string>(maxLength: 200, nullable: false),
                    description = table.Column<string>(maxLength: 200, nullable: true),
                    creation_time = table.Column<DateTime>(nullable: false),
                    expiration = table.Column<DateTime>(nullable: true),
                    consumed_time = table.Column<DateTime>(nullable: true),
                    data = table.Column<string>(maxLength: 50000, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("persisted_grant_pkey", x => x.key);
                });

            migrationBuilder.CreateIndex(
                name: "device_code. device_code. uqix",
                table: "device_code",
                column: "device_code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "device_code. expiration. ix",
                table: "device_code",
                column: "expiration");

            migrationBuilder.CreateIndex(
                name: "persisted_grant. expiration. ix",
                table: "persisted_grant",
                column: "expiration");

            migrationBuilder.CreateIndex(
                name: "persisted_grant. subject_id,client_id,type. ix",
                table: "persisted_grant",
                columns: new[] { "subject_id", "client_id", "type" });

            migrationBuilder.CreateIndex(
                name: "persisted_grant. subject_id,session_id,type. ix",
                table: "persisted_grant",
                columns: new[] { "subject_id", "session_id", "type" });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "device_code");

            migrationBuilder.DropTable(
                name: "persisted_grant");
        }
    }
}
