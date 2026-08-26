namespace ADHUNIK_BARI.Models
{
    public class AIComplaintReport
    {

        public string RiskLevel { get; set; }
            = string.Empty;


        public string TotalSummary { get; set; }
            = string.Empty;



        public List<AICriticalIssue> CriticalIssues { get; set; }
            = new();



        public List<AIProblem> CommonProblems { get; set; }
            = new();



        public List<string> HiddenProblems { get; set; }
            = new();



        public List<string> RecommendedActions { get; set; }
            = new();

    }




    public class AICriticalIssue
    {

        public string Title { get; set; }
            = string.Empty;


        public string Description { get; set; }
            = string.Empty;


        public string Severity { get; set; }
            = string.Empty;


        public string Action { get; set; }
            = string.Empty;

    }




    public class AIProblem
    {

        public string Name { get; set; }
            = string.Empty;


        public int Count { get; set; }

    }
}