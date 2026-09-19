using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NID_Project.Migrations
{
    /// <inheritdoc />
    public partial class AddEditApplications : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "EditApplications",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    RequestedFields = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    ReviewedByModeratorId = table.Column<string>(type: "nvarchar(450)", nullable: true),
                    ReviewedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ReviewMessage = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    EditedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    EditedDataJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ChangeReviewedByModeratorId = table.Column<string>(type: "nvarchar(450)", nullable: true),
                    ChangeReviewedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ChangeReviewMessage = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EditApplications", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EditApplications_AspNetUsers_ChangeReviewedByModeratorId",
                        column: x => x.ChangeReviewedByModeratorId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_EditApplications_AspNetUsers_ReviewedByModeratorId",
                        column: x => x.ReviewedByModeratorId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_EditApplications_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_EditApplications_ChangeReviewedByModeratorId",
                table: "EditApplications",
                column: "ChangeReviewedByModeratorId");

            migrationBuilder.CreateIndex(
                name: "IX_EditApplications_ReviewedByModeratorId",
                table: "EditApplications",
                column: "ReviewedByModeratorId");

            migrationBuilder.CreateIndex(
                name: "IX_EditApplications_UserId",
                table: "EditApplications",
                column: "UserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "EditApplications");
        }
    }
}
