using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using GameServer_01.Data;
using Newtonsoft.Json;
using FirebaseAdmin.Auth;
using Newtonsoft.Json.Linq;
using GameServer_01.Interfaces;
using GameServer_01.Services;

namespace GameServer_01.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class UserController : ControllerBase
    {
        private readonly IUserService _userService;
        public UserController(IUserService userService) => _userService = userService;

        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] JObject request)
        {
            var uid = User.FindFirst("firebase_uid")?.Value!;
            var email = User.FindFirst(ClaimTypes.Email)?.Value ?? "";

            // JObject에서 각 필드를 꺼낼 때는 request["필드명"]?.ToString() 을 사용
            string deviceId = request["deviceId"]?.ToString() ?? "";
            string provider = request["provider"]?.ToString() ?? "";

            var (IsSuccess, UID, ErrorMsg) = await _userService.RegisterAsync(uid, email, deviceId, provider);

            if (!IsSuccess)
            {
                return BadRequest(new {error = ErrorMsg});
            }
            var response = new JObject
            {
                ["user"] = new JObject
                {
                    ["uid"] = uid,
                }
            };
            return Ok(response);
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login()
        {
            var uid = User.FindFirst("firebase_uid")?.Value!;

            var (isFound, data, errorMsg) = await _userService.LoginAsync(uid);

            if (!isFound || data == null)
            {
                return NotFound(new {error = errorMsg});
            }

            return Ok(data);
        }

        [HttpPost("delete")]
        public async Task<IActionResult> DeleteAccount()
        {
            var uid = User.FindFirst("firebase_uid")?.Value!;
            bool deleted = await _userService.DeleteLocalUserAsync(uid);
            if (!deleted)
                return NotFound(new { error = "User not found." });

            try
            {
                await FirebaseAuth.DefaultInstance.DeleteUserAsync(uid);
            }
            catch (FirebaseAuthException ex)
            {
                // DB는 이미 삭제된 상태이므로, Firebase 삭제 실패 시 운영 로그를 남기고
                // 클라이언트에는 500 응답을 보낼 수 있습니다.
                return StatusCode(500, new { error = $"Failed to delete from Firebase Auth: {ex.Message}" });
            }
           

            // 삭제 완료 응답
            return Ok(new { message = "Account deleted successfully." });
        }
    }
}