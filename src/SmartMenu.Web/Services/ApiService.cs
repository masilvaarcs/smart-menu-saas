using System.Net.Http.Headers;
using System.Net.Http.Json;
using SmartMenu.Shared.DTOs;

namespace SmartMenu.Web.Services;

public class ApiService
{
    private readonly HttpClient _http;
    private const string BaseUrl = "http://localhost:5221";

    public string? Token { get; private set; }
    public string? NomeUsuario { get; private set; }
    public bool Autenticado => !string.IsNullOrEmpty(Token);

    public ApiService(HttpClient http)
    {
        _http = http;
        _http.BaseAddress = new Uri(BaseUrl);
    }

    // ── Auth ─────────────────────────────────────────

    public async Task<(bool ok, string erro)> LoginAsync(string email, string senha)
    {
        var resp = await _http.PostAsJsonAsync("/api/auth/login", new LoginRequest(email, senha));
        if (!resp.IsSuccessStatusCode)
        {
            var err = await resp.Content.ReadFromJsonAsync<MensagemErro>();
            return (false, err?.Mensagem ?? "Credenciais inválidas.");
        }
        var login = await resp.Content.ReadFromJsonAsync<LoginResponse>();
        Token = login!.Token;
        NomeUsuario = login.Nome;
        _http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", Token);
        return (true, string.Empty);
    }

    public void Logout()
    {
        Token = null;
        NomeUsuario = null;
        _http.DefaultRequestHeaders.Authorization = null;
    }

    // ── Restaurantes ─────────────────────────────────

    public Task<List<RestauranteResponse>?> GetRestaurantesAsync()
        => _http.GetFromJsonAsync<List<RestauranteResponse>>("/api/restaurantes");

    public Task<RestauranteResponse?> GetRestauranteAsync(int id)
        => _http.GetFromJsonAsync<RestauranteResponse>($"/api/restaurantes/{id}");

    public async Task<RestauranteResponse?> CriarRestauranteAsync(RestauranteRequest req)
    {
        var resp = await _http.PostAsJsonAsync("/api/restaurantes", req);
        resp.EnsureSuccessStatusCode();
        return await resp.Content.ReadFromJsonAsync<RestauranteResponse>();
    }

    public async Task<byte[]> GetQrCodeAsync(int restauranteId)
    {
        var resp = await _http.GetAsync($"/api/restaurantes/{restauranteId}/qrcode");
        resp.EnsureSuccessStatusCode();
        return await resp.Content.ReadAsByteArrayAsync();
    }

    // ── Categorias ────────────────────────────────────

    public Task<List<CategoriaResponse>?> GetCategoriasAsync(int restauranteId)
        => _http.GetFromJsonAsync<List<CategoriaResponse>>($"/api/restaurantes/{restauranteId}/categorias");

    // ── Itens ─────────────────────────────────────────

    public Task<List<ItemCardapioResponse>?> GetItensAsync(int restauranteId, int categoriaId)
        => _http.GetFromJsonAsync<List<ItemCardapioResponse>>($"/api/restaurantes/{restauranteId}/categorias/{categoriaId}/itens");

    // ── Menu Público ──────────────────────────────────

    public Task<CardapioPublicoResponse?> GetMenuPublicoAsync(string slug)
        => _http.GetFromJsonAsync<CardapioPublicoResponse>($"/api/menu/{slug}");

    private record MensagemErro(string Mensagem);
}
