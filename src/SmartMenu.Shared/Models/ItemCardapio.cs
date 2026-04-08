using System.ComponentModel.DataAnnotations;

namespace SmartMenu.Shared.Models;

/// <summary>
/// Item individual do cardápio (prato, bebida, sobremesa, etc.).
/// </summary>
public class ItemCardapio
{
    public int Id { get; set; }

    [Required(ErrorMessage = "Nome do item é obrigatório.")]
    [StringLength(120)]
    public string Nome { get; set; } = string.Empty;

    [StringLength(500)]
    public string Descricao { get; set; } = string.Empty;

    [Required]
    [Range(0.01, 99999.99, ErrorMessage = "Preço deve ser entre R$ 0,01 e R$ 99.999,99.")]
    public decimal Preco { get; set; }

    /// <summary>URL da imagem do prato (opcional).</summary>
    [StringLength(500)]
    public string ImagemUrl { get; set; } = string.Empty;

    /// <summary>Indica se o item está disponível no momento.</summary>
    public bool Disponivel { get; set; } = true;

    /// <summary>Indica destaque (aparece com badge especial).</summary>
    public bool Destaque { get; set; }

    /// <summary>Tags adicionais: vegetariano, vegano, sem glúten, etc.</summary>
    [StringLength(200)]
    public string Tags { get; set; } = string.Empty;

    /// <summary>Ordem de exibição dentro da categoria.</summary>
    public int Ordem { get; set; }

    public DateTime CriadoEm { get; set; } = DateTime.UtcNow;

    // Navegação
    public int CategoriaId { get; set; }
}
