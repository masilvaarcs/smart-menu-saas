using QRCoder;

namespace SmartMenu.Api.Services;

/// <summary>
/// Serviço de geração de QR Code para cardápio público.
/// </summary>
public class QrCodeService
{
    private readonly IConfiguration _config;

    public QrCodeService(IConfiguration config)
    {
        _config = config;
    }

    /// <summary>
    /// Gera QR Code como imagem PNG em base64 para o cardápio público.
    /// </summary>
    public string GerarQrCodeBase64(string slug)
    {
        var baseUrl = _config["App:BaseUrlPublica"] ?? "https://localhost:5001";
        var url = $"{baseUrl}/menu/{slug}";

        using var gerador = new QRCodeGenerator();
        using var dados = gerador.CreateQrCode(url, QRCodeGenerator.ECCLevel.M);
        using var qrCode = new PngByteQRCode(dados);
        var bytes = qrCode.GetGraphic(10);

        return Convert.ToBase64String(bytes);
    }
}
