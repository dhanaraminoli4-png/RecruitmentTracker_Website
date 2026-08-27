using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RecruitmentTracker.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddAIRecruitmentFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<double>(
                name: "AIOverallScore",
                table: "JobApplications",
                type: "float",
                nullable: false,
                defaultValue: 0.0);

            migrationBuilder.AddColumn<double>(
                name: "AIPreferredScore",
                table: "JobApplications",
                type: "float",
                nullable: false,
                defaultValue: 0.0);

            migrationBuilder.AddColumn<int>(
                name: "AIRank",
                table: "JobApplications",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "AIRecommendation",
                table: "JobApplications",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<double>(
                name: "AIRequiredScore",
                table: "JobApplications",
                type: "float",
                nullable: false,
                defaultValue: 0.0);

            migrationBuilder.AddColumn<double>(
                name: "AIScore",
                table: "JobApplications",
                type: "float",
                nullable: false,
                defaultValue: 0.0);

            migrationBuilder.AddColumn<bool>(
                name: "HRReviewed",
                table: "JobApplications",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "HRShortlisted",
                table: "JobApplications",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AIOverallScore",
                table: "JobApplications");

            migrationBuilder.DropColumn(
                name: "AIPreferredScore",
                table: "JobApplications");

            migrationBuilder.DropColumn(
                name: "AIRank",
                table: "JobApplications");

            migrationBuilder.DropColumn(
                name: "AIRecommendation",
                table: "JobApplications");

            migrationBuilder.DropColumn(
                name: "AIRequiredScore",
                table: "JobApplications");

            migrationBuilder.DropColumn(
                name: "AIScore",
                table: "JobApplications");

            migrationBuilder.DropColumn(
                name: "HRReviewed",
                table: "JobApplications");

            migrationBuilder.DropColumn(
                name: "HRShortlisted",
                table: "JobApplications");
        }
    }
}
