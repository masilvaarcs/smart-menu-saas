using System.ComponentModel.DataAnnotations;

namespace SmartMenu.Shared.Models;

/// <summary>
/// Usuário administrador de um ou mais restaurantes.
/// </summary>
public class Usuario
{
    public int Id { get; set; }

    [Required(ErrorMessage = "Nome é obrigatório.")]
    [StringLength(100)]
    public string Nome { get; set; } = string.Empty;

    [Required(ErrorMessage = "E-mail é obrigatório.")]
    [EmailAddress(ErrorMessage = "E-mail inválido.")]
    [StringLength(200)]
    public string Email { get; set; } = string.Empty;

    /// <summary>Hash da senha (BCrypt).</summary>
    public string SenhaHash { get; set; } = string.Empty;

    public DateTime CriadoEm { get; set; } = DateTime.UtcNow;

    public bool Ativo { get; set; } = true;

    // Navegação
    public List<Restaurante> Restaurantes { get; set; } = [];
}
