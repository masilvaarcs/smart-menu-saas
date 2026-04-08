using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Scalar.AspNetCore;
using SmartMenu.Api.Data;
using SmartMenu.Api.Services;
using SmartMenu.Shared.DTOs;
using SmartMenu.Shared.Models;

var builder = WebApplication.CreateBuilder(args);

// ========================
// SERVIÇOS
// ========================

// Banco de dados SQLite
builder.Services.AddDbContext<SmartMenuDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection")
        ?? "Data Source=smartmenu.db"));

// Autenticação JWT
var jwtChave = builder.Configuration["Jwt:Chave"]
    ?? throw new InvalidOperationException("Jwt:Chave não configurada em appsettings.json");

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = builder.Configuration["Jwt:Emissor"],
            ValidAudience = builder.Configuration["Jwt:Audiencia"],
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtChave))
        };
    });

builder.Services.AddAuthorization();

// Serviços personalizados
builder.Services.AddScoped<TokenService>();
builder.Services.AddScoped<QrCodeService>();

// CORS para Blazor WASM
builder.Services.AddCors(options =>
{
    options.AddPolicy("BlazorClient", policy =>
    {
        policy.WithOrigins(
                "https://localhost:5002", "http://localhost:5003",
                "http://localhost:5123", "https://localhost:7120")
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});

// Swagger / OpenAPI
builder.Services.AddOpenApi();

var app = builder.Build();

// ========================
// PIPELINE
// ========================

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();
}

app.MapGet("/", () => Results.Redirect("/scalar/v1")).ExcludeFromDescription();
app.MapGet("/health", () => Results.Ok(new { status = "healthy", timestamp = DateTime.UtcNow })).WithTags("Health").AllowAnonymous();

app.UseCors("BlazorClient");
app.UseAuthentication();
app.UseAuthorization();

// Criar banco e aplicar seed automaticamente
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<SmartMenuDbContext>();
    db.Database.EnsureCreated();
}

// ========================
// ENDPOINTS: AUTH
// ========================

var auth = app.MapGroup("/api/auth").WithTags("Autenticação");

auth.MapPost("/registrar", async (RegistroRequest req, SmartMenuDbContext db, TokenService tokenService) =>
{
    if (await db.Usuarios.AnyAsync(u => u.Email == req.Email))
        return Results.Conflict(new { mensagem = "E-mail já cadastrado." });

    var usuario = new Usuario
    {
        Nome = req.Nome,
        Email = req.Email,
        SenhaHash = BCrypt.Net.BCrypt.HashPassword(req.Senha)
    };

    db.Usuarios.Add(usuario);
    await db.SaveChangesAsync();

    var token = tokenService.GerarToken(usuario.Id, usuario.Nome, usuario.Email);
    return Results.Created($"/api/auth/{usuario.Id}", new LoginResponse(
        token, usuario.Nome, usuario.Email, DateTime.UtcNow.AddHours(24)));
})
.WithName("Registrar")
.Produces<LoginResponse>(StatusCodes.Status201Created)
.AllowAnonymous();

auth.MapPost("/login", async (LoginRequest req, SmartMenuDbContext db, TokenService tokenService) =>
{
    var usuario = await db.Usuarios.FirstOrDefaultAsync(u => u.Email == req.Email && u.Ativo);
    if (usuario is null || !BCrypt.Net.BCrypt.Verify(req.Senha, usuario.SenhaHash))
        return Results.Unauthorized();

    var token = tokenService.GerarToken(usuario.Id, usuario.Nome, usuario.Email);
    return Results.Ok(new LoginResponse(
        token, usuario.Nome, usuario.Email, DateTime.UtcNow.AddHours(24)));
})
.WithName("Login")
.Produces<LoginResponse>()
.AllowAnonymous();

// ========================
// ENDPOINTS: RESTAURANTES (autenticado)
// ========================

var restaurantes = app.MapGroup("/api/restaurantes")
    .WithTags("Restaurantes")
    .RequireAuthorization();

restaurantes.MapGet("/", async (ClaimsPrincipal user, SmartMenuDbContext db) =>
{
    var userId = int.Parse(user.FindFirstValue(ClaimTypes.NameIdentifier)!);
    var lista = await db.Restaurantes
        .Where(r => r.UsuarioId == userId)
        .Include(r => r.Categorias)
            .ThenInclude(c => c.Itens)
        .ToListAsync();

    return Results.Ok(lista.Select(r => new RestauranteResponse(
        r.Id, r.Nome, r.Descricao, r.Endereco, r.Telefone,
        r.Slug, r.LogoUrl, r.CorPrimaria, r.Plano.ToString(),
        r.Ativo, r.CriadoEm,
        r.Categorias.Sum(c => c.Itens.Count)
    )));
})
.WithName("ListarRestaurantes");

restaurantes.MapGet("/{id:int}", async (int id, ClaimsPrincipal user, SmartMenuDbContext db) =>
{
    var userId = int.Parse(user.FindFirstValue(ClaimTypes.NameIdentifier)!);
    var r = await db.Restaurantes
        .Include(r => r.Categorias).ThenInclude(c => c.Itens)
        .FirstOrDefaultAsync(r => r.Id == id && r.UsuarioId == userId);

    if (r is null) return Results.NotFound();

    return Results.Ok(new RestauranteResponse(
        r.Id, r.Nome, r.Descricao, r.Endereco, r.Telefone,
        r.Slug, r.LogoUrl, r.CorPrimaria, r.Plano.ToString(),
        r.Ativo, r.CriadoEm,
        r.Categorias.Sum(c => c.Itens.Count)));
})
.WithName("ObterRestaurante");

restaurantes.MapPost("/", async (RestauranteRequest req, ClaimsPrincipal user, SmartMenuDbContext db) =>
{
    var userId = int.Parse(user.FindFirstValue(ClaimTypes.NameIdentifier)!);

    // Verificar limite do plano Free (1 restaurante)
    var totalExistentes = await db.Restaurantes.CountAsync(r => r.UsuarioId == userId);
    if (totalExistentes >= 1)
    {
        var temPro = await db.Restaurantes.AnyAsync(r =>
            r.UsuarioId == userId && r.Plano == SmartMenu.Shared.Enums.PlanoAssinatura.Pro);
        if (!temPro)
            return Results.BadRequest(new { mensagem = "Plano gratuito permite apenas 1 restaurante. Faça upgrade para Pro." });
    }

    if (await db.Restaurantes.AnyAsync(r => r.Slug == req.Slug))
        return Results.Conflict(new { mensagem = "Slug já está em uso. Escolha outro." });

    var restaurante = new Restaurante
    {
        Nome = req.Nome,
        Descricao = req.Descricao ?? "",
        Endereco = req.Endereco ?? "",
        Telefone = req.Telefone ?? "",
        Slug = req.Slug,
        LogoUrl = req.LogoUrl ?? "",
        CorPrimaria = req.CorPrimaria ?? "#E53935",
        UsuarioId = userId
    };

    db.Restaurantes.Add(restaurante);
    await db.SaveChangesAsync();

    return Results.Created($"/api/restaurantes/{restaurante.Id}", new RestauranteResponse(
        restaurante.Id, restaurante.Nome, restaurante.Descricao, restaurante.Endereco,
        restaurante.Telefone, restaurante.Slug, restaurante.LogoUrl, restaurante.CorPrimaria,
        restaurante.Plano.ToString(), restaurante.Ativo, restaurante.CriadoEm, 0));
})
.WithName("CriarRestaurante");

restaurantes.MapPut("/{id:int}", async (int id, RestauranteRequest req, ClaimsPrincipal user, SmartMenuDbContext db) =>
{
    var userId = int.Parse(user.FindFirstValue(ClaimTypes.NameIdentifier)!);
    var restaurante = await db.Restaurantes.FirstOrDefaultAsync(r => r.Id == id && r.UsuarioId == userId);
    if (restaurante is null) return Results.NotFound();

    restaurante.Nome = req.Nome;
    restaurante.Descricao = req.Descricao ?? "";
    restaurante.Endereco = req.Endereco ?? "";
    restaurante.Telefone = req.Telefone ?? "";
    restaurante.LogoUrl = req.LogoUrl ?? "";
    restaurante.CorPrimaria = req.CorPrimaria ?? "#E53935";

    await db.SaveChangesAsync();
    return Results.NoContent();
})
.WithName("AtualizarRestaurante");

restaurantes.MapDelete("/{id:int}", async (int id, ClaimsPrincipal user, SmartMenuDbContext db) =>
{
    var userId = int.Parse(user.FindFirstValue(ClaimTypes.NameIdentifier)!);
    var restaurante = await db.Restaurantes.FirstOrDefaultAsync(r => r.Id == id && r.UsuarioId == userId);
    if (restaurante is null) return Results.NotFound();

    db.Restaurantes.Remove(restaurante);
    await db.SaveChangesAsync();
    return Results.NoContent();
})
.WithName("RemoverRestaurante");

// QR Code
restaurantes.MapGet("/{id:int}/qrcode", async (int id, ClaimsPrincipal user, SmartMenuDbContext db, QrCodeService qrService) =>
{
    var userId = int.Parse(user.FindFirstValue(ClaimTypes.NameIdentifier)!);
    var restaurante = await db.Restaurantes.FirstOrDefaultAsync(r => r.Id == id && r.UsuarioId == userId);
    if (restaurante is null) return Results.NotFound();

    var qrBase64 = qrService.GerarQrCodeBase64(restaurante.Slug);
    return Results.Ok(new { qrCode = $"data:image/png;base64,{qrBase64}", slug = restaurante.Slug });
})
.WithName("GerarQrCode");

// ========================
// ENDPOINTS: CATEGORIAS (autenticado)
// ========================

var categorias = app.MapGroup("/api/restaurantes/{restauranteId:int}/categorias")
    .WithTags("Categorias")
    .RequireAuthorization();

categorias.MapGet("/", async (int restauranteId, ClaimsPrincipal user, SmartMenuDbContext db) =>
{
    var userId = int.Parse(user.FindFirstValue(ClaimTypes.NameIdentifier)!);
    if (!await db.Restaurantes.AnyAsync(r => r.Id == restauranteId && r.UsuarioId == userId))
        return Results.NotFound();

    var lista = await db.Categorias
        .Where(c => c.RestauranteId == restauranteId && c.Ativo)
        .Include(c => c.Itens)
        .OrderBy(c => c.Ordem)
        .ToListAsync();

    return Results.Ok(lista.Select(c => new CategoriaResponse(
        c.Id, c.Nome, c.Descricao, c.Icone, c.Ordem, c.Ativo, c.Itens.Count)));
})
.WithName("ListarCategorias");

categorias.MapPost("/", async (int restauranteId, CategoriaRequest req, ClaimsPrincipal user, SmartMenuDbContext db) =>
{
    var userId = int.Parse(user.FindFirstValue(ClaimTypes.NameIdentifier)!);
    if (!await db.Restaurantes.AnyAsync(r => r.Id == restauranteId && r.UsuarioId == userId))
        return Results.NotFound();

    var categoria = new Categoria
    {
        Nome = req.Nome,
        Descricao = req.Descricao ?? "",
        Icone = req.Icone ?? "🍽️",
        Ordem = req.Ordem,
        RestauranteId = restauranteId
    };

    db.Categorias.Add(categoria);
    await db.SaveChangesAsync();

    return Results.Created($"/api/restaurantes/{restauranteId}/categorias/{categoria.Id}",
        new CategoriaResponse(categoria.Id, categoria.Nome, categoria.Descricao,
            categoria.Icone, categoria.Ordem, categoria.Ativo, 0));
})
.WithName("CriarCategoria");

categorias.MapPut("/{categoriaId:int}", async (int restauranteId, int categoriaId, CategoriaRequest req, ClaimsPrincipal user, SmartMenuDbContext db) =>
{
    var userId = int.Parse(user.FindFirstValue(ClaimTypes.NameIdentifier)!);
    if (!await db.Restaurantes.AnyAsync(r => r.Id == restauranteId && r.UsuarioId == userId))
        return Results.NotFound();

    var categoria = await db.Categorias.FirstOrDefaultAsync(c => c.Id == categoriaId && c.RestauranteId == restauranteId);
    if (categoria is null) return Results.NotFound();

    categoria.Nome = req.Nome;
    categoria.Descricao = req.Descricao ?? "";
    categoria.Icone = req.Icone ?? "🍽️";
    categoria.Ordem = req.Ordem;

    await db.SaveChangesAsync();
    return Results.NoContent();
})
.WithName("AtualizarCategoria");

categorias.MapDelete("/{categoriaId:int}", async (int restauranteId, int categoriaId, ClaimsPrincipal user, SmartMenuDbContext db) =>
{
    var userId = int.Parse(user.FindFirstValue(ClaimTypes.NameIdentifier)!);
    if (!await db.Restaurantes.AnyAsync(r => r.Id == restauranteId && r.UsuarioId == userId))
        return Results.NotFound();

    var categoria = await db.Categorias.FirstOrDefaultAsync(c => c.Id == categoriaId && c.RestauranteId == restauranteId);
    if (categoria is null) return Results.NotFound();

    db.Categorias.Remove(categoria);
    await db.SaveChangesAsync();
    return Results.NoContent();
})
.WithName("RemoverCategoria");

// ========================
// ENDPOINTS: ITENS DO CARDÁPIO (autenticado)
// ========================

var itens = app.MapGroup("/api/restaurantes/{restauranteId:int}/categorias/{categoriaId:int}/itens")
    .WithTags("Itens do Cardápio")
    .RequireAuthorization();

itens.MapGet("/", async (int restauranteId, int categoriaId, ClaimsPrincipal user, SmartMenuDbContext db) =>
{
    var userId = int.Parse(user.FindFirstValue(ClaimTypes.NameIdentifier)!);
    if (!await db.Restaurantes.AnyAsync(r => r.Id == restauranteId && r.UsuarioId == userId))
        return Results.NotFound();

    var lista = await db.ItensCardapio
        .Where(i => i.CategoriaId == categoriaId)
        .OrderBy(i => i.Ordem)
        .ToListAsync();

    return Results.Ok(lista.Select(i => new ItemCardapioResponse(
        i.Id, i.Nome, i.Descricao, i.Preco, i.ImagemUrl,
        i.Disponivel, i.Destaque, i.Tags, i.Ordem)));
})
.WithName("ListarItens");

itens.MapPost("/", async (int restauranteId, int categoriaId, ItemCardapioRequest req, ClaimsPrincipal user, SmartMenuDbContext db) =>
{
    var userId = int.Parse(user.FindFirstValue(ClaimTypes.NameIdentifier)!);
    var restaurante = await db.Restaurantes
        .Include(r => r.Categorias).ThenInclude(c => c.Itens)
        .FirstOrDefaultAsync(r => r.Id == restauranteId && r.UsuarioId == userId);

    if (restaurante is null) return Results.NotFound();

    // Verificar limite do plano Free (15 itens)
    if (restaurante.Plano == SmartMenu.Shared.Enums.PlanoAssinatura.Gratis)
    {
        var totalItens = restaurante.Categorias.Sum(c => c.Itens.Count);
        if (totalItens >= 15)
            return Results.BadRequest(new { mensagem = "Plano gratuito permite até 15 itens. Faça upgrade para Pro." });
    }

    if (!await db.Categorias.AnyAsync(c => c.Id == categoriaId && c.RestauranteId == restauranteId))
        return Results.NotFound();

    var item = new ItemCardapio
    {
        Nome = req.Nome,
        Descricao = req.Descricao ?? "",
        Preco = req.Preco,
        ImagemUrl = req.ImagemUrl ?? "",
        Disponivel = req.Disponivel,
        Destaque = req.Destaque,
        Tags = req.Tags ?? "",
        Ordem = req.Ordem,
        CategoriaId = categoriaId
    };

    db.ItensCardapio.Add(item);
    await db.SaveChangesAsync();

    return Results.Created($"/api/restaurantes/{restauranteId}/categorias/{categoriaId}/itens/{item.Id}",
        new ItemCardapioResponse(item.Id, item.Nome, item.Descricao, item.Preco,
            item.ImagemUrl, item.Disponivel, item.Destaque, item.Tags, item.Ordem));
})
.WithName("CriarItem");

itens.MapPut("/{itemId:int}", async (int restauranteId, int categoriaId, int itemId, ItemCardapioRequest req, ClaimsPrincipal user, SmartMenuDbContext db) =>
{
    var userId = int.Parse(user.FindFirstValue(ClaimTypes.NameIdentifier)!);
    if (!await db.Restaurantes.AnyAsync(r => r.Id == restauranteId && r.UsuarioId == userId))
        return Results.NotFound();

    var item = await db.ItensCardapio.FirstOrDefaultAsync(i => i.Id == itemId && i.CategoriaId == categoriaId);
    if (item is null) return Results.NotFound();

    item.Nome = req.Nome;
    item.Descricao = req.Descricao ?? "";
    item.Preco = req.Preco;
    item.ImagemUrl = req.ImagemUrl ?? "";
    item.Disponivel = req.Disponivel;
    item.Destaque = req.Destaque;
    item.Tags = req.Tags ?? "";
    item.Ordem = req.Ordem;

    await db.SaveChangesAsync();
    return Results.NoContent();
})
.WithName("AtualizarItem");

itens.MapDelete("/{itemId:int}", async (int restauranteId, int categoriaId, int itemId, ClaimsPrincipal user, SmartMenuDbContext db) =>
{
    var userId = int.Parse(user.FindFirstValue(ClaimTypes.NameIdentifier)!);
    if (!await db.Restaurantes.AnyAsync(r => r.Id == restauranteId && r.UsuarioId == userId))
        return Results.NotFound();

    var item = await db.ItensCardapio.FirstOrDefaultAsync(i => i.Id == itemId && i.CategoriaId == categoriaId);
    if (item is null) return Results.NotFound();

    db.ItensCardapio.Remove(item);
    await db.SaveChangesAsync();
    return Results.NoContent();
})
.WithName("RemoverItem");

// ========================
// ENDPOINTS: CARDÁPIO PÚBLICO (sem autenticação)
// ========================

var cardapio = app.MapGroup("/api/menu").WithTags("Cardápio Público");

cardapio.MapGet("/{slug}", async (string slug, SmartMenuDbContext db) =>
{
    var restaurante = await db.Restaurantes
        .Include(r => r.Categorias.Where(c => c.Ativo).OrderBy(c => c.Ordem))
            .ThenInclude(c => c.Itens.Where(i => i.Disponivel).OrderBy(i => i.Ordem))
        .FirstOrDefaultAsync(r => r.Slug == slug && r.Ativo);

    if (restaurante is null)
        return Results.NotFound(new { mensagem = "Cardápio não encontrado." });

    var response = new CardapioPublicoResponse(
        restaurante.Nome, restaurante.Descricao, restaurante.LogoUrl,
        restaurante.CorPrimaria, restaurante.Telefone, restaurante.Endereco,
        restaurante.Categorias.Select(c => new CategoriaPublicaResponse(
            c.Nome, c.Icone,
            c.Itens.Select(i => new ItemPublicoResponse(
                i.Nome, i.Descricao, i.Preco, i.ImagemUrl, i.Destaque, i.Tags
            )).ToList()
        )).ToList()
    );

    return Results.Ok(response);
})
.WithName("ObterCardapioPublico")
.AllowAnonymous();

app.Run();

// Para testes de integração
public partial class Program { }
