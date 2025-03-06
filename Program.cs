using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;
using QuizWebApi.Data;
using QuizWebApi.Services; // <-- dla QuestionLoader (jeśli używasz)
using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;
// ewentualnie
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.EntityFrameworkCore;
using QuizWebApi.Data;
using QuizWebApi.Services;
using System.Text.Json.Serialization;

var builder = WebApplication.CreateBuilder(args);

// Rejestracja kontrolerów
builder.Services.AddControllers()
     .AddJsonOptions(options =>
     {
         options.JsonSerializerOptions.ReferenceHandler = System.Text.Json.Serialization.ReferenceHandler.IgnoreCycles;
     }); ;

// Rejestracja usług potrzebnych do swaggera
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Rejestracja DbContext (pamiętaj, aby connection string był poprawny)
builder.Services.AddDbContext<QuizDbContext>(options =>
    options.UseSqlServer(builder.Configuration["ConnectionStrings:QuizDB"]));

var app = builder.Build();

// Umożliwienie serwowania statycznych plików z folderu wwwroot
app.UseStaticFiles();

// Jednorazowe utworzenie bazy i (opcjonalnie) załadowanie pytań
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<QuizDbContext>();
    db.Database.EnsureCreated();
    // Opcjonalnie: Załaduj pytania z plików
    // var loader = new QuestionLoader(db);
    // loader.LoadQuestionsFromFile(@"C:\path\to\a.txt");
    // loader.LoadQuestionsFromFile(@"C:\path\to\b.txt");
    // loader.LoadQuestionsFromFile(@"C:\path\to\c.txt");
    // loader.LoadQuestionsFromFile(@"C:\path\to\d.txt");
}

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseAuthorization();

app.MapControllers();

app.Run();
