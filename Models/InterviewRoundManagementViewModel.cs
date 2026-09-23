
namespace RecruitmentTracker.Models
{
    public class InterviewRoundManagementViewModel
    {
        public InterviewRound Form { get; set; }
            = new InterviewRound();

        public List<InterviewRound> InterviewRounds { get; set; }
            = new List<InterviewRound>();

        public List<JobVacancy> Jobs { get; set; }
            = new List<JobVacancy>();

        public string Search { get; set; } = string.Empty;

        public int? JobId { get; set; }

        public string Status { get; set; } = "all";

        public int TotalCount { get; set; }

        public int ActiveCount { get; set; }

        public int InactiveCount { get; set; }

        public bool IsEditing => Form.Id > 0;
    }
}