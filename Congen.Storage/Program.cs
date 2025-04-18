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
using Congen.Storage.Data.Data_Objects.Models;
using Congen.Storage.Data.Data_Objects.RabbitMQ;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http.Features;
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
            builder.WithOrigins("http://localhost:3000", "https://congen.ofneill.com") // Removed trailing slashes
                   .AllowAnyMethod()
                   .AllowAnyHeader();
        });
});

builder.Services.AddClerkApiClient(config =>
{
    config.SecretKey = builder.Configuration["Clerk:SecretKey"]!;
});

// Setup Clerk auth
builder.Services.AddAuthentication(ClerkAuthenticationDefaults.AuthenticationScheme)
    .AddClerkAuthentication(x =>
    {
        x.Authority = configuration["Clerk:Authority"]!;
    });

// Increase form value count limit to account for audio files
builder.Services.Configure<FormOptions>(options =>
{
    options.ValueCountLimit = int.MaxValue;
});

Util.ClerkInit(configuration["Clerk:SecretKey"]);

// Initialise Azure Identity and blob service client
Util.InitBlobStorageClient(configuration["Azure:StorageAccountUrl"], new StorageSharedKeyCredential(configuration["Azure:StorageAccountName"], configuration["Azure:SharedKey"]));
await Util.InitRabbit(configuration["Rabbit:User"], configuration["Rabbit:Password"]);

var app = builder.Build();

// Handle OPTIONS requests before authentication
app.Use(async (context, next) =>
{
    if (context.Request.Method == "OPTIONS")
    {
        context.Response.Headers.Add("Access-Control-Allow-Origin", "*");
        context.Response.Headers.Add("Access-Control-Allow-Methods", "GET, POST, OPTIONS");
        context.Response.Headers.Add("Access-Control-Allow-Headers", "Authorization, Content-Type");
        context.Response.StatusCode = 200;
        await context.Response.WriteAsync("OK");
        return;
    }
    await next();
});

app.UseCors("AllowAll");
app.UseAuthentication();
//app.UseAuthorization(); // Add if you plan to use authorization policies
app.UseSwagger();
app.UseSwaggerUI();
app.UseHttpsRedirection();

#endregion

#region Blob


app.MapGet("/storage/service", async (int type, IHttpContextAccessor context, IStorageRepo repo) =>
{
    ServiceResponse response = new ServiceResponse();
    try
    {
        string auth = (string?)context.HttpContext.Request.Headers["Authorization"];

        if (auth == null) throw new Exception("No auth noob");

        auth = auth.Replace("Bearer ", "");

        switch (type) {
            case (int)ServiceTypes.Template:
                // get tempaltes
                Service s1 = new Service();
                s1.Id = 1;
                s1.Url = "https://some-url-to-resource.mp4";
                s1.Type = (int)ServiceTypes.Template;
                response.ServiceData = [s1];
                break;
            
            case (int)ServiceTypes.Music:
                Service s2 = new Service();                
                s2.Id = 1;
                s2.Url = "https://some-url-to-resource.mp3";
                s2.Type = (int)ServiceTypes.Music; 
                response.ServiceData = [s2];
                break;

            case (int)ServiceTypes.Audio:
                Service s3 = new Service();                
                s3.Id = 1;
                s3.Url = "https://some-url-to-resource.mp3";
                s3.Type = (int)ServiceTypes.Audio; 
                response.ServiceData = [s3];
                break;
                
            default:
                throw new Exception("Serice type does not exist");
        }

        response.IsSuccessful = true;
    }
    catch (Exception ex)
    {
        response.ErrorCode = 500;
        response.IsSuccessful = false;
        response.ErrorMessage = "";
    }

    return response;
})
.WithName("Service")
.WithOpenApi();

app.MapGet("/storage/get-file", async (string fileName, IHttpContextAccessor context, IStorageRepo repo) =>
{
    string text = "";

    try
    {
        //get auth from headers
        string auth = (string?)context.HttpContext.Request.Headers["Authorization"];

        if (auth == null) throw new Exception("No auth noob");

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

        if (auth == null) throw new Exception("No auth noob");

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
.WithName("Get Files")
.WithOpenApi();

app.MapPost("/storage/save-file", async (IHttpContextAccessor context, IStorageRepo repo) =>
{
    SaveFileResponse response = new SaveFileResponse();

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

        if (String.IsNullOrEmpty(fileName))
        {
            response.IsSuccessful = false;
            response.ErrorCode = (int)ErrorCodes.FileNotFound;
            response.ErrorMessage = "FILE NOT FOUND: UPLOADED FILE NOT SAVED IN BLOB!";
            return response;
        }

        response.FileName = fileName;
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
            VideoName = request.Video,
            AudioName = request.Audio,
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

app.Run();

/*
 * 
 * Service layer (Program Layer). => takes in requests (.Api)
 * Business layer (Apllication Layer). stores request and response object and executes business logic
 * Data layer (Access Layer). => Stores data objects/contracts and reads from database (blob).
 * 
 */