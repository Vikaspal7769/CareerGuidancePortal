using System.Text;
using System.Text.Json;
using CareerGuidancePortal.Models;
namespace CareerGuidancePortal.Services;
public class AiRecommendationService(IConfiguration config,IHttpClientFactory httpClientFactory)
{
    public async Task<List<CareerRecommendation>> GetRecommendations(Student s)
    {
        var key=config["Gemini:ApiKey"]??Environment.GetEnvironmentVariable("GEMINI_API_KEY");
        if(!string.IsNullOrWhiteSpace(key))
        {
            try
            {
                var prompt=$"""You are a career guidance assistant. Analyze this student profile and return JSON only. Name:{s.Name}; Education:{s.Education}; College:{s.College}; Skills:{s.Skills}; Interests:{s.Interests}; Goal:{s.CareerGoal}; Location:{s.PreferredLocation}. Return an array of 4 objects with title, matchScore (0-100 integer), description, reason, skills (array of 3-5 strings). Do not invent personal facts.""";
                var body=JsonSerializer.Serialize(new{contents=new[]{new{parts=new[]{new{text=prompt}}}}});
                var client=httpClientFactory.CreateClient();
                var r=await client.PostAsync($"https://generativelanguage.googleapis.com/v1beta/models/gemini-2.5-flash:generateContent?key={key}",new StringContent(body,Encoding.UTF8,"application/json"));
                if(r.IsSuccessStatusCode){var j=JsonDocument.Parse(await r.Content.ReadAsStringAsync());var t=j.RootElement.GetProperty("candidates")[0].GetProperty("content").GetProperty("parts")[0].GetProperty("text").GetString()??"";t=t.Replace("```json","").Replace("```","").Trim();var result=JsonSerializer.Deserialize<List<CareerRecommendation>>(t,new JsonSerializerOptions{PropertyNameCaseInsensitive=true});if(result is{Count:>0})return result.Take(6).ToList();}
            }catch{ }
        }
        return Local(s);
    }
    static List<CareerRecommendation> Local(Student s)
    {
        var all=$"{s.Skills} {s.Interests} {s.CareerGoal}".ToLowerInvariant();var list=new List<CareerRecommendation>();
        void Add(string t,int score,string d,string r,params string[] skills)=>list.Add(new CareerRecommendation{Title=t,MatchScore=score,Description=d,Reason=r,Skills=skills.ToList()});
        if(all.Contains("ai")||all.Contains("machine")||all.Contains("python")||all.Contains("data"))Add("AI / Machine Learning Engineer",94,"Build intelligent applications using data, Python and machine learning.","Your profile mentions AI, data or Python-related interests.","Python","Machine Learning","SQL","Statistics");
        if(all.Contains("web")||all.Contains("html")||all.Contains("css")||all.Contains("javascript")||all.Contains("java"))Add("Full Stack Developer",91,"Design and build modern web applications across frontend and backend.","Your technical skills align with application development.","JavaScript","HTML/CSS","APIs","Databases");
        if(all.Contains("cyber")||all.Contains("security")||all.Contains("network")||all.Contains("ethical"))Add("Cybersecurity Analyst",89,"Help organizations identify threats and strengthen systems.","Your interests point toward security and networking.","Networking","Linux","Security","SIEM");
        if(all.Contains("sql")||all.Contains("data")||all.Contains("analytics"))Add("Data Analyst",87,"Turn structured data into useful business and product insights.","Your profile includes data-oriented skills or interests.","SQL","Excel","Python","Data Visualization");
        Add("Software Engineer",82,"Create, test and maintain software products and services.","Software engineering is a broad path that matches a computing education.","Programming","Git","Testing","Problem Solving");
        return list.GroupBy(x=>x.Title).Select(x=>x.First()).Take(5).ToList();
    }
}
