# 📸 Screenshots para o SHOWCASE — SmartMenu SaaS

Projetos rodando em:
- **API + Scalar:** http://localhost:5221
- **Web (Blazor WASM):** http://localhost:5123 *(template padrão — UI real está na API)*

---

## ⚠️ Situação real do frontend

O `SmartMenu.Web` contém o projeto Blazor WASM mas as páginas de UI (login, cardápio, QR Code)
ainda não foram implementadas — as evidências visuais relevantes são capturadas via **Scalar** (documentação interativa da API).

---

## Checklist de screenshots (tire em ordem)

| Arquivo       | Onde capturar                                                 | O que mostrar                                                                   |
|---------------|---------------------------------------------------------------|---------------------------------------------------------------------------------|
| `img01.png`   | http://localhost:5221/scalar/v1                               | **Tela inicial do Scalar** — lista de grupos de endpoints                       |
| `img02.png`   | http://localhost:5221/scalar/v1 → Auth → POST /login         | **Endpoint de login** expandido com body de exemplo                             |
| `img03.png`   | http://localhost:5221/scalar/v1 → Restaurantes → Execute     | **Resposta JSON** de GET /restaurantes com os dados do tenant demo              |
| `img04.png`   | http://localhost:5221/api/menu/pizzaria-do-marcos (JSON)      | **Cardápio público** completo — Pizzaria do Marcos — sem autenticação           |
| `img05.png`   | http://localhost:5221/scalar/v1 → Restaurantes → QR Code     | **Endpoint de QR Code** — mostrar a imagem base64 ou endpoint documentado       |
| `img06.png`   | http://localhost:5123                                         | **Blazor WASM rodando** — tela inicial do projeto Web                           |

---

## Login de demonstração (para testar via Scalar)

| Campo  | Valor               |
|--------|---------------------|
| E-mail | `demo@smartmenu.com` |
| Senha  | `demo123`           |

## Cardápio público (sem auth)

```
GET http://localhost:5221/api/menu/pizzaria-do-marcos
```
Retorna: Pizzaria do Marcos — 4 categorias, 10 itens, cor `#E53935`

---

## Como rodar o projeto

```powershell
# Terminal 1 — API
cd src/SmartMenu.Api
dotnet run --urls http://localhost:5221

# Terminal 2 — Web
cd src/SmartMenu.Web
dotnet run --urls http://localhost:5123
```

