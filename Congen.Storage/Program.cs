using Azure.Identity;
using Azure.Storage;
using Azure.Storage.Blobs;
using Clerk.Net.AspNetCore.Security;
using Clerk.Net.DependencyInjection;
using Congen.Storage.Business;
using Congen.Storage.Business.Data_Objects.Responses;
using Congen.Storage.Data;
using Congen.Storage.Data.Data_Objects.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Azure;
using Microsoft.Extensions.Primitives;
using Microsoft.Identity.Client;
using System.Linq.Expressions;
using System.Reflection.Metadata;

#region Configuration

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddScoped<IStorageRepo, StorageRepo>();
builder.Services.AddHttpContextAccessor();
ConfigurationManager configuration = builder.Configuration;

builder.Services.AddClerkApiClient(config =>
{
    config.SecretKey = builder.Configuration["Clerk:SecretKey"]!;
});

//setup clerk auth
builder.Services.AddAuthentication(ClerkAuthenticationDefaults.AuthenticationScheme) 
    .AddClerkAuthentication(x =>
    {
        x.Authority = configuration["Clerk:Authority"]!;
        x.AuthorizedParty = configuration["Clerk:AuthorizedParty"]!;
    });

//require auth
builder.Services.AddAuthorizationBuilder()
    .SetFallbackPolicy(new AuthorizationPolicyBuilder()
        .RequireAuthenticatedUser()
        .Build());

Util.ClerkInit(configuration["Clerk:SecretKey"]);

//Initialise Azure Identity and blob service client
Util.InitBlobStorageClient(configuration["Azure:StorageAccountUrl"], new StorageSharedKeyCredential(configuration["Azure:StorageAccountName"], configuration["Azure:SharedKey"]));
await Util.InitRabbit(configuration["Rabbit:User"], configuration["Rabbit:Password"]);

var app = builder.Build();

// Configure the HTTP request pipeline.
app.UseSwagger();
app.UseSwaggerUI();

app.UseHttpsRedirection();

#endregion

#region Blob

app.MapGet("/storage/save-file", async (IHttpContextAccessor context, IStorageRepo repo) =>
{
    SaveFileResponse response = new SaveFileResponse();

    try
    {
        //get auth from headers
        string auth = (string?)context.HttpContext.Request.Headers["Authorization"];

        if(auth == null)
        {
            response.ErrorMessage = "Could not authenticate. Please provide an Authorization token.";
            response.ErrorCode = (int)ErrorCodes.Unauthorized;
            response.IsSuccessful = false;

            return response;
        }

        auth = auth.Replace("Bearer ", "");

        string container = await Util.GetUserContainer(auth);

        if (String.IsNullOrEmpty(container))
        {
            throw new Exception("This account does not have a 'container' value assigned in private metadata. Please resolve and try again");
        }

        IFormFile file = context.HttpContext.Request.Form.Files.FirstOrDefault();

        if (file == null)
        {
            response.IsSuccessful = false;
            response.ErrorCode = (int)ErrorCodes.BadRequest;
            response.ErrorMessage = "BAD REQUEST: INCLUDE FILE IN FORM REQUEST!";

            return response;
        }

        if (!file.FileName.Contains("."))
        {
            response.IsSuccessful = false;
            response.ErrorCode = (int)ErrorCodes.BadRequest;
            response.ErrorMessage = "BAD REQUEST: INCLUDE FILE EXTENSION IN FILE NAME!";

            return response;
        }

        string extension = file.FileName.Split('.').LastOrDefault();

        Stream stream = file.OpenReadStream();

        string fileName = repo.SaveFile(container, stream, extension);

        if(String.IsNullOrEmpty(fileName))
        {
            response.IsSuccessful = false;
            response.ErrorCode = (int)ErrorCodes.FileNotFound;
            response.ErrorMessage = "FILE NOT FOUND: UPLOADED FILE NOT SAVED IN BLOB!";
            return response;
        }

        response.IsSuccessful = true;
    }

    catch(Exception ex)
    {
        response.IsSuccessful = false;
        response.ErrorMessage = ex.Message;
        response.ErrorCode = 500;
    }

    return response;
})
.WithName("Save File")
.WithOpenApi();

#endregion

app.Run();

/*
 * 
 * Service layer (Program Layer). => takes in requests (.Api)
 * Business layer (Apllication Layer). stores request and response object and executes business logic
 * Data layer (Access Layer). => Stores data objects/contracts and reads from database (blob).
 * 
 */