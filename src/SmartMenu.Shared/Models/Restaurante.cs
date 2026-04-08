using System.ComponentModel.DataAnnotations;

namespace SmartMenu.Shared.Models;

/// <summary>
/// Representa um restaurante (tenant) no sistema SaaS.
/// Cada restaurante é um tenant isolado com seus próprios dados.
/// </summary>
public class Restaurante
{
    public int Id { get; set; }

    [Required(ErrorMessage = "Nome do restaurante é obrigatório.")]
    [StringLength(100, ErrorMessage = "Nome deve ter no máximo 100 caracteres.")]
    public string Nome { get; set; } = string.Empty;

    [StringLength(500)]
    public string Descricao { get; set; } = string.Empty;

    [StringLength(200)]
    public string Endereco { get; set; } = string.Empty;

    [StringLength(20)]
    public string Telefone { get; set; } = string.Empty;

    /// <summary>Slug único para URL pública do cardápio (ex: /menu/pizzaria-do-marcos)</summary>
    [Required]
    [StringLength(100)]
    public string Slug { get; set; } = string.Empty;

    /// <summary>URL da logo do restaurante (opcional).</summary>
    [StringLength(500)]
    public string LogoUrl { get; set; } = string.Empty;

    /// <summary>Cor primária do tema (hex). Ex: #E53935</summary>
    [StringLength(7)]
    public string CorPrimaria { get; set; } = "#E53935";

    public Enums.PlanoAssinatura Plano { get; set; } = Enums.PlanoAssinatura.Gratis;

    public DateTime CriadoEm { get; set; } = DateTime.UtcNow;

    public bool Ativo { get; set; } = true;

    // Navegação
    public int UsuarioId { get; set; }
    public List<Categoria> Categorias { get; set; } = [];
}
