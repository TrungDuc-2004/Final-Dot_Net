using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using static System.Net.WebRequestMethods;
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
            string apiUrl = "https://api.openai.com/v1/chat/completions";
            string apiKey = "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";

            // Chuỗi tin nhắn bạn muốn gửi đến ChatGPT
            string mess = message;

            // Tạo HttpClient để gửi yêu cầu
            using (HttpClient httpClient = new HttpClient())
            {
                try
                {
                    // Chuẩn bị dữ liệu yêu cầu dưới dạng JSON
                    string jsonRequest = $"{{\"prompt\":\"{mess}\",\"max_tokens\":150}}";

                    // Tạo đối tượng HttpRequestMessage và thêm API key vào tiêu đề "Authorization"
                    HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Post, apiUrl);
                    request.Content = new StringContent(jsonRequest, Encoding.UTF8, "application/json");
                    request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", apiKey);

                    // Gửi yêu cầu POST đến API của OpenAI
                    HttpResponseMessage response = await httpClient.SendAsync(request);

                    // Đọc và hiển thị câu trả lời từ phản hồi của API
                    string jsonResponse = await response.Content.ReadAsStringAsync();
                    return jsonResponse;
                }
                catch (HttpRequestException e)
                {
                    return e.Message.ToString();
                }
            }
        }
    }
}