using Microservice.Admin.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Net;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddTransient<AuthOptions>();

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultSignInScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;

}).AddJwtBearer(JwtBearerDefaults.AuthenticationScheme,
    option =>
    {
        option.RequireHttpsMetadata = false; // елсли  если равно false, то SSL при отправке токена не используется. Однако данный вариант установлен только дя тестирования. В реальном приложении все же лучше использовать передачу данных по протоколу https.
        option.SaveToken = true;
        option.TokenValidationParameters = new TokenValidationParameters
        {
            //указывает, будет ли валидироваться создатель при валидации токена
            ValidateIssuer = true,
            //строка представляющая издателя
            ValidIssuer = AuthOptions.ISSUER,

            //будет ли валидироваться потребитель токена
            ValidateAudience = true,
            //установка потребителя токена
            ValidAudience = AuthOptions.AUDIENCE,

            //будет ли валидироваться время существования токена
            ValidateLifetime = true,

            //установка секретного ключа
            IssuerSigningKey = AuthOptions.GetSymmetricSecurityKey(),
            //валидация секретного ключа
            ValidateIssuerSigningKey = true,
        };
    });

builder.Services.AddCors(conf =>
{
    conf.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyHeader();
        policy.AllowAnyMethod();
        policy.AllowAnyOrigin();
    });
});




var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}
app.UseHttpsRedirection();
app.UseCors("AllowAll");
app.Use(async (context, next) =>
{
    var token = context.Request.Cookies["Microcervice.Authorization.Cookie"];
    if (!string.IsNullOrEmpty(token))
        context.Request.Headers.Add("Authorization", "Bearer " + token);
    context.Response.Headers.Add("X-Content-Type-Options", "nosniff");
    context.Response.Headers.Add("X-Xss-Protection", "1");
    context.Response.Headers.Add("X-Frame-Options", "DENY");
    await next();
});
app.UseStatusCodePages(async context =>
{
    var request = context.HttpContext.Request;
    var response = context.HttpContext.Response;

    if (response.StatusCode == (int)HttpStatusCode.Unauthorized)
    {
        response.Redirect("https://localhost:7291/Auth/Login");
    }
});
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
