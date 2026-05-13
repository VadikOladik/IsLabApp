using Microsoft.Data.SqlClient;

var builder = WebApplication.CreateBuilder(args);

var appName = builder.Configuration["App:Name"] ?? "IsLabApp";
var appVersion = builder.Configuration["App:Version"] ?? "1.0.0";
var connectionString = builder.Configuration.GetConnectionString("Mssql") ?? "";

var app = builder.Build();

app.MapGet("/health", () => new { status = "ok", time = DateTime.Now });
app.MapGet("/version", () => new { name = appName, version = appVersion });

// Эндпоинт /db/ping - реальная проверка подключения к БД
app.MapGet("/db/ping", async () =>
{
    if (string.IsNullOrEmpty(connectionString))
    {
        return Results.Ok(new { status = "error", message = "Connection string is not configured" });
    }

    try
    {
        using (var connection = new SqlConnection(connectionString))
        {
            await connection.OpenAsync();
            return Results.Ok(new { status = "ok", message = "Database connection successful" });
        }
    }
    catch (Exception ex)
    {
        return Results.Ok(new { status = "error", message = $"Database connection failed: {ex.Message}" });
    }
});

// CRUD Заметок
var notes = new List<object>();
int nextId = 1;

app.MapPost("/api/notes", (NoteDto note) =>
{
    if (string.IsNullOrWhiteSpace(note.Title))
        return Results.BadRequest(new { error = "Title is required" });

    if (string.IsNullOrWhiteSpace(note.Text))
        return Results.BadRequest(new { error = "Text is required" });

    if (note.Title.Length > 100)
        return Results.BadRequest(new { error = "Title is too long (max 100 characters)" });

    var newNote = new
    {
        id = nextId++,
        title = note.Title.Trim(),
        text = note.Text.Trim(),
        createdAt = DateTime.Now
    };
    notes.Add(newNote);
    return Results.Created($"/api/notes/{newNote.id}", newNote);
});

app.MapGet("/api/notes", () => notes);

app.MapGet("/api/notes/{id}", (int id) =>
{
    var note = notes.FirstOrDefault(n => ((dynamic)n).id == id);
    return note is not null ? Results.Ok(note) : Results.NotFound();
});

app.MapDelete("/api/notes/{id}", (int id) =>
{
    var note = notes.FirstOrDefault(n => ((dynamic)n).id == id);
    if (note is not null) notes.Remove(note);
    return Results.NoContent();
});

app.Run();

record NoteDto(string Title, string Text);