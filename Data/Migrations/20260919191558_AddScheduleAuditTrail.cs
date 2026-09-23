using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RecruitmentTracker.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddScheduleAuditTrail : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedAt",
                table: "InterviewerSchedules",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DeletedBy",
                table: "InterviewerSchedules",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "InterviewerSchedules",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DeletedAt",
                table: "InterviewerSchedules");

            migrationBuilder.DropColumn(
                name: "DeletedBy",
                table: "InterviewerSchedules");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                table: "InterviewerSchedules");
        }
    }
}
