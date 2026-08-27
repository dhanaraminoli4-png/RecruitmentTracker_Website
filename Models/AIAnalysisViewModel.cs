using System.Collections.Generic;

namespace RecruitmentTracker.Models
{
    public class AIAnalysisViewModel
    {
        public JobApplication Application { get; set; } = null!;

        public List<AIAnalysisRequirement> RequiredResults { get; set; }
            = new();

        public List<AIAnalysisRequirement> PreferredResults { get; set; }
            = new();
    }

    public class AIAnalysisRequirement
    {
        public string Requirement { get; set; } = "";

        public double Score { get; set; }

        public string Status { get; set; } = "";

        public string Method { get; set; } = "";
    }
}