using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RecruitmentTracker.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddInterviewFeedbackWorkflow : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "InterviewDecisionBy",
                table: "JobApplications",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "InterviewDecisionDate",
                table: "JobApplications",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "InterviewOverallScore",
                table: "JobApplications",
                type: "float",
                nullable: false,
                defaultValue: 0.0);

            migrationBuilder.AddColumn<bool>(
                name: "InterviewProcessCompleted",
                table: "JobApplications",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "InterviewStageStatus",
                table: "JobApplications",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<DateTime>(
                name: "CompletedDate",
                table: "Interviews",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RoundDecisionBy",
                table: "Interviews",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "RoundDecisionDate",
                table: "Interviews",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RoundResult",
                table: "Interviews",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<double>(
                name: "RoundScore",
                table: "Interviews",
                type: "float",
                nullable: false,
                defaultValue: 0.0);

            migrationBuilder.CreateTable(
                name: "InterviewFeedback",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    InterviewId = table.Column<int>(type: "int", nullable: false),
                    InterviewerId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    TechnicalKnowledge = table.Column<int>(type: "int", nullable: false),
                    ProblemSolving = table.Column<int>(type: "int", nullable: false),
                    Communication = table.Column<int>(type: "int", nullable: false),
                    RoleUnderstanding = table.Column<int>(type: "int", nullable: false),
                    Professionalism = table.Column<int>(type: "int", nullable: false),
                    OverallScore = table.Column<double>(type: "float", nullable: false),
                    Strengths = table.Column<string>(type: "nvarchar(1500)", maxLength: 1500, nullable: false),
                    Concerns = table.Column<string>(type: "nvarchar(1500)", maxLength: 1500, nullable: false),
                    Comments = table.Column<string>(type: "nvarchar(3000)", maxLength: 3000, nullable: false),
                    Recommendation = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    SubmittedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InterviewFeedback", x => x.Id);
                    table.ForeignKey(
                        name: "FK_InterviewFeedback_AspNetUsers_InterviewerId",
                        column: x => x.InterviewerId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_InterviewFeedback_Interviews_InterviewId",
                        column: x => x.InterviewId,
                        principalTable: "Interviews",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_InterviewFeedback_InterviewerId",
                table: "InterviewFeedback",
                column: "InterviewerId");

            migrationBuilder.CreateIndex(
                name: "IX_InterviewFeedback_InterviewId",
                table: "InterviewFeedback",
                column: "InterviewId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "InterviewFeedback");

            migrationBuilder.DropColumn(
                name: "InterviewDecisionBy",
                table: "JobApplications");

            migrationBuilder.DropColumn(
                name: "InterviewDecisionDate",
                table: "JobApplications");

            migrationBuilder.DropColumn(
                name: "InterviewOverallScore",
                table: "JobApplications");

            migrationBuilder.DropColumn(
                name: "InterviewProcessCompleted",
                table: "JobApplications");

            migrationBuilder.DropColumn(
                name: "InterviewStageStatus",
                table: "JobApplications");

            migrationBuilder.DropColumn(
                name: "CompletedDate",
                table: "Interviews");

            migrationBuilder.DropColumn(
                name: "RoundDecisionBy",
                table: "Interviews");

            migrationBuilder.DropColumn(
                name: "RoundDecisionDate",
                table: "Interviews");

            migrationBuilder.DropColumn(
                name: "RoundResult",
                table: "Interviews");

            migrationBuilder.DropColumn(
                name: "RoundScore",
                table: "Interviews");
        }
    }
}
