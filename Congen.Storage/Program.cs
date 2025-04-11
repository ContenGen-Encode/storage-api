using Azure.Identity;
using Azure.Storage;
using Azure.Storage.Blobs;
using Congen.Storage.Business;
using Congen.Storage.Business.Data_Objects.Responses;
using Congen.Storage.Data;
using Congen.Storage.Data.Data_Objects.Enums;
using Microsoft.Extensions.Azure;
using System.Linq.Expressions;

#region Configuration

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddScoped<IStorageRepo, StorageRepo>();
builder.Services.AddHttpContextAccessor();
ConfigurationManager configuration = builder.Configuration;

//Initialise Azure Identity and blob service client
string storageAccountName = configuration["Azure.StorageAccountName"];
string accountUrl = configuration["Azure.StorageAccountUrl"];
string tenantId = configuration["Azure.TenantId"];
string clientId = configuration["Azure.ClientId"];
string clientSecret = configuration["Azure.ClientSecret"];
string sharedKey = configuration["Azure.SharedKey"];

Util.Init(accountUrl, new StorageSharedKeyCredential(storageAccountName, sharedKey));

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

#endregion

app.Run();

/*
 * 
 * Service layer (Program Layer). => takes in requests (.Api)
 * Business layer (Apllication Layer). stores request and response object and executes business logic
 * Data layer (Access Layer). => Stores data objects/contracts and reads from database (blob).
 * 
 */