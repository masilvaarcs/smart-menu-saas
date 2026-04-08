using System.ComponentModel.DataAnnotations;

namespace SmartMenu.Shared.DTOs;

// ========================
// AUTH DTOs
// ========================

public record RegistroRequest(
    [Required(ErrorMessage = "Nome é obrigatório.")] string Nome,
    [Required(ErrorMessage = "E-mail é obrigatório.")][EmailAddress] string Email,
    [Required(ErrorMessage = "Senha é obrigatória.")][MinLength(6, ErrorMessage = "Senha deve ter no mínimo 6 caracteres.")] string Senha
);

public record LoginRequest(
    [Required] string Email,
    [Required] string Senha
);

public record LoginResponse(string Token, string Nome, string Email, DateTime Expiracao);

// ========================
// RESTAURANTE DTOs
// ========================

public record RestauranteRequest(
    [Required][StringLength(100)] string Nome,
    string? Descricao,
    string? Endereco,
    string? Telefone,
    [Required][StringLength(100)] string Slug,
    string? LogoUrl,
    string? CorPrimaria
);

public record RestauranteResponse(
    int Id, string Nome, string Descricao, string Endereco,
    string Telefone, string Slug, string LogoUrl, string CorPrimaria,
    string Plano, bool Ativo, DateTime CriadoEm, int TotalItens
);

// ========================
// CATEGORIA DTOs
// ========================

public record CategoriaRequest(
    [Required][StringLength(80)] string Nome,
    string? Descricao,
    string? Icone,
    int Ordem
);

public record CategoriaResponse(
    int Id, string Nome, string Descricao, string Icone,
    int Ordem, bool Ativo, int TotalItens
);

// ========================
// ITEM CARDÁPIO DTOs
// ========================

public record ItemCardapioRequest(
    [Required][StringLength(120)] string Nome,
    string? Descricao,
    [Required][Range(0.01, 99999.99)] decimal Preco,
    string? ImagemUrl,
    bool Disponivel,
    bool Destaque,
    string? Tags,
    int Ordem
);

public record ItemCardapioResponse(
    int Id, string Nome, string Descricao, decimal Preco,
    string ImagemUrl, bool Disponivel, bool Destaque,
    string Tags, int Ordem
);

// ========================
// CARDÁPIO PÚBLICO DTO
// ========================

public record CardapioPublicoResponse(
    string NomeRestaurante, string Descricao, string LogoUrl,
    string CorPrimaria, string Telefone, string Endereco,
    List<CategoriaPublicaResponse> Categorias
);

public record CategoriaPublicaResponse(
    string Nome, string Icone, List<ItemPublicoResponse> Itens
);

public record ItemPublicoResponse(
    string Nome, string Descricao, decimal Preco,
    string ImagemUrl, bool Destaque, string Tags
);
