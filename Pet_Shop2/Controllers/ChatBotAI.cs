using Markdig;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Text;

namespace Pet_Shop2.Controllers
{

    public class ChatBotAI : Controller
    {
        private static HttpClient Http = new HttpClient();


        [HttpPost]
        public async Task<string> GenerateLoremIpsum(string message)
        {


            // Địa chỉ API và API key của bạn
            string apiUrl = "https://openrouter.ai/api/v1/chat/completions";
            string apiKey = "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";

            var requestData = new
            {
                model = "deepseek/deepseek-r1:free",
                messages = new[]
                {
                    new { role = "user", content = message }
                },
            };
            // Tạo HttpClient để gửi yêu cầu
            using (HttpClient httpClient = new HttpClient())
            {
                try
                {
                    // Chuẩn bị dữ liệu yêu cầu dưới dạng JSON
                    string jsonRequest = JsonConvert.SerializeObject(requestData);

                    // Tạo đối tượng HttpRequestMessage và thêm API key vào tiêu đề "Authorization"
                    HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Post, apiUrl);
                    request.Content = new StringContent(jsonRequest, Encoding.UTF8, "application/json");
                    request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", apiKey);

                    // Gửi yêu cầu POST đến API của OpenAI
                    HttpResponseMessage response = await httpClient.SendAsync(request);

                    // Đọc và hiển thị câu trả lời từ phản hồi của API
                    string jsonResponse = await response.Content.ReadAsStringAsync();

                    // Parse JSON để lấy nội dung phản hồi
                    JObject parsed = JObject.Parse(jsonResponse);
                    string content = parsed["choices"]?[0]?["message"]?["content"]?.ToString();

                    if (string.IsNullOrEmpty(content))
                        return "Không thể trích xuất phản hồi từ OpenAI.";

                    // Chuyển markdown sang HTML
                    string htmlContent = Markdown.ToHtml(content);

                    return htmlContent;
                }
                catch (HttpRequestException e)
                {
                    return e.Message.ToString();
                }
            }
        }
    }
}
