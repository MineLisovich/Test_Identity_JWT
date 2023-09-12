using Microservice.Authorization.Data;
using Microservice.Authorization.Data.Entities;
using Microservice.Authorization.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

var builder = WebApplication.CreateBuilder(args);

var configuration = builder.Configuration.GetValue<string>("DbConnection");
builder.Services.AddDbContext<AuthDbContext>(x => x.UseSqlServer(configuration));
builder.Services.AddTransient<AuthOptions>();

builder.Services.AddIdentity<IdentityUser, IdentityRole>(conf =>
{
    //������ ������
    conf.Password.RequiredLength = 4;
    //���������� � ������
    conf.Password.RequireDigit = false;
    //���������� � ���� ��������
    conf.Password.RequireNonAlphanumeric = false;
    //���������� � ��������� ������
    conf.Password.RequireUppercase = false;
}).AddEntityFrameworkStores<AuthDbContext>().AddDefaultTokenProviders();

builder.Services.ConfigureApplicationCookie(conf =>
{
    conf.Cookie.Name = "Microcervice.Authorization.Cookie";
    conf.LoginPath = "/Auth/Login";
    conf.LogoutPath = "/Auth/Logout";
    conf.ExpireTimeSpan = TimeSpan.FromMinutes(120);
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

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultSignInScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;

}).AddJwtBearer(JwtBearerDefaults.AuthenticationScheme,
    option =>
    {
        option.RequireHttpsMetadata = false; // �����  ���� ����� false, �� SSL ��� �������� ������ �� ������������. ������ ������ ������� ���������� ������ �� ������������. � �������� ���������� ��� �� ����� ������������ �������� ������ �� ��������� https.
        option.SaveToken = true;
        option.TokenValidationParameters = new TokenValidationParameters
        {
            //���������, ����� �� �������������� ��������� ��� ��������� ������
            ValidateIssuer = true,
            //������ �������������� ��������
            ValidIssuer = AuthOptions.ISSUER,

            //����� �� �������������� ����������� ������
            ValidateAudience = true,
            //��������� ����������� ������
            ValidAudience = AuthOptions.AUDIENCE,

            //����� �� �������������� ����� ������������� ������
            ValidateLifetime = true,

            //��������� ���������� �����
            IssuerSigningKey = AuthOptions.GetSymmetricSecurityKey(),
            //��������� ���������� �����
            ValidateIssuerSigningKey = true,
        };
    });

builder.Services.AddAuthorization(conf =>
{
    conf.AddPolicy("AdminArea", policy =>
    {
        policy.RequireRole(UserRoles.Admin);
    });
    conf.AddPolicy("UserArea", policy =>
    {
        policy.RequireRole(UserRoles.User);
    });
});

builder.Services.AddControllersWithViews(conf =>
{
    conf.Conventions.Add(new AdminAreaAuthorization(UserRoles.Admin, "AdminArea"));
    conf.Conventions.Add(new UserAreaAuthorization(UserRoles.User, "UserArea"));
}).AddSessionStateTempDataProvider();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseCors("AllowAll");
app.UseRouting();

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
app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "Admin",
    pattern: "{area:exists}/{controller=Home}/{action=Index}/{id?}");
app.MapControllerRoute(
    name: "User",
    pattern: "{area:exists}/{controller=Home}/{action=Index}/{id?}");
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
