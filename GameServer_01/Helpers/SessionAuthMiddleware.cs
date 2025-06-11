using System.Security.Claims;

namespace GameServer_01.Helpers
{
    public class SessionAuthMiddleware
    {
        private readonly RequestDelegate _next;
        public SessionAuthMiddleware(RequestDelegate next) => _next = next;

        public async Task InvokeAsync(HttpContext ctx)
        {
            // 세션 키가 없으면 401
            var uid = ctx.Session.GetString("UserUid");
            if (string.IsNullOrEmpty(uid))
            {
                ctx.Response.StatusCode = StatusCodes.Status401Unauthorized;
                await ctx.Response.WriteAsync("세션이 없습니다. 로그인 후 이용하세요.");
                return;
            }

            // 유효하면 클레임 세팅
            var claims = new[] { new Claim("firebase_uid", uid) };
            ctx.User = new ClaimsPrincipal(new ClaimsIdentity(claims, "Session"));

            await _next(ctx);
        }
    }
}
