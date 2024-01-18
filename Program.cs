using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddDbContext<TodoDbContext>(options =>
    options.UseInMemoryDatabase("TodoDb"));
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddCors(o => o.AddPolicy("MyPolicy", builder =>
        {
            builder.AllowAnyOrigin()
                   .AllowAnyMethod()
                   .AllowAnyHeader();
        }));

var app = builder.Build();
app.UseCors("MyPolicy");
// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.MapGet("/todoitems", async (TodoDbContext db) =>
    await db.TodoItems.ToListAsync());

app.MapGet("/todoitems/{id}", async (TodoDbContext db, int id) =>
    await db.TodoItems.FindAsync(id) is TodoItem todo
        ? Results.Ok(todo)
        : Results.NotFound());

app.MapPost("/todoitems", async (TodoDbContext db, TodoItemRequestDto request) =>
{   
    int id = db.TodoItems.Count() + 1;
    TodoItem item = new(id, request.Name, request.IsComplete);
    db.TodoItems.Add(item);
    await db.SaveChangesAsync();

    return Results.Created($"/todoitems/{id}", item);
});

app.MapPut("/todoitems/{id}", async (TodoDbContext db, int id, TodoItemRequestDto inputTodo) =>
{
    var todo = await db.TodoItems.FindAsync(id);
    if (todo is null) return Results.NotFound();
    var updated = todo with { Name = inputTodo.Name, IsComplete = inputTodo.IsComplete};

    db.TodoItems.Remove(todo);
    await db.TodoItems.AddAsync(updated);
    await db.SaveChangesAsync();
    return Results.NoContent();
});

app.MapDelete("/todoitems/{id}", async (TodoDbContext db, int id) =>
{
    var todo = await db.TodoItems.FindAsync(id);
    if (todo is null) return Results.NotFound();
    db.TodoItems.Remove(todo);

    await db.SaveChangesAsync();
    return Results.NoContent();
});

app.Run();

public record TodoItem(int Id, string Name, bool IsComplete);
public record TodoItemRequestDto(string Name, bool IsComplete);

public class TodoDbContext : DbContext
{
    public TodoDbContext(DbContextOptions<TodoDbContext> options)
        : base(options) { }
    public DbSet<TodoItem> TodoItems { get; set; }
}
