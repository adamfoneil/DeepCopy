using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SampleAppLocal.Migrations
{
    /// <inheritdoc />
    public partial class TemplatePatient : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "TemplatePatients",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Sex = table.Column<string>(type: "nvarchar(1)", nullable: false),
                    Species = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TemplatePatients", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_TemplateService_TemplatePatientId",
                table: "TemplateService",
                column: "TemplatePatientId");

            migrationBuilder.AddForeignKey(
                name: "FK_TemplateService_TemplatePatients_TemplatePatientId",
                table: "TemplateService",
                column: "TemplatePatientId",
                principalTable: "TemplatePatients",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_TemplateService_TemplatePatients_TemplatePatientId",
                table: "TemplateService");

            migrationBuilder.DropTable(
                name: "TemplatePatients");

            migrationBuilder.DropIndex(
                name: "IX_TemplateService_TemplatePatientId",
                table: "TemplateService");
        }
    }
}
