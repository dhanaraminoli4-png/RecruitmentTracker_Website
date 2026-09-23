namespace RecruitmentTracker.Models
{
    public class InterviewTypeManagementViewModel
    {
        public InterviewType Form { get; set; } = new InterviewType();

        public List<InterviewType> InterviewTypes { get; set; }
            = new List<InterviewType>();

        public string Search { get; set; } = string.Empty;

        public string Status { get; set; } = "all";

        public bool IsEditing => Form.Id > 0;
    }
}