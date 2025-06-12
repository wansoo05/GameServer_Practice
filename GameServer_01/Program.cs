using System;
using FirebaseAdmin;
using Google.Apis.Auth.OAuth2;
using GameServer_01.Data;
using GameServer_01.Helpers;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.EntityFrameworkCore;
using GameServer_01.Interfaces;
using GameServer_01.Services;
using Microsoft.Extensions.Options;
using System.Security.Cryptography.X509Certificates;
using Microsoft.AspNetCore.Server.Kestrel.Https;

// WebApplication 초기화
var builder = WebApplication.CreateBuilder(args);

// ───────── Redis 캐시 설정 ─────────
// StackExchange.Redis 기반의 분산 캐시를 사용하도록 설정
builder.Services.AddStackExchangeRedisCache(options =>
{
    options.Configuration = builder.Configuration["Redis:Configuration"];   // Redis 연결 문자열
    options.InstanceName = "GameServerSession:";                          // Redis 키 앞에 붙일 인스턴스 네임스페이스
});

// ───────── 세션(Stateful) 설정 ─────────
// 서버 측 세션 상태를 Redis에 저장
builder.Services.AddSession(options =>
{
    options.Cookie.Name = ".GameServer.Session";           // 세션 쿠키 이름
    options.IdleTimeout = TimeSpan.FromHours(1);           // 1시간 동안 요청 없으면 세션 만료
    options.Cookie.HttpOnly = true;                            // 클라이언트 자바스크립트에서 쿠키 접근 불가
    options.Cookie.SecurePolicy = CookieSecurePolicy.Always;       // HTTPS 연결에서만 쿠키 전송
    options.Cookie.SameSite = SameSiteMode.Strict;             // 엄격한 SameSite 정책
    // options.Cookie.IsEssential = true;                         // GDPR 등에서 필수 쿠키로 표시할 때 사용
});

// ───────── HTTPS용 인증서 로드 ─────────
// 프로젝트 루트의 certs 폴더에 있는 server.pfx 인증서 사용
var baseDir = AppContext.BaseDirectory;
var projectRoot = Directory.GetParent(baseDir)!.Parent!.Parent!.Parent!.FullName;
var certPath = Path.Combine(projectRoot, "certs", "server.pfx");
var cert = new X509Certificate2(
   certPath,
   "PfxPassWordFlashCtrlZ!#%"    // 인증서 비밀번호
);

// Kestrel 서버 설정
builder.WebHost.ConfigureKestrel(opts =>
{
    // HTTP 1.1 포트 5000 바인딩
    opts.ListenAnyIP(5000, listen => listen.Protocols = HttpProtocols.Http1);

    // HTTPS 기본 인증서 지정
    opts.ConfigureHttpsDefaults(httpsOptions =>
    {
        httpsOptions.ServerCertificate = cert;
    });

    // HTTPS 포트 5001 바인딩 (UseHttps()를 통해 인증서 자동 적용)
    opts.ListenAnyIP(5001, lo => lo.UseHttps());
});

// ───────── EF Core + MySQL 설정 ─────────
// GameDbContext를 MySQL에 연결 (8.0.31 버전), 재시도 정책 포함
builder.Services.AddDbContext<GameDbContext>(opts =>
    opts.UseMySql(
        builder.Configuration.GetConnectionString("Default"),
        new MySqlServerVersion(new Version(8, 0, 31)),
        mySqlOpts => mySqlOpts.EnableRetryOnFailure(
            maxRetryCount: 5,                    // 최대 5회 재시도
            maxRetryDelay: TimeSpan.FromSeconds(10),
            errorNumbersToAdd: null
        )
    )
);

// ───────── Firebase Admin SDK 초기화 ─────────
FirebaseApp.Create(new AppOptions
{
    Credential = GoogleCredential.FromFile("google-service-account.json")
});

// ───────── DI(의존성 주입) 설정 ─────────
// IUserService 구현체 등록
builder.Services.AddScoped<IUserService, UserService>();
// IContentsService 구현체 등록
builder.Services.AddScoped<IContentsService, ContentsService>();

// ───────── 컨트롤러, JSON 직렬화 설정 ─────────
// MVC 컨트롤러 + Newtonsoft.Json으로 JSON 직렬화/역직렬화
builder.Services.AddControllers().AddNewtonsoftJson();

// Swagger/OpenAPI 설정
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// 인증·인가 설정
builder.Services.AddAuthorization();

var app = builder.Build();

// ───────── 미들웨어 파이프라인 ─────────

// 1) 모든 요청에 대해 로그 출력
app.Use(async (ctx, next) =>
{
    Console.WriteLine($"[Incoming] {ctx.Request.Method} {ctx.Request.Path}");
    await next();
});

// 2) 개발 환경에서 Swagger UI 활성화
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// 3) HTTP → HTTPS 리다이렉션
app.UseHttpsRedirection();

// 4) 세션 사용 (AddSession() 후에 호출해야 함)
app.UseSession();

// 5) /api/auth/** 요청은 Firebase JWT 인증 흐름 사용
app.UseWhen(
    ctx => ctx.Request.Path.StartsWithSegments("/api/auth"),
    branch =>
    {
        branch.UseAuthentication();
        branch.UseMiddleware<FirebaseAuthMiddleware>();
    }
);

// 6) 그 외 API 요청은 세션 기반 인증 흐름 사용
app.UseWhen(
    ctx => !ctx.Request.Path.StartsWithSegments("/api/auth"),
    branch =>
    {
        branch.UseAuthentication();
        branch.UseMiddleware<SessionAuthMiddleware>();
    }
);

// 7) 최종적으로 권한 검사 미들웨어
app.UseAuthorization();

// 8) 컨트롤러 라우팅
app.MapControllers();

// 9) 애플리케이션 실행
app.Run();