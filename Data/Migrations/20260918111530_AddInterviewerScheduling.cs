using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RecruitmentTracker.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddInterviewerScheduling : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "InterviewerRequiredSkills",
                table: "JobVacancies",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "MinimumInterviewerExperience",
                table: "JobVacancies",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "MinimumInterviewerSeniority",
                table: "JobVacancies",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Department",
                table: "AspNetUsers",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "JobTitle",
                table: "AspNetUsers",
                type: "nvarchar(max)",
                nullable: true);

           

            migrationBuilder.AddColumn<string>(
                name: "Skills",
                table: "AspNetUsers",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "YearsOfExperience",
                table: "AspNetUsers",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "InterviewerRequiredSkills",
                table: "JobVacancies");

            migrationBuilder.DropColumn(
                name: "MinimumInterviewerExperience",
                table: "JobVacancies");

            migrationBuilder.DropColumn(
                name: "MinimumInterviewerSeniority",
                table: "JobVacancies");

            migrationBuilder.DropColumn(
                name: "Department",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "JobTitle",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "SeniorityLevel",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "Skills",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "YearsOfExperience",
                table: "AspNetUsers");
        }
    }
}
