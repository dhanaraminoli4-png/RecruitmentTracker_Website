using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RecruitmentTracker.Data.Migrations
{
    /// <inheritdoc />
    public partial class UpdateInterviewForScheduling : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Interviews",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    JobApplicationId = table.Column<int>(type: "int", nullable: false),
                    InterviewRoundId = table.Column<int>(type: "int", nullable: false),
                    InterviewTypeId = table.Column<int>(type: "int", nullable: false),
                    InterviewDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    InterviewTime = table.Column<TimeSpan>(type: "time", nullable: false),
                    DurationMinutes = table.Column<int>(type: "int", nullable: false),
                    Location = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    MeetingLink = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    InterviewerIds = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Status = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    FeedbackStatus = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Interviews", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Interviews_InterviewRounds_InterviewRoundId",
                        column: x => x.InterviewRoundId,
                        principalTable: "InterviewRounds",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Interviews_InterviewTypes_InterviewTypeId",
                        column: x => x.InterviewTypeId,
                        principalTable: "InterviewTypes",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Interviews_JobApplications_JobApplicationId",
                        column: x => x.JobApplicationId,
                        principalTable: "JobApplications",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_Interviews_InterviewRoundId",
                table: "Interviews",
                column: "InterviewRoundId");

            migrationBuilder.CreateIndex(
                name: "IX_Interviews_InterviewTypeId",
                table: "Interviews",
                column: "InterviewTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_Interviews_JobApplicationId",
                table: "Interviews",
                column: "JobApplicationId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Interviews");
        }
    }
}
