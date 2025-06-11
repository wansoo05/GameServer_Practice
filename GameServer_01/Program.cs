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

// ������ 1) Kestrel ���ε� ���� ��������������������������������������������������������������
builder.WebHost.ConfigureKestrel(opts =>
{
    // HTTP  : ��� IP�� 5000 ��Ʈ ����
    opts.ListenAnyIP(5000, listen => listen.Protocols = HttpProtocols.Http1);
    opts.ListenAnyIP(5001, (httpsOpt) => { httpsOpt.UseHttps(); });
});

// ������ 2) EF Core + MySQL (��õ� �ɼ� ����) ����������������������������������
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

// ������ 3) Firebase Admin SDK �ʱ�ȭ ��������������������������������������������������
FirebaseApp.Create(new AppOptions
{
    Credential = GoogleCredential.FromFile("google-service-account.json")
});

// ������ 4) ��Ʈ�ѷ���Swagger ��� ����������������������������������������������������������
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

// ������ 5) ���� ȯ���� �� Swagger UI Ȱ��ȭ ����������������������������������������
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// ������ 6) HTTPS �����̷��� ������������������������������������������������������������������
app.UseHttpsRedirection();
//app.UseSession();

//// �α��� / ȸ�����Կ� Firebase ��ū ����
//app.UseWhen(
//    ctx => ctx.Request.Path.StartsWithSegments("/api/auth"),
//   branch =>
//    {
//        // JWT ���� ��Ŵ�� �ִٸ� Firebase ��ū ����
//        branch.UseAuthentication();
//branch.UseMiddleware<FirebaseAuthMiddleware>();
//    }
//);

//// �׿� ��� API : ���� ����
//app.UseWhen(
//    ctx => !ctx.Request.Path.StartsWithSegments("/api/auth"),
//    branch =>
//    {
//        branch.UseMiddleware<SessionAuthMiddleware>();
//    }
//);

app.UseAuthentication();
app.UseMiddleware<FirebaseAuthMiddleware>();

// ������ 8) ����(Authorization) �̵���� ����������������������������������������������
app.UseAuthorization();

// ������ 9) ��Ʈ�ѷ� ����� ������������������������������������������������������������������
app.MapControllers();

app.Run();
