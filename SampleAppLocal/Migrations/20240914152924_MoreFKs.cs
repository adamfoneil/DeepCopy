using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SampleAppLocal.Migrations
{
    /// <inheritdoc />
    public partial class MoreFKs : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "BillToClientId1",
                table: "TemplateService",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_TemplateService_BillToClientId1",
                table: "TemplateService",
                column: "BillToClientId1");

            migrationBuilder.CreateIndex(
                name: "IX_TemplateService_ServiceId",
                table: "TemplateService",
                column: "ServiceId");

            migrationBuilder.AddForeignKey(
                name: "FK_TemplateService_Services_ServiceId",
                table: "TemplateService",
                column: "ServiceId",
                principalTable: "Services",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_TemplateService_VolumeClients_BillToClientId1",
                table: "TemplateService",
                column: "BillToClientId1",
                principalTable: "VolumeClients",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_TemplateService_Services_ServiceId",
                table: "TemplateService");

            migrationBuilder.DropForeignKey(
                name: "FK_TemplateService_VolumeClients_BillToClientId1",
                table: "TemplateService");

            migrationBuilder.DropIndex(
                name: "IX_TemplateService_BillToClientId1",
                table: "TemplateService");

            migrationBuilder.DropIndex(
                name: "IX_TemplateService_ServiceId",
                table: "TemplateService");

            migrationBuilder.DropColumn(
                name: "BillToClientId1",
                table: "TemplateService");
        }
    }
}
