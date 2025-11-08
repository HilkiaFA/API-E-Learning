using E_Learning_Quiz.Data;
using E_Learning_Quiz.Services;
using Microsoft.EntityFrameworkCore;
using OfficeOpenXml;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddScoped<IPasswordService, PasswordService>();
builder.Services.AddDbContext<DBText>(options =>
    options.UseSqlServer("Data Source=.\\sqlexpress;Initial Catalog=E_LEARNING_DB;Integrated Security=True;Trust Server Certificate=True"));
ExcelPackage.LicenseContext = OfficeOpenXml.LicenseContext.NonCommercial;
var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
       
        c.DefaultModelsExpandDepth(-1); 
    });
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
