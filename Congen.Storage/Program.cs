using Azure.Identity;
using Azure.Storage;
using Azure.Storage.Blobs;
using Clerk.Net.AspNetCore.Security;
using Clerk.Net.DependencyInjection;
using Congen.Storage.Business;
using Congen.Storage.Business.Data_Objects.Requests;
using Congen.Storage.Business.Data_Objects.Responses;
using Congen.Storage.Data;
using Congen.Storage.Data.Data_Objects.Enums;
using Congen.Storage.Data.Data_Objects.RabbitMQ;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Azure;
using Microsoft.Extensions.Primitives;
using Microsoft.Identity.Client;
using Newtonsoft.Json;
using System.Linq.Expressions;
using System.Reflection.Metadata;

#region Configuration

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddScoped<IStorageRepo, StorageRepo>();
builder.Services.AddScoped<IAIRepo, AIRepo>();
builder.Services.AddHttpContextAccessor();
ConfigurationManager configuration = builder.Configuration;

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll",
        builder =>
        {
            builder.AllowAnyOrigin()
                .AllowAnyMethod()
                .AllowAnyHeader();
        });
});

builder.Services.AddClerkApiClient(config =>
{
    config.SecretKey = builder.Configuration["Clerk:SecretKey"]!;
});

//setup clerk auth
builder.Services.AddAuthentication(ClerkAuthenticationDefaults.AuthenticationScheme)
    .AddClerkAuthentication(x =>
    {
        x.Authority = configuration["Clerk:Authority"]!;
    });

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

app.MapGet("/storage/get-file", async (string fileName, IHttpContextAccessor context, IStorageRepo repo) =>
{
    string text = "";

    try
    {
        //get auth from headers
        string auth = (string?)context.HttpContext.Request.Headers["Authorization"];

        auth = auth.Replace("Bearer ", "");

        string container = await Util.GetUserContainer(auth);

        if (String.IsNullOrEmpty(container))
        {
            throw new Exception("This account does not have a 'container' value assigned in private metadata. Please resolve and try again");
        }

        var file = repo.GetFile(container, fileName);

        //get text from file
        StreamReader reader = new StreamReader(file);
        text = reader.ReadToEnd();
    }

    catch (Exception ex)
    {
        return "";
    }

    return text;
})
.WithName("Get File")
.WithOpenApi();

app.MapGet("/storage/get-files", async (string[] fileNames, IHttpContextAccessor context, IStorageRepo repo) =>
{
    Stream[] files = new Stream[fileNames.Length];

    try
    {
        //get auth from headers
        string auth = (string?)context.HttpContext.Request.Headers["Authorization"];

        auth = auth.Replace("Bearer ", "");

        string container = await Util.GetUserContainer(auth);

        if (String.IsNullOrEmpty(container))
        {
            throw new Exception("This account does not have a 'container' value assigned in private metadata. Please resolve and try again");
        }

        files = repo.GetFiles(container, fileNames);
    }

    catch (Exception ex)
    {
        return files;
    }

    return files;
})
.WithName("Get File")
.WithOpenApi();

app.MapPost("/storage/save-file", async (IHttpContextAccessor context, IStorageRepo repo) =>
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

#region AI

app.MapPost("/generate/file", async (int tone, string videoName, string audioName, IHttpContextAccessor context, IStorageRepo repo) =>
{
    ResponseBase response = new ResponseBase();

    try
    {
        //get auth from headers
        string auth = (string?)context.HttpContext.Request.Headers["Authorization"];

        if (auth == null)
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

        var toneName = Enum.GetName(typeof(Tones), tone);

        if (String.IsNullOrEmpty(toneName))
        {
            response.IsSuccessful = false;
            response.ErrorCode = (int)ErrorCodes.BadRequest;
            response.ErrorMessage = "BAD REQUEST: INVALID TONE VALUE!";
            return response;
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

        //save file to blob so it can be referenced later on
        string fileName = repo.SaveFile(container, stream, extension);

        if (String.IsNullOrEmpty(fileName))
        {
            response.IsSuccessful = false;
            response.ErrorCode = (int)ErrorCodes.FileNotFound;
            response.ErrorMessage = "FILE NOT FOUND: UPLOADED FILE NOT SAVED IN BLOB!";
            return response;
        }

        var message = new Message()
        {
            Tone = toneName,
            VideoName = videoName,
            AudioName = audioName,
            FileName = fileName,
            AccessToken = auth,
            UserId = Util.GetUserId(auth)
        };

        await Util.SendMessage(JsonConvert.SerializeObject(message), Util.GetUserId(auth));

        response.IsSuccessful = true;
    }

    catch (Exception ex)
    {
        response.IsSuccessful = false;
        response.ErrorMessage = ex.Message;
        response.ErrorCode = 500;
    }

    return response;
})
.WithName("Generate AI With File")
.WithOpenApi();

app.MapPost("/generate/prompt", async (GenerateVideoRequest request, IHttpContextAccessor context, IStorageRepo repo) =>
{
    ResponseBase response = new ResponseBase();

    try
    {
        //get auth from headers
        string auth = (string?)context.HttpContext.Request.Headers["Authorization"];

        if (auth == null)
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

        var toneName = Enum.GetName(typeof(Tones), request.Tone);

        if (String.IsNullOrEmpty(toneName))
        {
            response.IsSuccessful = false;
            response.ErrorCode = (int)ErrorCodes.BadRequest;
            response.ErrorMessage = "BAD REQUEST: INVALID TONE VALUE!";
            return response;
        }

        var message = new Message()
        {
            Tone = toneName,
            VideoName = request.VideoName,
            AudioName = request.AudioName,
            Prompt = request.Prompt,
            AccessToken = auth,
            UserId = Util.GetUserId(auth)
        };

        //send message to rabbitmq
        await Util.SendMessage(JsonConvert.SerializeObject(message), Util.GetUserId(auth));

        response.IsSuccessful = true;
    }

    catch (Exception ex)
    {
        response.IsSuccessful = false;
        response.ErrorMessage = ex.Message;
        response.ErrorCode = 500;
    }

    return response;
})
.WithName("Generate AI With Prompt")
.WithOpenApi();


#endregion

app.UseCors();

app.Run();

/*
 * 
 * Service layer (Program Layer). => takes in requests (.Api)
 * Business layer (Apllication Layer). stores request and response object and executes business logic
 * Data layer (Access Layer). => Stores data objects/contracts and reads from database (blob).
 * 
 */