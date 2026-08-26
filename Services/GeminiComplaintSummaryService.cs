using System.Text;
using System.Text.Json;
using ADHUNIK_BARI.Models;

namespace ADHUNIK_BARI.Services
{
    public class GeminiComplaintSummaryService
        : IAIComplaintSummaryService
    {

        private readonly IConfiguration configuration;
        private readonly HttpClient httpClient;


        public GeminiComplaintSummaryService(
            IConfiguration configuration,
            HttpClient httpClient)
        {
            this.configuration = configuration;
            this.httpClient = httpClient;

            this.httpClient.Timeout =
                TimeSpan.FromSeconds(60);
        }



        public async Task<AIComplaintReport> GenerateComplaintSummary(
            string complaintText)
        {

            var apiKey =
                configuration["Gemini:ApiKey"];



            var model =
                configuration["Gemini:Model"]
                ?.Replace("models/", "");



            var url =
                $"https://generativelanguage.googleapis.com/v1beta/models/{model}:generateContent?key={apiKey}";



            var prompt = $@"

You are an AI assistant for an apartment management system.

Analyze the following resident complaints.

Return ONLY valid JSON.
Do not include markdown.
Do not include ```json.


Required JSON format:


{{
  ""riskLevel"": ""LOW/MEDIUM/HIGH"",
  
  ""totalSummary"": """",

  ""criticalIssues"": [
    {{
      ""title"": """",
      ""description"": """",
      ""severity"": """",
      ""action"": """"
    }}
  ],


  ""commonProblems"": [
    {{
      ""name"": """",
      ""count"": 0
    }}
  ],


  ""hiddenProblems"": [
    """"
  ],


  ""recommendedActions"": [
    """"
  ]
}}



Resident Complaints:

{complaintText}

";



            var requestBody = new
            {
                contents = new[]
                {
                    new
                    {
                        parts = new[]
                        {
                            new
                            {
                                text = prompt
                            }
                        }
                    }
                }
            };



            var json =
                JsonSerializer.Serialize(requestBody);



            var response =
                await httpClient.PostAsync(
                    url,
                    new StringContent(
                        json,
                        Encoding.UTF8,
                        "application/json"
                    )
                );



            var result =
                await response.Content.ReadAsStringAsync();



            if (!response.IsSuccessStatusCode)
            {
                throw new Exception(
                    $"Gemini API Error: {result}"
                );
            }



            using var document =
                JsonDocument.Parse(result);



            var aiText =
                document
                .RootElement
                .GetProperty("candidates")[0]
                .GetProperty("content")
                .GetProperty("parts")[0]
                .GetProperty("text")
                .GetString();



            if (string.IsNullOrEmpty(aiText))
            {
                throw new Exception(
                    "Gemini returned empty response."
                );
            }



            // Remove accidental markdown if AI returns it

            aiText =
                aiText
                .Replace("```json", "")
                .Replace("```", "")
                .Trim();



            var report =
                JsonSerializer.Deserialize<AIComplaintReport>(
                    aiText,
                    new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    }
                );



            if (report == null)
            {
                throw new Exception(
                    "Unable to parse AI response."
                );
            }



            return report;

        }
    }
}