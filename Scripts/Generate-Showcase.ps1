<#
.SYNOPSIS
    Gera SHOWCASE.html e Portfolio_LinkedIn.html para o SmartMenu SaaS.
    Execute após colocar os screenshots em ../images/ (veja images/README.md).

.EXAMPLE
    .\Generate-Showcase.ps1
#>

$ErrorActionPreference = "Stop"

$scriptRoot   = Split-Path -Parent $MyInvocation.MyCommand.Path
$projectRoot  = Split-Path -Parent $scriptRoot
$mainScript   = "c:\__DEV__\__Projetos_2026\AnaliseRepositoriosGitHub\RepositoriosGitHub\copilot-prompts-private\prompts\showcase\Generate-Showcase.ps1"

if (-not (Test-Path $mainScript)) {
    Write-Error "Script principal nao encontrado: $mainScript"
    exit 1
}

& $mainScript `
    -ProjectPath        $projectRoot `
    -ProjectName        "SmartMenu SaaS" `
    -ProjectDescription "Cardápio digital multi-tenant para restaurantes — .NET 10 Minimal APIs, Blazor WebAssembly e QR Code." `
    -AccentColor        "#4CAF50" `
    -SecondaryColor     "#FF9800" `
    -RepoUrls @(
        @{ Name = "smart-menu-saas"; Url = "https://github.com/masilvaarcs/smart-menu-saas"; Description = "Repositório principal" }
    ) `
    -TechStack @(".NET 10", "Minimal APIs", "Blazor WebAssembly", "Entity Framework Core", "SQLite", "JWT Bearer", "BCrypt", "QRCoder", "xUnit") `
    -KPIs @(
        @{ Value = "29";      Label = "Testes Passando" },
        @{ Value = "SaaS";    Label = "Arquitetura" },
        @{ Value = "Multi";   Label = "Multi-Tenant" },
        @{ Value = "QR";      Label = "QR Code Gerado" }
    ) `
    -Screenshots @(
        @{ File = "img01.png"; Tag = "API Docs";     Title = "Scalar — Visão Geral";         Desc = "Todos os endpoints documentados com Scalar/OpenAPI — Auth, Restaurantes, Cardápio e Menu Público";  ClipStyle = "a" },
        @{ File = "img02.png"; Tag = "Auth";         Title = "Endpoint de Login";             Desc = "POST /api/auth/login — JWT Bearer gerado com claims de userId, nome e e-mail";                       ClipStyle = "b" },
        @{ File = "img03.png"; Tag = "Restaurantes"; Title = "GET /api/restaurantes";         Desc = "Lista de restaurantes do tenant autenticado — isolamento multi-tenant por UsuarioId";                ClipStyle = "c" },
        @{ File = "img04.png"; Tag = "Menu Público"; Title = "Cardápio Público — JSON";       Desc = "/api/menu/pizzaria-do-marcos — 4 categorias, 10 itens, sem autenticação";                             ClipStyle = "d" },
        @{ File = "img05.png"; Tag = "QR Code";      Title = "Endpoint de QR Code";           Desc = "GET /api/restaurantes/{id}/qrcode — base64 PNG gerado pela API via QRCoder";                        ClipStyle = "e" },
        @{ File = "img06.png"; Tag = "Blazor WASM";  Title = "Frontend Blazor WebAssembly";   Desc = "Projeto SmartMenu.Web rodando em http://localhost:5123 — Blazor WebAssembly .NET 10";               ClipStyle = "f" }
    ) `
    -Features @(
        @{
            Image   = "img03.png"
            Title   = "Multi-Tenancy com Isolamento de Dados"
            Desc    = "Cada restaurante pertence a um tenant (usuário). Todas as queries filtram por UsuarioId — sem vazamento de dados entre tenants. Plano Free limita a 1 restaurante e 15 itens; Pro é ilimitado."
            Tags    = @("JWT Bearer", "EF Core", "SQLite", "Isolamento por Tenant")
            Reverse = $false
        },
        @{
            Image   = "img04.png"
            Title   = "Cardápio Público sem Autenticação"
            Desc    = "Endpoint público /api/menu/{slug} retorna o cardápio completo em JSON — pronto para qualquer cliente consumir. A Pizzaria do Marcos tem 4 categorias e 10 itens de demonstração disponíveis sem token."
            Tags    = @("Minimal API", "Slug único", "Multi-tenant", "Sem auth")
            Reverse = $true
        }
    ) `
    -Challenges @(
        @{ Title = "Isolamento de tenant sem banco separado";  Desc = "Multi-tenancy por coluna (single-database) com filtros automáticos via EF Core Global Query Filters — sem risco de cross-tenant leak." },
        @{ Title = "JWT + BCrypt seguros por padrão";          Desc = "Senhas hasheadas com BCrypt (work factor 12). Token JWT com claims de userId e expiração curta — sem segredos expostos em código." },
        @{ Title = "Planos de assinatura na camada de domínio"; Desc = "Validação dos limites (Free: 1 restaurante, 15 itens) feita na API antes de persistir — lógica de tier encapsulada e testável." },
        @{ Title = "Blazor WASM consumindo Minimal APIs";      Desc = "Frontend Blazor WebAssembly com HttpClient configurado via BaseAddress — CORS habilitado seletivamente no backend." }
    ) `
    -ClosingQuote "SaaS não é só tecnologia — é modelo de negócio. Cada linha de código aqui reflete como escalar um produto com dados isolados, planos e acesso público." `
    -UpdateReadme $true
