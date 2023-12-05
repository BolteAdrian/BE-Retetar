using Microsoft.AspNetCore.Identity;
using Retetar.Repository;
using Microsoft.EntityFrameworkCore;
using Retetar.Interfaces;
using Retetar.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Identity.Web;
using Retetar.Models;

var builder = WebApplication.CreateBuilder(args);

// Configuration
builder.Configuration.AddJsonFile("appsettings.json"); // Add this line to load appsettings.json

// Add services to the container.
builder.Services.AddDbContext<RecipeDbContext>(options =>
options.UseSqlServer(builder.Configuration.GetConnectionString("Default")));   //use this for SQL connection

builder.Services.AddIdentity<User, IdentityRole>(options =>
{
    // Customize the Identity options as needed
    options.User.AllowedUserNameCharacters = null;
    options.User.RequireUniqueEmail = true;
})
    .AddEntityFrameworkStores<RecipeDbContext>()
    .AddDefaultTokenProviders();

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddMicrosoftIdentityWebApi(builder.Configuration.GetSection("AzureAd"));

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", builder =>
    {
        builder.WithOrigins("http://localhost:4200") // Replace with your Angular app URL
               .AllowAnyMethod()
               .AllowAnyHeader();
    });
});

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("ManagerOnly", policy =>
    {
        policy.RequireRole("Manager");
    });
});

builder.Services.AddScoped<UserService>();
builder.Services.AddScoped<RecipeService>();
builder.Services.AddScoped<IngredientService>();
builder.Services.AddScoped<CategoryService>();
builder.Services.AddScoped<IngredientQuantitiesService>();

builder.Services.Configure<IEmailConfiguration>(builder.Configuration.GetSection("EmailConfiguration")); // Register EmailConfiguration
builder.Services.AddTransient<IEmailSender, EmailSender>(); // Register EmailSender

builder.Services.AddControllers();

// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.Configure<IJwtOptions>(builder.Configuration.GetSection("Jwt"));

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors("AllowFrontend");

app.UseHttpsRedirection();

app.UseAuthentication();

app.UseAuthorization();

app.MapControllers();

app.Run();
