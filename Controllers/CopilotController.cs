using AsusLaptop.Models;
using AsusLaptop.Services;
using Microsoft.AspNetCore.Mvc;

namespace AsusLaptop.Controllers
{
    public class CopilotController : Controller
    {
        private readonly LaptopCopilotService _copilot;
        private readonly ZaloAiTtsService _zaloAiTts;
        public CopilotController(LaptopCopilotService copilot, ZaloAiTtsService zaloAiTts)
        {
            _copilot = copilot;
            _zaloAiTts = zaloAiTts;
        }

        [HttpGet]
        public IActionResult Index() => View();

        [HttpPost]
        public async Task<IActionResult> Recommend([FromBody] CopilotRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.Message))
                return BadRequest(new { message = "Hãy cho Copilot biết nhu cầu của bạn." });
            return Json(await _copilot.RecommendAsync(request.Message));
        }

        // Zalo AI runs server-side: customers receive only the final audio, never the API key.
        [HttpGet]
        public async Task<IActionResult> Voice(string? text)
        {
            if (string.IsNullOrWhiteSpace(text)) return BadRequest();
            try
            {
                var audio = await _zaloAiTts.SynthesizeAsync(text.Trim());
                if (audio == null) return StatusCode(502);
                return File(audio, "audio/mpeg");
            }
            catch
            {
                return StatusCode(502);
            }
        }
    }
}
