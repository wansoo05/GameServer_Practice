using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Newtonsoft.Json.Linq;
using GameServer_01.Interfaces;

namespace GameServer_01.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class ContentsController : ControllerBase
    {
        private readonly IContentsService _contentsService;
        public ContentsController(IContentsService contentsService) => _contentsService = contentsService;

        [HttpPost("stage/clear")]
        public async Task<IActionResult> StageClear([FromBody] JObject request)
        {
            var uid = User.FindFirst("firebase_uid")?.Value!;

            // JObject에서 각 필드를 꺼낼 때는 request["필드명"]?.ToString() 을 사용
            string stageIDStr = request["stageId"]?.ToString() ?? "";
            string goldStr = request["gold"]?.ToString() ?? "";
            string gemStr = request["gem"]?.ToString() ?? "";
            uint stageID = uint.TryParse(stageIDStr, out var tmpStage) ? tmpStage : 0;
            ulong gold = ulong.TryParse(goldStr, out var tmpGold) ? tmpGold : 0;
            ulong gem = uint.TryParse(gemStr, out var tmpGem) ? tmpGem : 0;

            var (isFound, data, errorMsg) = await _contentsService.StageClearAsync(uid, stageID, gold, gem);

            if (!isFound || data == null)
            {
                return NotFound(new { error = errorMsg });
            }

            return Ok(data);
        }

        [HttpPost("chapter/clear")]
        public async Task<IActionResult> ChapterClear([FromBody] JObject request)
        {
            var uid = User.FindFirst("firebase_uid")?.Value!;

            // JObject에서 각 필드를 꺼낼 때는 request["필드명"]?.ToString() 을 사용
            string chapterIDStr = request["chapterId"]?.ToString() ?? "";
            string goldStr = request["gold"]?.ToString() ?? "";
            string gemStr = request["gem"]?.ToString() ?? "";
            uint chapterID = uint.TryParse(chapterIDStr, out var tmpStage) ? tmpStage : 0;
            ulong gold = ulong.TryParse(goldStr, out var tmpGold) ? tmpGold : 0;
            ulong gem = uint.TryParse(gemStr, out var tmpGem) ? tmpGem : 0;

            var (isFound, data, errorMsg) = await _contentsService.ChapterClearAsync(uid, chapterID, gold, gem);

            if (!isFound || data == null)
            {
                return NotFound(new { error = errorMsg });
            }

            return Ok(data);
        }
    }
}
