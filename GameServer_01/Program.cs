using System;
using FirebaseAdmin;
using Google.Apis.Auth.OAuth2;
using GameServer_01.Data;
using GameServer_01.Helpers;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.EntityFrameworkCore;
using GameServer_01.Interfaces;
using GameServer_01.Services;

var builder = WebApplication.CreateBuilder(args);

//builder.Services.AddStackExchangeRedisCache(options =>
//{
//    options.Configuration = builder.Configuration["Redis:Configuration"];
//    options.InstanceName = "GameServerSession:";
//});

//builder.Services.AddSession(options =>
//{
//    options.Cookie.Name = ".GameServer.Session";
//    options.IdleTimeout = TimeSpan.FromHours(1);
//    options.Cookie.HttpOnly = true;
//    options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
//    options.Cookie.SameSite = SameSiteMode.Strict;
//    //options.Cookie.IsEssential = true;
//});

// ─── 1) Kestrel 바인딩 설정 ───────────────────────────────
builder.WebHost.ConfigureKestrel(opts =>
{
    // HTTP  : 모든 IP의 5000 포트 수신
    opts.ListenAnyIP(5000, listen => listen.Protocols = HttpProtocols.Http1);
    opts.ListenAnyIP(5001, (httpsOpt) => { httpsOpt.UseHttps(); });
});

// ─── 2) EF Core + MySQL (재시도 옵션 포함) ─────────────────
builder.Services.AddDbContext<GameDbContext>(opts =>
    opts.UseMySql(
        builder.Configuration.GetConnectionString("Default"),
        new MySqlServerVersion(new Version(8, 0, 31)),
        mySqlOpts => mySqlOpts
            .EnableRetryOnFailure(
                maxRetryCount: 5,
                maxRetryDelay: TimeSpan.FromSeconds(10),
                errorNumbersToAdd: null
            )
    )
);

// ─── 3) Firebase Admin SDK 초기화 ─────────────────────────
FirebaseApp.Create(new AppOptions
{
    Credential = GoogleCredential.FromFile("google-service-account.json")
});

// ─── 4) 컨트롤러·Swagger 등록 ─────────────────────────────
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<IContentsService, ContentsService>();
builder.Services.AddControllers().AddNewtonsoftJson();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddAuthorization();


var app = builder.Build();
app.Use(async (ctx, next) =>
{
    Console.WriteLine($"[Incoming] {ctx.Request.Method} {ctx.Request.Path}");
    await next();
});

// ─── 5) 개발 환경일 때 Swagger UI 활성화 ────────────────────
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// ─── 6) HTTPS 리다이렉션 ─────────────────────────────────
app.UseHttpsRedirection();
//app.UseSession();

//// 로그인 / 회원가입용 Firebase 토큰 검증
//app.UseWhen(
//    ctx => ctx.Request.Path.StartsWithSegments("/api/auth"),
//   branch =>
//    {
//        // JWT 인증 스킴이 있다면 Firebase 토큰 검증
//        branch.UseAuthentication();
//branch.UseMiddleware<FirebaseAuthMiddleware>();
//    }
//);

//// 그외 모든 API : 세션 검증
//app.UseWhen(
//    ctx => !ctx.Request.Path.StartsWithSegments("/api/auth"),
//    branch =>
//    {
//        branch.UseMiddleware<SessionAuthMiddleware>();
//    }
//);

app.UseAuthentication();
app.UseMiddleware<FirebaseAuthMiddleware>();

// ─── 8) 권한(Authorization) 미들웨어 ───────────────────────
app.UseAuthorization();

// ─── 9) 컨트롤러 라우팅 ─────────────────────────────────
app.MapControllers();

app.Run();
