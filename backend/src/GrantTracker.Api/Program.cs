var builder = WebApplication.CreateBuilder(args);

// OpenAPI description + Swagger UI, so the endpoints can be tried from the browser.
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Services (DbContext, TimeProvider, domain services) are registered here in later steps.

var app = builder.Build();

// Swagger only in Development: it is a tool for developers and reviewers, not for officers.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();    // http://localhost:5080/swagger/v1/swagger.json
    app.UseSwaggerUI();  // http://localhost:5080/swagger
}

// Endpoints are mapped here in later steps.

app.Run();

// Lets the integration tests start the API with WebApplicationFactory<Program>.
public partial class Program;
