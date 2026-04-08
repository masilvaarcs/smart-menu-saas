using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using SmartMenu.Api.Data;
using SmartMenu.Api.Services;
using SmartMenu.Shared.DTOs;
using SmartMenu.Shared.Enums;
using SmartMenu.Shared.Models;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace SmartMenu.Tests;

// ==========================================
// TESTES DE MODELO — Validação de entidades
// ==========================================

public class UsuarioTests
{
    [Fact]
    public void Usuario_Novo_DeveInicializarComValoresPadrao()
    {
        var usuario = new Usuario();

        Assert.True(usuario.Ativo);
        Assert.Empty(usuario.Nome);
        Assert.Empty(usuario.Email);
        Assert.Empty(usuario.SenhaHash);
        Assert.NotNull(usuario.Restaurantes);
        Assert.Empty(usuario.Restaurantes);
    }

    [Fact]
    public void Usuario_DeveArmazenarPropriedades()
    {
        var usuario = new Usuario
        {
            Id = 1,
            Nome = "Marcos Silva",
            Email = "marcos@email.com",
            SenhaHash = "hash123",
            Ativo = true
        };

        Assert.Equal(1, usuario.Id);
        Assert.Equal("Marcos Silva", usuario.Nome);
        Assert.Equal("marcos@email.com", usuario.Email);
    }
}

public class RestauranteTests
{
    [Fact]
    public void Restaurante_Novo_DeveTerPlanoGratis()
    {
        var restaurante = new Restaurante();

        Assert.Equal(PlanoAssinatura.Gratis, restaurante.Plano);
        Assert.True(restaurante.Ativo);
        Assert.Equal("#E53935", restaurante.CorPrimaria);
    }

    [Fact]
    public void Restaurante_DeveConterCategorias()
    {
        var restaurante = new Restaurante { Nome = "Test" };
        restaurante.Categorias.Add(new Categoria { Nome = "Pizza" });
        restaurante.Categorias.Add(new Categoria { Nome = "Bebida" });

        Assert.Equal(2, restaurante.Categorias.Count);
    }

    [Fact]
    public void Restaurante_SlugDeveSerConsistente()
    {
        var restaurante = new Restaurante
        {
            Nome = "Pizzaria do Marcos",
            Slug = "pizzaria-do-marcos"
        };

        Assert.Equal("pizzaria-do-marcos", restaurante.Slug);
    }
}

public class CategoriaTests
{
    [Fact]
    public void Categoria_Novo_DeveInicializarComIconePadrao()
    {
        var categoria = new Categoria();

        Assert.Equal("🍽️", categoria.Icone);
        Assert.True(categoria.Ativo);
        Assert.Empty(categoria.Itens);
    }

    [Fact]
    public void Categoria_DeveConterItens()
    {
        var categoria = new Categoria { Nome = "Pizzas" };
        categoria.Itens.Add(new ItemCardapio { Nome = "Margherita", Preco = 42.90m });

        Assert.Single(categoria.Itens);
        Assert.Equal("Margherita", categoria.Itens[0].Nome);
    }
}

public class ItemCardapioTests
{
    [Fact]
    public void ItemCardapio_Novo_DeveEstarDisponivel()
    {
        var item = new ItemCardapio();

        Assert.True(item.Disponivel);
        Assert.False(item.Destaque);
        Assert.Equal(0m, item.Preco);
    }

    [Fact]
    public void ItemCardapio_DeveArmazenarPrecoDecimal()
    {
        var item = new ItemCardapio
        {
            Nome = "Pizza Especial",
            Preco = 59.90m,
            Tags = "premium,destaque"
        };

        Assert.Equal(59.90m, item.Preco);
        Assert.Equal("premium,destaque", item.Tags);
    }
}

// ==========================================
// TESTES DE DTOs — Records imutáveis
// ==========================================

public class DtoTests
{
    [Fact]
    public void RegistroRequest_DeveCriarComPropriedades()
    {
        var req = new RegistroRequest("Marcos", "marcos@email.com", "senha123");

        Assert.Equal("Marcos", req.Nome);
        Assert.Equal("marcos@email.com", req.Email);
        Assert.Equal("senha123", req.Senha);
    }

    [Fact]
    public void LoginResponse_DeveCriarComToken()
    {
        var resp = new LoginResponse("jwt-token", "Marcos", "marcos@email.com", DateTime.UtcNow);

        Assert.Equal("jwt-token", resp.Token);
        Assert.Equal("Marcos", resp.Nome);
    }

    [Fact]
    public void RestauranteResponse_DeveConterTotalItens()
    {
        var resp = new RestauranteResponse(
            1, "Pizzaria", "Desc", "End", "Fone", "slug", "", "#E53935", "Pro", true, DateTime.UtcNow, 15);

        Assert.Equal(15, resp.TotalItens);
        Assert.Equal("Pro", resp.Plano);
    }

    [Fact]
    public void CategoriaResponse_DeveMapearCorretamente()
    {
        var resp = new CategoriaResponse(1, "Pizzas", "Tradicionais", "🍕", 1, true, 5);

        Assert.Equal("🍕", resp.Icone);
        Assert.Equal(5, resp.TotalItens);
    }

    [Fact]
    public void CardapioPublicoResponse_DeveConterCategoriasAninhadas()
    {
        var itens = new List<ItemPublicoResponse>
        {
            new("Margherita", "Molho e queijo", 42.90m, "", true, "vegetariano")
        };
        var categorias = new List<CategoriaPublicaResponse>
        {
            new("Pizzas", "🍕", itens)
        };
        var resp = new CardapioPublicoResponse("Pizzaria", "Desc", "", "#E53935", "51-999", "Rua X", categorias);

        Assert.Single(resp.Categorias);
        Assert.Single(resp.Categorias[0].Itens);
        Assert.True(resp.Categorias[0].Itens[0].Destaque);
    }
}

// ==========================================
// TESTES DE ENUM
// ==========================================

public class PlanoAssinaturaTests
{
    [Fact]
    public void PlanoGratis_DeveSerZero()
    {
        Assert.Equal(0, (int)PlanoAssinatura.Gratis);
    }

    [Fact]
    public void PlanoPro_DeveSerUm()
    {
        Assert.Equal(1, (int)PlanoAssinatura.Pro);
    }
}

// ==========================================
// TESTES DE SERVIÇO — TokenService
// ==========================================

public class TokenServiceTests
{
    private static TokenService CriarTokenService()
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Jwt:Chave"] = "SmartMenu-SaaS-ChaveSecreta-2026-DevSenior-MinLength32chars!",
                ["Jwt:Emissor"] = "SmartMenu.Api",
                ["Jwt:Audiencia"] = "SmartMenu.Web",
                ["Jwt:ExpiracaoHoras"] = "24"
            })
            .Build();

        return new TokenService(config);
    }

    [Fact]
    public void GerarToken_DeveRetornarStringNaoVazia()
    {
        var service = CriarTokenService();
        var token = service.GerarToken(1, "Marcos", "marcos@email.com");

        Assert.False(string.IsNullOrWhiteSpace(token));
    }

    [Fact]
    public void GerarToken_DeveConterClaimsCorretas()
    {
        var service = CriarTokenService();
        var token = service.GerarToken(42, "Marcos Silva", "marcos@test.com");

        var handler = new JwtSecurityTokenHandler();
        var jwt = handler.ReadJwtToken(token);

        Assert.Equal("42", jwt.Claims.First(c => c.Type == ClaimTypes.NameIdentifier).Value);
        Assert.Equal("Marcos Silva", jwt.Claims.First(c => c.Type == ClaimTypes.Name).Value);
        Assert.Equal("marcos@test.com", jwt.Claims.First(c => c.Type == ClaimTypes.Email).Value);
    }

    [Fact]
    public void GerarToken_DeveExpirarEm24Horas()
    {
        var service = CriarTokenService();
        var antes = DateTime.UtcNow;
        var token = service.GerarToken(1, "Test", "test@test.com");

        var handler = new JwtSecurityTokenHandler();
        var jwt = handler.ReadJwtToken(token);

        Assert.True(jwt.ValidTo > antes.AddHours(23));
        Assert.True(jwt.ValidTo < antes.AddHours(25));
    }

    [Fact]
    public void GerarToken_DeveTerEmissorCorreto()
    {
        var service = CriarTokenService();
        var token = service.GerarToken(1, "Test", "test@test.com");

        var handler = new JwtSecurityTokenHandler();
        var jwt = handler.ReadJwtToken(token);

        Assert.Equal("SmartMenu.Api", jwt.Issuer);
        Assert.Contains("SmartMenu.Web", jwt.Audiences);
    }
}

// ==========================================
// TESTES DE SERVIÇO — QrCodeService
// ==========================================

public class QrCodeServiceTests
{
    private static QrCodeService CriarQrCodeService()
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["App:BaseUrlPublica"] = "https://meu-menu.com"
            })
            .Build();

        return new QrCodeService(config);
    }

    [Fact]
    public void GerarQrCode_DeveRetornarBase64Valido()
    {
        var service = CriarQrCodeService();
        var result = service.GerarQrCodeBase64("pizzaria-do-marcos");

        Assert.False(string.IsNullOrWhiteSpace(result));
        var bytes = Convert.FromBase64String(result);
        Assert.True(bytes.Length > 100);
    }

    [Fact]
    public void GerarQrCode_DeveGerarPng()
    {
        var service = CriarQrCodeService();
        var result = service.GerarQrCodeBase64("test-slug");

        var bytes = Convert.FromBase64String(result);
        // PNG header: 137 80 78 71
        Assert.Equal(0x89, bytes[0]);
        Assert.Equal(0x50, bytes[1]);
        Assert.Equal(0x4E, bytes[2]);
        Assert.Equal(0x47, bytes[3]);
    }
}

// ==========================================
// TESTES DE BANCO — DbContext com SQLite InMemory
// ==========================================

public class SmartMenuDbContextTests : IDisposable
{
    private readonly SmartMenuDbContext _db;

    public SmartMenuDbContextTests()
    {
        var options = new DbContextOptionsBuilder<SmartMenuDbContext>()
            .UseSqlite("DataSource=:memory:")
            .Options;

        _db = new SmartMenuDbContext(options);
        _db.Database.OpenConnection();
        _db.Database.EnsureCreated();
    }

    [Fact]
    public void SeedData_DeveCriarUsuarioDemo()
    {
        var usuario = _db.Usuarios.FirstOrDefault(u => u.Email == "demo@smartmenu.com");

        Assert.NotNull(usuario);
        Assert.Equal("Demo Admin", usuario.Nome);
    }

    [Fact]
    public void SeedData_DeveCriarRestauranteComPlanoPro()
    {
        var restaurante = _db.Restaurantes.FirstOrDefault(r => r.Slug == "pizzaria-do-marcos");

        Assert.NotNull(restaurante);
        Assert.Equal(PlanoAssinatura.Pro, restaurante.Plano);
    }

    [Fact]
    public void SeedData_DeveCriar4Categorias()
    {
        var categorias = _db.Categorias.ToList();

        Assert.True(categorias.Count >= 4);
        Assert.Contains(categorias, c => c.Nome == "Pizzas Tradicionais");
        Assert.Contains(categorias, c => c.Nome == "Bebidas");
    }

    [Fact]
    public void SeedData_DeveCriarItensCardapio()
    {
        var itens = _db.ItensCardapio.ToList();

        Assert.True(itens.Count >= 8);
        Assert.Contains(itens, i => i.Nome == "Margherita");
    }

    [Fact]
    public void EmailUsuario_DeveSerUnico()
    {
        _db.Usuarios.Add(new Usuario
        {
            Nome = "Duplicate",
            Email = "demo@smartmenu.com",
            SenhaHash = "hash"
        });

        Assert.Throws<DbUpdateException>(() => _db.SaveChanges());
    }

    [Fact]
    public void SlugRestaurante_DeveSerUnico()
    {
        _db.Restaurantes.Add(new Restaurante
        {
            Nome = "Duplicate",
            Slug = "pizzaria-do-marcos",
            UsuarioId = 1
        });

        Assert.Throws<DbUpdateException>(() => _db.SaveChanges());
    }

    [Fact]
    public async Task CascadeDelete_DeveRemoverCategoriasComRestaurante()
    {
        var restaurante = await _db.Restaurantes
            .Include(r => r.Categorias)
            .FirstAsync(r => r.Slug == "pizzaria-do-marcos");

        var categoriaCount = restaurante.Categorias.Count;
        Assert.True(categoriaCount > 0);

        _db.Restaurantes.Remove(restaurante);
        await _db.SaveChangesAsync();

        Assert.Empty(_db.Categorias.Where(c => c.RestauranteId == restaurante.Id));
    }

    public void Dispose() => _db.Dispose();
}
