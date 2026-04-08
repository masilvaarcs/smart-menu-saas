using Microsoft.EntityFrameworkCore;
using SmartMenu.Shared.Models;

namespace SmartMenu.Api.Data;

/// <summary>
/// Contexto do banco de dados multi-tenant.
/// Cada restaurante é um tenant com dados isolados.
/// </summary>
public class SmartMenuDbContext : DbContext
{
    public SmartMenuDbContext(DbContextOptions<SmartMenuDbContext> options) : base(options) { }

    public DbSet<Usuario> Usuarios => Set<Usuario>();
    public DbSet<Restaurante> Restaurantes => Set<Restaurante>();
    public DbSet<Categoria> Categorias => Set<Categoria>();
    public DbSet<ItemCardapio> ItensCardapio => Set<ItemCardapio>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Usuario
        modelBuilder.Entity<Usuario>(e =>
        {
            e.HasIndex(u => u.Email).IsUnique();
            e.HasMany(u => u.Restaurantes)
             .WithOne()
             .HasForeignKey(r => r.UsuarioId)
             .OnDelete(DeleteBehavior.Cascade);
        });

        // Restaurante
        modelBuilder.Entity<Restaurante>(e =>
        {
            e.HasIndex(r => r.Slug).IsUnique();
            e.HasMany(r => r.Categorias)
             .WithOne()
             .HasForeignKey(c => c.RestauranteId)
             .OnDelete(DeleteBehavior.Cascade);
        });

        // Categoria       
        modelBuilder.Entity<Categoria>(e =>
        {
            e.HasMany(c => c.Itens)
             .WithOne()
             .HasForeignKey(i => i.CategoriaId)
             .OnDelete(DeleteBehavior.Cascade);
        });

        // ItemCardapio
        modelBuilder.Entity<ItemCardapio>(e =>
        {
            e.Property(i => i.Preco).HasColumnType("decimal(10,2)");
        });

        // Seed de demonstração
        SeedData(modelBuilder);
    }

    private static void SeedData(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Usuario>().HasData(new Usuario
        {
            Id = 1,
            Nome = "Demo Admin",
            Email = "demo@smartmenu.com",
            // Senha: demo123
            SenhaHash = BCrypt.Net.BCrypt.HashPassword("demo123"),
            CriadoEm = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc)
        });

        modelBuilder.Entity<Restaurante>().HasData(new Restaurante
        {
            Id = 1,
            Nome = "Pizzaria do Marcos",
            Descricao = "As melhores pizzas artesanais de Gravataí!",
            Endereco = "Rua Exemplo, 123 - Gravataí/RS",
            Telefone = "(51) 99999-0000",
            Slug = "pizzaria-do-marcos",
            CorPrimaria = "#E53935",
            Plano = Shared.Enums.PlanoAssinatura.Pro,
            UsuarioId = 1,
            CriadoEm = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc)
        });

        modelBuilder.Entity<Categoria>().HasData(
            new Categoria { Id = 1, Nome = "Pizzas Tradicionais", Icone = "🍕", Ordem = 1, RestauranteId = 1 },
            new Categoria { Id = 2, Nome = "Pizzas Especiais", Icone = "⭐", Ordem = 2, RestauranteId = 1 },
            new Categoria { Id = 3, Nome = "Bebidas", Icone = "🥤", Ordem = 3, RestauranteId = 1 },
            new Categoria { Id = 4, Nome = "Sobremesas", Icone = "🍰", Ordem = 4, RestauranteId = 1 }
        );

        modelBuilder.Entity<ItemCardapio>().HasData(
            // Pizzas Tradicionais
            new ItemCardapio { Id = 1, Nome = "Margherita", Descricao = "Molho de tomate, mozzarella, manjericão fresco e azeite", Preco = 42.90m, Disponivel = true, Destaque = true, Tags = "vegetariano", Ordem = 1, CategoriaId = 1, CriadoEm = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
            new ItemCardapio { Id = 2, Nome = "Calabresa", Descricao = "Molho de tomate, calabresa artesanal, cebola e azeitonas", Preco = 44.90m, Disponivel = true, Ordem = 2, CategoriaId = 1, CriadoEm = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
            new ItemCardapio { Id = 3, Nome = "Portuguesa", Descricao = "Molho de tomate, presunto, ovo, cebola, azeitona e ervilha", Preco = 46.90m, Disponivel = true, Ordem = 3, CategoriaId = 1, CriadoEm = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
            new ItemCardapio { Id = 4, Nome = "Quatro Queijos", Descricao = "Mozzarella, provolone, catupiry e gorgonzola", Preco = 49.90m, Disponivel = true, Tags = "vegetariano", Ordem = 4, CategoriaId = 1, CriadoEm = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc) },

            // Pizzas Especiais
            new ItemCardapio { Id = 5, Nome = "Parma com Rúcula", Descricao = "Presunto de Parma, rúcula, tomate cereja e parmesão", Preco = 59.90m, Disponivel = true, Destaque = true, Ordem = 1, CategoriaId = 2, CriadoEm = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
            new ItemCardapio { Id = 6, Nome = "Trufada", Descricao = "Creme de trufa negra, cogumelos frescos e parmesão", Preco = 64.90m, Disponivel = true, Destaque = true, Tags = "vegetariano,premium", Ordem = 2, CategoriaId = 2, CriadoEm = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc) },

            // Bebidas
            new ItemCardapio { Id = 7, Nome = "Refrigerante Lata", Descricao = "Coca-Cola, Guaraná ou Fanta (350ml)", Preco = 7.90m, Disponivel = true, Ordem = 1, CategoriaId = 3, CriadoEm = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
            new ItemCardapio { Id = 8, Nome = "Suco Natural", Descricao = "Laranja, limão ou maracujá (500ml)", Preco = 12.90m, Disponivel = true, Tags = "natural", Ordem = 2, CategoriaId = 3, CriadoEm = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
            new ItemCardapio { Id = 9, Nome = "Chopp Artesanal", Descricao = "Pilsen ou IPA (500ml)", Preco = 18.90m, Disponivel = true, Destaque = true, Ordem = 3, CategoriaId = 3, CriadoEm = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc) },

            // Sobremesas
            new ItemCardapio { Id = 10, Nome = "Petit Gâteau", Descricao = "Bolo quente de chocolate com sorvete de creme", Preco = 24.90m, Disponivel = true, Destaque = true, Ordem = 1, CategoriaId = 4, CriadoEm = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc) }
        );
    }
}
