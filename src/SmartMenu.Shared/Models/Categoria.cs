using System.ComponentModel.DataAnnotations;

namespace SmartMenu.Shared.Models;

/// <summary>
/// Categoria de itens do cardápio (ex: Entradas, Pratos Principais, Bebidas).
/// </summary>
public class Categoria
{
    public int Id { get; set; }

    [Required(ErrorMessage = "Nome da categoria é obrigatório.")]
    [StringLength(80)]
    public string Nome { get; set; } = string.Empty;

    [StringLength(200)]
    public string Descricao { get; set; } = string.Empty;

    /// <summary>Ícone (emoji ou FontAwesome). Ex: 🍕</summary>
    [StringLength(10)]
    public string Icone { get; set; } = "🍽️";

    /// <summary>Ordem de exibição no cardápio.</summary>
    public int Ordem { get; set; }

    public bool Ativo { get; set; } = true;

    // Navegação
    public int RestauranteId { get; set; }
    public List<ItemCardapio> Itens { get; set; } = [];
}
