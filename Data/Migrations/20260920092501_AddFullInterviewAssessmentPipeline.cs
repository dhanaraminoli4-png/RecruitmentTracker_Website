using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RecruitmentTracker.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddFullInterviewAssessmentPipeline : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Concerns",
                table: "InterviewFeedback");

            migrationBuilder.DropColumn(
                name: "OverallScore",
                table: "InterviewFeedback");

            migrationBuilder.RenameColumn(
                name: "TechnicalKnowledge",
                table: "InterviewFeedback",
                newName: "TechnicalRating");

            migrationBuilder.RenameColumn(
                name: "SubmittedAt",
                table: "InterviewFeedback",
                newName: "SubmittedDate");

            migrationBuilder.RenameColumn(
                name: "RoleUnderstanding",
                table: "InterviewFeedback",
                newName: "RoleFitRating");

            migrationBuilder.RenameColumn(
                name: "Professionalism",
                table: "InterviewFeedback",
                newName: "ProfessionalismRating");

            migrationBuilder.RenameColumn(
                name: "ProblemSolving",
                table: "InterviewFeedback",
                newName: "ProblemSolvingRating");

            migrationBuilder.RenameColumn(
                name: "Communication",
                table: "InterviewFeedback",
                newName: "OverallRating");

            migrationBuilder.AddColumn<string>(
                name: "RoundDecisionNotes",
                table: "Interviews",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Strengths",
                table: "InterviewFeedback",
                type: "nvarchar(2000)",
                maxLength: 2000,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(1500)",
                oldMaxLength: 1500);

            migrationBuilder.AlterColumn<string>(
                name: "Comments",
                table: "InterviewFeedback",
                type: "nvarchar(4000)",
                maxLength: 4000,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(3000)",
                oldMaxLength: 3000);

            migrationBuilder.AddColumn<int>(
                name: "CommunicationRating",
                table: "InterviewFeedback",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "Weaknesses",
                table: "InterviewFeedback",
                type: "nvarchar(2000)",
                maxLength: 2000,
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateTable(
                name: "AssessmentTemplate",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    JobVacancyId = table.Column<int>(type: "int", nullable: false),
                    InterviewRoundId = table.Column<int>(type: "int", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    Instructions = table.Column<string>(type: "nvarchar(3000)", maxLength: 3000, nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    UpdatedDate = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AssessmentTemplate", x => x.Id);
                    table.ForeignKey(
    name: "FK_AssessmentTemplate_InterviewRounds_InterviewRoundId",
    column: x => x.InterviewRoundId,
    principalTable: "InterviewRounds",
    principalColumn: "Id",
    onDelete: ReferentialAction.NoAction);
                    table.ForeignKey(
    name: "FK_AssessmentTemplate_JobVacancies_JobVacancyId",
    column: x => x.JobVacancyId,
    principalTable: "JobVacancies",
    principalColumn: "Id",
    onDelete: ReferentialAction.NoAction);
                });

            migrationBuilder.CreateTable(
                name: "AssessmentQuestion",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    AssessmentTemplateId = table.Column<int>(type: "int", nullable: false),
                    QuestionText = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    QuestionType = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    OptionsJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    IsRequired = table.Column<bool>(type: "bit", nullable: false),
                    DisplayOrder = table.Column<int>(type: "int", nullable: false),
                    MaximumMarks = table.Column<double>(type: "float", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AssessmentQuestion", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AssessmentQuestion_AssessmentTemplate_AssessmentTemplateId",
                        column: x => x.AssessmentTemplateId,
                        principalTable: "AssessmentTemplate",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "InterviewAssessment",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    InterviewId = table.Column<int>(type: "int", nullable: false),
                    AssessmentTemplateId = table.Column<int>(type: "int", nullable: false),
                    AssignedBy = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    AssignedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Score = table.Column<double>(type: "float", nullable: true),
                    MaxScore = table.Column<double>(type: "float", nullable: true),
                    ResultStatus = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    AssessmentDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Comments = table.Column<string>(type: "nvarchar(3000)", maxLength: 3000, nullable: true),
                    ConductedByInterviewerId = table.Column<string>(type: "nvarchar(450)", nullable: true),
                    SubmittedDate = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InterviewAssessment", x => x.Id);
                    table.ForeignKey(
                        name: "FK_InterviewAssessment_AspNetUsers_ConductedByInterviewerId",
                        column: x => x.ConductedByInterviewerId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_InterviewAssessment_AssessmentTemplate_AssessmentTemplateId",
                        column: x => x.AssessmentTemplateId,
                        principalTable: "AssessmentTemplate",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.NoAction);
                    table.ForeignKey(
                        name: "FK_InterviewAssessment_Interviews_InterviewId",
                        column: x => x.InterviewId,
                        principalTable: "Interviews",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.NoAction);
                });

            migrationBuilder.CreateTable(
                name: "AssessmentAnswer",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    InterviewAssessmentId = table.Column<int>(type: "int", nullable: false),
                    AssessmentQuestionId = table.Column<int>(type: "int", nullable: false),
                    AnswerText = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Score = table.Column<double>(type: "float", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AssessmentAnswer", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AssessmentAnswer_AssessmentQuestion_AssessmentQuestionId",
                        column: x => x.AssessmentQuestionId,
                        principalTable: "AssessmentQuestion",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_AssessmentAnswer_InterviewAssessment_InterviewAssessmentId",
                        column: x => x.InterviewAssessmentId,
                        principalTable: "InterviewAssessment",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AssessmentAnswer_AssessmentQuestionId",
                table: "AssessmentAnswer",
                column: "AssessmentQuestionId");

            migrationBuilder.CreateIndex(
                name: "IX_AssessmentAnswer_InterviewAssessmentId",
                table: "AssessmentAnswer",
                column: "InterviewAssessmentId");

            migrationBuilder.CreateIndex(
                name: "IX_AssessmentQuestion_AssessmentTemplateId",
                table: "AssessmentQuestion",
                column: "AssessmentTemplateId");

            migrationBuilder.CreateIndex(
                name: "IX_AssessmentTemplate_InterviewRoundId",
                table: "AssessmentTemplate",
                column: "InterviewRoundId");

            migrationBuilder.CreateIndex(
                name: "IX_AssessmentTemplate_JobVacancyId",
                table: "AssessmentTemplate",
                column: "JobVacancyId");

            migrationBuilder.CreateIndex(
                name: "IX_InterviewAssessment_AssessmentTemplateId",
                table: "InterviewAssessment",
                column: "AssessmentTemplateId");

            migrationBuilder.CreateIndex(
                name: "IX_InterviewAssessment_ConductedByInterviewerId",
                table: "InterviewAssessment",
                column: "ConductedByInterviewerId");

            migrationBuilder.CreateIndex(
                name: "IX_InterviewAssessment_InterviewId",
                table: "InterviewAssessment",
                column: "InterviewId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AssessmentAnswer");

            migrationBuilder.DropTable(
                name: "AssessmentQuestion");

            migrationBuilder.DropTable(
                name: "InterviewAssessment");

            migrationBuilder.DropTable(
                name: "AssessmentTemplate");

            migrationBuilder.DropColumn(
                name: "RoundDecisionNotes",
                table: "Interviews");

            migrationBuilder.DropColumn(
                name: "CommunicationRating",
                table: "InterviewFeedback");

            migrationBuilder.DropColumn(
                name: "Weaknesses",
                table: "InterviewFeedback");

            migrationBuilder.RenameColumn(
                name: "TechnicalRating",
                table: "InterviewFeedback",
                newName: "TechnicalKnowledge");

            migrationBuilder.RenameColumn(
                name: "SubmittedDate",
                table: "InterviewFeedback",
                newName: "SubmittedAt");

            migrationBuilder.RenameColumn(
                name: "RoleFitRating",
                table: "InterviewFeedback",
                newName: "RoleUnderstanding");

            migrationBuilder.RenameColumn(
                name: "ProfessionalismRating",
                table: "InterviewFeedback",
                newName: "Professionalism");

            migrationBuilder.RenameColumn(
                name: "ProblemSolvingRating",
                table: "InterviewFeedback",
                newName: "ProblemSolving");

            migrationBuilder.RenameColumn(
                name: "OverallRating",
                table: "InterviewFeedback",
                newName: "Communication");

            migrationBuilder.AlterColumn<string>(
                name: "Strengths",
                table: "InterviewFeedback",
                type: "nvarchar(1500)",
                maxLength: 1500,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(2000)",
                oldMaxLength: 2000);

            migrationBuilder.AlterColumn<string>(
                name: "Comments",
                table: "InterviewFeedback",
                type: "nvarchar(3000)",
                maxLength: 3000,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(4000)",
                oldMaxLength: 4000);

            migrationBuilder.AddColumn<string>(
                name: "Concerns",
                table: "InterviewFeedback",
                type: "nvarchar(1500)",
                maxLength: 1500,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<double>(
                name: "OverallScore",
                table: "InterviewFeedback",
                type: "float",
                nullable: false,
                defaultValue: 0.0);
        }
    }
}
