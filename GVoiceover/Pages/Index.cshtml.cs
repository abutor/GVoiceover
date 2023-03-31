using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace GVoiceover.Pages
{
    public class IndexModel : PageModel
    {
        private readonly ILogger<IndexModel> _logger;
        private readonly HttpClient _httpClient;

        public string Token { get; set; } = string.Empty;
        public string Voice { get; set; } = "en-US-Studio-M";

        public IndexModel(ILogger<IndexModel> logger, IHttpClientFactory factory)
        {
            _logger = logger;
            _httpClient = factory.CreateClient();
        }

        public void OnGet()
        {
            if (Request.Cookies.TryGetValue("google_token", out var token)) 
            {
                Token = token;
            }
        }

        public async Task<IActionResult> OnPost([FromForm] string token, [FromForm] string voice, [FromForm] string text)
        {
            if (string.IsNullOrEmpty(token)
                || string.IsNullOrEmpty(voice)
                || string.IsNullOrEmpty(text))
            {
                return RedirectToPage("Error");
            }

            Response.Cookies.Append("google_token", token);

            var request = new
            {
                input = new { text },
                audioConfig = new { audioEncoding = "MP3" },
                voice = new
                {
                    name = voice,
                    languageCode = voice[..5]
                }
            };
            const string baseUrl = "https://us-central1-texttospeech.googleapis.com/v1beta1/text:synthesize";
            using var response = await _httpClient.PostAsJsonAsync($"{baseUrl}?key={token}", request);
            
            if (!response.IsSuccessStatusCode)
            {
                return RedirectToPage("Error");
            }

            var result = await response.Content.ReadFromJsonAsync<GoogleResponse>();
            if (result == null)
            {
                return RedirectToPage("Error");
            }

            return File(result.GetBytes(), "audio/mpeg", text[..12] + ".mp3");
        }

        class GoogleResponse
        {
            public string? audioContent { get; set; }

            public byte[] GetBytes()
            {
                if (string.IsNullOrEmpty(audioContent))
                {
                    return Array.Empty<byte>();
                }

                return Convert.FromBase64String(audioContent);
            }
        }
    }
}