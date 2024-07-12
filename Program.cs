using I3S_API.Middleware;
using Microsoft.Extensions.Localization;
using Microsoft.OpenApi.Models;
using System.Reflection;
using System.Text.Encodings.Web;
using System.Text.Unicode;
using I3S_API.Filter;

var builder = WebApplication.CreateBuilder(new WebApplicationOptions
{
    Args = args,
    WebRootPath = "ClientApp"
});

ConfigurationManager configuration = builder.Configuration;

//CORS設定，從appsettings來
string[] corsOrigins = configuration["CORS:AllowOrigin"].Split(',', StringSplitOptions.RemoveEmptyEntries);
if (corsOrigins.Length > 0)
{
    builder.Services.AddCors(options =>
    {
        options.AddDefaultPolicy(
            builder =>
            {
                if (corsOrigins.Contains("*"))
                {
                    builder.SetIsOriginAllowed(_ => true);
                }
                else
                {
                    builder.WithOrigins(corsOrigins);
                }
                builder.AllowAnyMethod();
                builder.AllowAnyHeader();
                builder.AllowCredentials();

            });
    });
}

//header設定
builder.Services.AddHsts(options =>
{
    options.Preload = true;
    options.IncludeSubDomains = true;
    options.MaxAge = TimeSpan.FromDays(60);
});

// swagger
builder.Services.AddControllers(options =>
{
    options.Filters.Add<ResultFilter>();
});
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "I3S API文件", Version = "v1", Description  = "" });
    var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
    c.IncludeXmlComments(xmlPath);
});

// 通過資安檢測某一項
builder.Services.AddMvcCore().AddMvcOptions(options =>
{
    var L = builder.Services.BuildServiceProvider().GetService<IStringLocalizer>();
    options.ModelBindingMessageProvider.SetAttemptedValueIsInvalidAccessor((x, y) => $"The value is not valid.");
    options.ModelBindingMessageProvider.SetNonPropertyAttemptedValueIsInvalidAccessor(x => "The value is not valid.");
    options.ModelBindingMessageProvider.SetValueIsInvalidAccessor(x => "The value is invalid.");
    options.ModelBindingMessageProvider.SetValueMustNotBeNullAccessor(x => "The value is invalid.");
    options.ModelBindingMessageProvider.SetValueMustBeANumberAccessor(x => "The value is invalid.");
    options.ModelBindingMessageProvider.SetMissingBindRequiredValueAccessor(x => "The value is invalid.");
    options.ModelBindingMessageProvider.SetUnknownValueIsInvalidAccessor(x => "The value is invalid.");
    options.ModelBindingMessageProvider.SetMissingKeyOrValueAccessor(() => L["The value is invalid."]);
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHsts();
app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseCors();

app.UseMiddleware<ErrorLogMiddleware>();

app.UseWhen(
    context => context.Request.Path.StartsWithSegments("/api"),
    appBuilder =>
    {
        appBuilder.UseMiddleware<MidMiddleware>();
    }
);

app.UseAuthorization();

app.MapControllers();

app.UseSpa(spa =>
{
    spa.Options.SourcePath = "ClientApp";
});

app.Run();
