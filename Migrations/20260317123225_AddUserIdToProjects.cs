using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PlanAI.Migrations
{
    /// <inheritdoc />
    public partial class AddUserIdToProjects : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "UserId",
                table: "ProjectPlans",
                type: "TEXT",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_ProjectPlans_UserId",
                table: "ProjectPlans",
                column: "UserId");

            migrationBuilder.AddForeignKey(
                name: "FK_ProjectPlans_Users_UserId",
                table: "ProjectPlans",
                column: "UserId",
                principalTable: "Users",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ProjectPlans_Users_UserId",
                table: "ProjectPlans");

            migrationBuilder.DropIndex(
                name: "IX_ProjectPlans_UserId",
                table: "ProjectPlans");

            migrationBuilder.DropColumn(
                name: "UserId",
                table: "ProjectPlans");
        }
    }
}
