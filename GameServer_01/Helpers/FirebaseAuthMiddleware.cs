using System.Linq;
using System.Threading.Tasks;
using FirebaseAdmin.Auth;
using Microsoft.AspNetCore.Http;
using System.Security.Claims;

namespace GameServer_01.Helpers
{
    public class FirebaseAuthMiddleware
    {
        private readonly RequestDelegate _next;
        public FirebaseAuthMiddleware(RequestDelegate next) => _next = next;

        public async Task Invoke(HttpContext ctx)
        {
            var header = ctx.Request.Headers["Authorization"].FirstOrDefault();
            if (header == null || !header.StartsWith("Bearer "))
            {
                Console.WriteLine("❌ Authorization header missing.");
                ctx.Response.StatusCode = 401;
                return;
            }

            var token = header["Bearer ".Length..].Trim();
            try
            {
                var decoded = await FirebaseAuth.DefaultInstance.VerifyIdTokenAsync(token);
                var claims = new List<Claim>();
                claims.Add(new Claim("firebase_uid", decoded.Uid)); // decoded.Uid가 FirebaseUid
                if (decoded.Claims.TryGetValue("email", out var emailObj) && !string.IsNullOrEmpty(emailObj?.ToString()))
                {
                    claims.Add(new Claim(ClaimTypes.Email, emailObj.ToString()!));
                }
                ctx.User = new ClaimsPrincipal(new ClaimsIdentity(claims, "Firebase"));

                await _next(ctx);
                Console.WriteLine(">>> AuthMW 다음 미들웨어 복귀");
            }
            catch (FirebaseAuthException fae)
            {
                Console.WriteLine("[AuthMW] JWT 검증 실패: " + fae.Message);
                ctx.Response.StatusCode = StatusCodes.Status401Unauthorized;
                await ctx.Response.WriteAsync($"Firebase token verification failed: {fae.Message}");
            }
            catch (System.Exception ex)
            {
                Console.WriteLine($"❌ Firebase token verification failed: {ex.Message}");
                ctx.Response.StatusCode = 401;
            }
        }
    }
}