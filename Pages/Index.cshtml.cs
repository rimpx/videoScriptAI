using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Net.Http.Headers;

namespace videoscriptAI.Pages
{
    public class IndexModel : PageModel
    {
        private readonly IHttpClientFactory _httpClientFactory;

        public IndexModel(IHttpClientFactory httpClientFactory)
        {
            _httpClientFactory = httpClientFactory;
        }

        [BindProperty]
        public IFormFile? VideoFile { get; set; }

        [BindProperty]
        public string? YouTubeLink { get; set; }

        public async Task<IActionResult> OnPostAsync()
        {
            if (VideoFile == null && string.IsNullOrEmpty(YouTubeLink))
            {
                ModelState.AddModelError(string.Empty, "Devi caricare un video o inserire un link YouTube.");
                return Page();
            }

            var client = _httpClientFactory.CreateClient();
            client.BaseAddress = new Uri("https://api.gemini.com");

            HttpResponseMessage response;
            int chatId;

            if (VideoFile != null)
            {
                using var content = new MultipartFormDataContent();
                await using var stream = VideoFile.OpenReadStream();
                var videoContent = new StreamContent(stream);
                videoContent.Headers.ContentType = new MediaTypeHeaderValue("video/mp4");
                content.Add(videoContent, "file", VideoFile.FileName);

                response = await client.PostAsync("/create-chat", content);
            }
            else
            {
                var payload = new { youtubeLink = YouTubeLink };
                response = await client.PostAsJsonAsync("/create-chat", payload);
            }

            if (response.IsSuccessStatusCode)
            {
                chatId = await response.Content.ReadFromJsonAsync<int>();
                return RedirectToPage("/Chat", new { chatId });
            }
            else
            {
                ModelState.AddModelError(string.Empty, "Errore durante l'elaborazione del video.");
                return Page();
            }
        }
    }
}