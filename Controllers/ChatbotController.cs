using Microsoft.AspNetCore.Mvc;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace SIRH.EY.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ChatbotController : ControllerBase
    {
        private readonly IHttpClientFactory _httpClientFactory;

        public ChatbotController(IHttpClientFactory httpClientFactory)
        {
            _httpClientFactory = httpClientFactory;
        }

        public class ChatRequest
        {
            public string Message { get; set; }
        }

        public class ChatReply
        {
            public string Reply { get; set; }
        }

        [HttpPost("ask")]
        public async Task<IActionResult> Ask([FromBody] ChatRequest request)
        {
            if (request == null || string.IsNullOrWhiteSpace(request.Message))
            {
                return BadRequest("Message cannot be empty.");
            }

            try
            {
                var client = _httpClientFactory.CreateClient();
                var webhookUrl = "http://localhost:5678/webhook/hr-chatbot";

                var jsonContent = JsonSerializer.Serialize(new { message = request.Message });
                var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

                var response = await client.PostAsync(webhookUrl, content);
                
                if (response.IsSuccessStatusCode)
                {
                    var responseString = await response.Content.ReadAsStringAsync();
                    // Attempt to parse the response as JSON. If the n8n webhook returns {"reply": "..."}
                    // we can just forward the JSON.
                    
                    try 
                    {
                        var jsonResponse = JsonSerializer.Deserialize<JsonElement>(responseString);
                        // Check if the reply field exists
                        if (jsonResponse.TryGetProperty("reply", out var replyElement))
                        {
                            return Ok(new { reply = replyElement.GetString() });
                        }
                        return Ok(new { reply = responseString }); // Fallback if no specific "reply" field
                    }
                    catch (JsonException)
                    {
                        // Fallback if the response is not JSON
                        return Ok(new { reply = responseString });
                    }
                }
                else
                {
                    return StatusCode((int)response.StatusCode, new { reply = "Erreur de connexion avec l'assistant IA." });
                }
            }
            catch (System.Exception ex)
            {
                // In production, log the exception.
                return StatusCode(500, new { reply = "Le service est temporairement indisponible." });
            }
        }
    }
}
