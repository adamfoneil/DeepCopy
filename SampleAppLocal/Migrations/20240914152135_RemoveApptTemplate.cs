using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SampleAppLocal.Migrations
{
    /// <inheritdoc />
    public partial class RemoveApptTemplate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_TemplateService_TemplatePatient_TemplatePatientId",
                table: "TemplateService");

            migrationBuilder.DropTable(
                name: "TemplatePatient");

            migrationBuilder.DropTable(
                name: "AppointmentTemplates");

            migrationBuilder.DropIndex(
                name: "IX_TemplateService_TemplatePatientId",
                table: "TemplateService");

            migrationBuilder.CreateIndex(
                name: "IX_TemplateService_BillToClientId",
                table: "TemplateService",
                column: "BillToClientId");

            migrationBuilder.AddForeignKey(
                name: "FK_TemplateService_VolumeClients_BillToClientId",
                table: "TemplateService",
                column: "BillToClientId",
                principalTable: "VolumeClients",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_TemplateService_VolumeClients_BillToClientId",
                table: "TemplateService");

            migrationBuilder.DropIndex(
                name: "IX_TemplateService_BillToClientId",
                table: "TemplateService");

            migrationBuilder.CreateTable(
                name: "AppointmentTemplates",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AppointmentTemplates", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "TemplatePatient",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    AppointmentTemplateId = table.Column<int>(type: "int", nullable: false),
                    Sex = table.Column<string>(type: "nvarchar(1)", nullable: false),
                    Species = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TemplatePatient", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TemplatePatient_AppointmentTemplates_AppointmentTemplateId",
                        column: x => x.AppointmentTemplateId,
                        principalTable: "AppointmentTemplates",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_TemplateService_TemplatePatientId",
                table: "TemplateService",
                column: "TemplatePatientId");

            migrationBuilder.CreateIndex(
                name: "IX_TemplatePatient_AppointmentTemplateId",
                table: "TemplatePatient",
                column: "AppointmentTemplateId");

            migrationBuilder.AddForeignKey(
                name: "FK_TemplateService_TemplatePatient_TemplatePatientId",
                table: "TemplateService",
                column: "TemplatePatientId",
                principalTable: "TemplatePatient",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
