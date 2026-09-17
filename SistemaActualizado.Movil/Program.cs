using Microsoft.EntityFrameworkCore;
using SISTEMAACTUALIZADO.Data;
using SISTEMAACTUALIZADO.Models;
using SISTEMAACTUALIZADO.Services;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddEndpointsApiExplorer();

var app = builder.Build();

// 0. VISTA PRINCIPAL
app.MapGet("/", () => Results.Content(PaginaHtml, "text/html; charset=utf-8"));

// 1. LOGIN OFICIAL DIAGNÓSTICO
app.MapPost("/api/login", async (LoginCredencialesRequest req) =>
{
    try
    {
        using var db = new AppDbContext();
        string u = (req.Usuario ?? "").Trim().ToLower();
        string p = (req.Clave ?? "").Trim();

        // Si tu proyecto tiene UsuarioService con login, también se puede usar directamente:
        // var uService = new UsuarioService();
        // var valido = uService.Autenticar(u, p);

        // Buscar primero al usuario por nombre de usuario o nombre completo
        var usuario = await db.Usuarios
            .AsNoTracking()
            .FirstOrDefaultAsync(x => 
                (x.NombreUsuario != null && x.NombreUsuario.ToLower() == u) ||
                (x.NombreCompleto != null && x.NombreCompleto.ToLower().Contains(u)) ||
                (x.UsuarioID.ToString() == u)
            );

        if (usuario == null)
        {
            return Results.Json(new { ok = false, mensaje = $"Usuario '{u}' no encontrado en la base de datos." });
        }

        // Función auxiliar para calcular SHA256 si la clave está hasheada
        static string CalcularSha256(string texto)
        {
            using var sha = System.Security.Cryptography.SHA256.Create();
            byte[] bytes = sha.ComputeHash(System.Text.Encoding.UTF8.GetBytes(texto));
            return Convert.ToHexString(bytes).ToLower();
        }

        string pSha256 = CalcularSha256(p);

        // Comprobación: Texto plano O Hash SHA256 (mayúsculas o minúsculas)
        bool claveValida = 
            usuario.Clave == p || 
            usuario.Clave.Equals(pSha256, StringComparison.OrdinalIgnoreCase);

        if (!claveValida)
        {
            return Results.Json(new { ok = false, mensaje = "Contraseña incorrecta." });
        }

        return Results.Json(new { 
            ok = true, 
            vendedor = string.IsNullOrWhiteSpace(usuario.NombreCompleto) ? usuario.NombreUsuario : usuario.NombreCompleto, 
            usuarioId = usuario.UsuarioID 
        });
    }
    catch (Exception ex)
    {
        return Results.Json(new { ok = false, mensaje = "Error en servidor: " + ex.Message });
    }
});
// 2. BUSCADOR DE PRODUCTOS Y CATEGORÍAS
app.MapGet("/api/productos", (string? q, string? cat) =>
{
    var prodService = new ProductoService();
    var todos = prodService.ObtenerProductosActivos();

    if (!string.IsNullOrWhiteSpace(cat) && cat != "Todas")
    {
        todos = todos.Where(p => (p.Categoria != null && p.Categoria.Equals(cat, StringComparison.OrdinalIgnoreCase))).ToList();
    }

    if (!string.IsNullOrWhiteSpace(q))
    {
        string filtro = q.Trim().ToLower();
        todos = todos.Where(p => 
            (!string.IsNullOrEmpty(p.Nombre) && p.Nombre.ToLower().Contains(filtro)) ||
            (!string.IsNullOrEmpty(p.CodigoBarra) && p.CodigoBarra.Contains(filtro))
        ).ToList();
    }

    var respuesta = todos.Take(60).Select(p => new
    {
        p.ProductoID,
        p.Nombre,
        CodigoBarra = string.IsNullOrEmpty(p.CodigoBarra) ? "00000" : p.CodigoBarra,
        Categoria = string.IsNullOrEmpty(p.Categoria) ? "General" : p.Categoria,
        Precio = prodService.ObtenerPrecioProductoConCliente(p, 1, 1, 0),
        Stock = p.Stock
    });

    return Results.Json(respuesta);
});

// 3. RUTA RELATIVA DE IMÁGENES PORTABLE
string carpetaImagenes = Path.Combine(AppContext.BaseDirectory, "Imagenes");
if (!Directory.Exists(carpetaImagenes))
{
    carpetaImagenes = Path.Combine(Directory.GetCurrentDirectory(), "Imagenes");
}

static string Normalizar(string texto)
{
    if (string.IsNullOrWhiteSpace(texto)) return "";
    return new string(texto
        .Normalize(System.Text.NormalizationForm.FormD)
        .Where(c => System.Globalization.CharUnicodeInfo.GetUnicodeCategory(c) != System.Globalization.UnicodeCategory.NonSpacingMark)
        .ToArray())
        .ToLower()
        .Trim();
}

app.MapGet("/api/productos/{id:int}/imagen", async (int id) =>
{
    using var db = new AppDbContext();
    var prod = await db.Productos.AsNoTracking().FirstOrDefaultAsync(p => p.ProductoID == id);

    if (prod != null && Directory.Exists(carpetaImagenes))
    {
        string nombreLimpio = Normalizar(prod.Nombre);
        var palabras = nombreLimpio.Split(new[] { ' ', '-', '_' }, StringSplitOptions.RemoveEmptyEntries);
        var archivos = Directory.GetFiles(carpetaImagenes);

        var coincidencia = archivos.FirstOrDefault(f =>
        {
            string bName = Path.GetFileNameWithoutExtension(f).ToLower();
            return bName == prod.ProductoID.ToString() ||
                   (!string.IsNullOrEmpty(prod.CodigoBarra) && bName == prod.CodigoBarra.ToLower());
        });

        if (coincidencia == null && palabras.Length > 0)
        {
            coincidencia = archivos.FirstOrDefault(f =>
            {
                string archLimpio = Normalizar(Path.GetFileNameWithoutExtension(f));
                return archLimpio.Length > 2 && palabras.Any(p => p.Length > 2 && (archLimpio.Contains(p) || p.Contains(archLimpio)));
            });
        }

        if (coincidencia != null && File.Exists(coincidencia))
        {
            string ext = Path.GetExtension(coincidencia).ToLower();
            string mime = ext switch
            {
                ".png" => "image/png",
                ".webp" => "image/webp",
                ".jfif" => "image/jpeg",
                _ => "image/jpeg"
            };
            return Results.File(coincidencia, mime);
        }
    }

    string svgDefault = """
    <svg xmlns="http://www.w3.org/2000/svg" width="60" height="60" viewBox="0 0 24 24" fill="none" stroke="#1e293b" stroke-width="1.2">
        <path d="M21 16V8a2 2 0 0 0-1-1.73l-7-4a2 2 0 0 0-2 0l-7 4A2 2 0 0 0 3 8v8a2 2 0 0 0 1 1.73l7 4a2 2 0 0 0 2 0l7-4A2 2 0 0 0 21 16z"></path>
        <polyline points="3.27 6.96 12 12.01 20.73 6.96"></polyline>
        <line x1="12" y1="22.08" x2="12" y2="12"></line>
    </svg>
    """;
    return Results.Content(svgDefault, "image/svg+xml");
});

// 4. GENERACIÓN DE TICKET
app.MapPost("/api/enviar-caja", (VentaMovilRequest req) =>
{
    if (req.Items == null || req.Items.Count == 0)
        return Results.Json(new { ok = false, mensaje = "El carrito está vacío" });

    try
    {
        var ventaService = new VentaService();
        var carritoServicio = req.Items.Select(i => new DetalleCarrito
        {
            ProductoID = i.ProductoId,
            Nombre = i.Nombre,
            PrecioUnitario = i.Precio,
            Cantidad = i.Cantidad
        }).ToList();

        int nroTicket = ventaService.GenerarTicketVenta(
            carritoServicio,
            req.Vendedor ?? "Vendedor Móvil",
            "Consumidor Final",
            ""
        );

        return Results.Json(new { ok = true, nroTicket });
    }
    catch (Exception ex)
    {
        return Results.Json(new { ok = false, mensaje = ex.Message });
    }
});

app.Run("http://0.0.0.0:5050");

public record LoginCredencialesRequest(string Usuario, string Clave);
public record VentaMovilRequest(string Vendedor, List<ItemCarritoMovil> Items);
public record ItemCarritoMovil(int ProductoId, string Nombre, decimal Precio, int Cantidad);

public partial class Program
{
    public const string PaginaHtml = """
    <!DOCTYPE html>
    <html lang="es">
    <head>
        <meta charset="UTF-8">
        <meta name="viewport" content="width=device-width, initial-scale=1.0, maximum-scale=1.0, user-scalable=no">
        <title>POS SYSTEM - Punto de Venta DTE</title>
        <style>
            :root {
                --sidebar-bg: #111827;
                --sidebar-active: #0284c7;
                --bg: #f8fafc;
                --card-bg: #ffffff;
                --border: #e2e8f0;
                --text: #0f172a;
                --muted: #64748b;
                --price-blue: #0284c7;
                --stock-green: #059669;
                --brand-blue: #0066ff;
            }
            * { box-sizing: border-box; margin: 0; padding: 0; font-family: -apple-system, BlinkMacSystemFont, "Segoe UI", Roboto, sans-serif; -webkit-tap-highlight-color: transparent; }
            body { background: var(--bg); color: var(--text); min-height: 100vh; }

            .pantalla { display: none; }
            .activa { display: block; }

            /* --- LOGIN IDÉNTICO AL ESCRITORIO --- */
            .login-viewport {
                min-height: 100vh;
                background: #0f172a;
                display: flex;
                align-items: center;
                justify-content: center;
                padding: 16px;
            }
            .login-card {
                width: 100%;
                max-width: 860px;
                background: #fff;
                border-radius: 18px;
                overflow: hidden;
                display: flex;
                box-shadow: 0 25px 50px -12px rgba(0,0,0,0.5);
                position: relative;
            }
            .login-left {
                flex: 1;
                background: linear-gradient(145deg, #091124 0%, #0d1e3d 100%);
                color: #fff;
                padding: 48px 36px;
                display: flex;
                flex-direction: column;
                align-items: center;
                justify-content: center;
                text-align: center;
            }
            .cube-icon {
                width: 130px;
                height: 130px;
                margin-bottom: 24px;
                filter: drop-shadow(0 10px 15px rgba(0,102,255,0.4));
            }
            .brand-logo-text {
                font-size: 26px;
                font-weight: 900;
                letter-spacing: 0.5px;
                display: flex;
                align-items: center;
            }
            .brand-logo-text span:first-child { color: #fff; }
            .brand-logo-text span:last-child { color: #38bdf8; }
            .brand-sub {
                color: #94a3b8;
                font-size: 13px;
                margin-top: 8px;
            }

            .login-right {
                flex: 1.1;
                padding: 48px 42px;
                display: flex;
                flex-direction: column;
                justify-content: center;
                position: relative;
            }
            .close-x {
                position: absolute;
                top: 20px;
                right: 20px;
                color: #94a3b8;
                font-size: 20px;
                cursor: pointer;
            }
            .login-title {
                font-size: 26px;
                font-weight: 800;
                color: #0f172a;
            }
            .login-subtitle {
                font-size: 13px;
                color: var(--muted);
                margin: 4px 0 24px;
            }
            .form-group {
                margin-bottom: 18px;
            }
            .form-label {
                display: block;
                font-size: 11px;
                font-weight: 800;
                letter-spacing: 0.5px;
                color: #334155;
                margin-bottom: 6px;
                text-transform: uppercase;
            }
            .input-wrapper {
                position: relative;
                display: flex;
                align-items: center;
            }
            .input-icon {
                position: absolute;
                left: 14px;
                color: #94a3b8;
                font-size: 14px;
            }
            .form-input {
                width: 100%;
                padding: 12px 14px 12px 38px;
                border: 1.5px solid #e2e8f0;
                border-radius: 8px;
                font-size: 14px;
                color: #0f172a;
                background: #f8fafc;
                outline: none;
                transition: border-color 0.2s;
            }
            .form-input:focus {
                border-color: #0066ff;
                background: #fff;
            }
            .login-options {
                display: flex;
                justify-content: space-between;
                align-items: center;
                font-size: 12px;
                margin-bottom: 22px;
            }
            .checkbox-label {
                display: flex;
                align-items: center;
                gap: 6px;
                color: #334155;
                cursor: pointer;
            }
            .forgot-link {
                color: #0066ff;
                text-decoration: none;
            }
            .btn-login-submit {
                width: 100%;
                background: #0066ff;
                color: #fff;
                border: none;
                padding: 14px;
                border-radius: 8px;
                font-size: 14px;
                font-weight: 800;
                letter-spacing: 0.5px;
                cursor: pointer;
                display: flex;
                align-items: center;
                justify-content: center;
                gap: 6px;
            }
            .btn-login-submit:active { background: #0052cc; }
            .ssl-notice {
                display: flex;
                align-items: center;
                justify-content: center;
                gap: 6px;
                font-size: 11px;
                color: #94a3b8;
                margin-top: 20px;
            }

            /* --- TOPBAR & PUNTO DE VENTA --- */
            .top-header { background: #fff; border-bottom: 1px solid var(--border); padding: 14px 22px; display: flex; justify-content: space-between; align-items: center; }
            .top-header h1 { font-size: 20px; font-weight: 900; color: #0f172a; }
            .session-badge { color: #0284c7; font-size: 13px; font-weight: 800; display: flex; align-items: center; gap: 6px; }

            .app-layout { display: flex; width: 100%; min-height: calc(100vh - 58px); }

            .sidebar-nav { width: 220px; background: var(--sidebar-bg); color: #fff; padding: 18px 10px; display: flex; flex-direction: column; gap: 4px; flex-shrink: 0; }
            .sidebar-brand { font-size: 16px; font-weight: 900; letter-spacing: 0.5px; padding: 6px 12px 18px; display: flex; align-items: center; gap: 8px; border-bottom: 1px solid #1f2937; margin-bottom: 12px; }
            .nav-link { padding: 10px 14px; border-radius: 8px; font-size: 13px; font-weight: 700; color: #94a3b8; display: flex; align-items: center; gap: 10px; cursor: pointer; border: none; background: none; width: 100%; text-align: left; }
            .nav-link.active { background: var(--sidebar-active); color: #fff; }

            .pos-center { flex: 1; padding: 20px; overflow-y: auto; }

            .search-row { display: flex; gap: 12px; margin-bottom: 14px; align-items: center; }
            .search-input-box { position: relative; flex: 1; }
            .search-input { width: 100%; padding: 10px 40px 10px 14px; border: 1px solid var(--border); border-radius: 8px; font-size: 14px; outline: none; background: #fff; }
            .search-badge { position: absolute; right: 10px; top: 9px; font-size: 11px; background: #f1f5f9; padding: 2px 6px; border-radius: 4px; color: var(--muted); font-weight: 800; }
            .btn-head-action { padding: 9px 14px; border-radius: 8px; border: 1px solid #cbd5e1; background: #fff; font-size: 12px; font-weight: 700; cursor: pointer; display: flex; align-items: center; gap: 6px; }

            .section-label { font-size: 11px; font-weight: 800; color: var(--muted); text-transform: uppercase; margin-bottom: 8px; }
            .cat-bar { display: flex; gap: 8px; overflow-x: auto; padding-bottom: 14px; scrollbar-width: none; }
            .cat-bar::-webkit-scrollbar { display: none; }
            .cat-btn { padding: 7px 16px; border-radius: 6px; font-size: 12px; font-weight: 800; border: 1px solid var(--border); background: #fff; cursor: pointer; display: flex; align-items: center; gap: 6px; white-space: nowrap; }
            .cat-btn.todas { background: #0284c7; color: #fff; border-color: #0284c7; }
            .cat-btn.abarrotes { border-color: #bae6fd; color: #0284c7; }
            .cat-btn.fiambres { border-color: #fecdd3; color: #e11d48; }
            .cat-btn.general { border-color: #fed7aa; color: #ea580c; }
            .cat-btn.lacteos { border-color: #bbf7d0; color: #16a34a; }
            .cat-btn.otros { border-color: #e9d5ff; color: #9333ea; }
            .cat-btn.active:not(.todas) { background: #e0f2fe; }

            .prod-container { display: grid; grid-template-columns: repeat(auto-fill, minmax(170px, 1fr)); gap: 14px; margin-top: 10px; }
            .p-card { background: #fff; border: 1px solid var(--border); border-radius: 10px; padding: 14px; display: flex; flex-direction: column; align-items: center; text-align: center; cursor: pointer; transition: transform 0.1s; }
            .p-card:active { transform: scale(0.97); }
            .p-img-box { width: 100%; height: 95px; display: flex; align-items: center; justify-content: center; margin-bottom: 8px; }
            .p-img-box img { max-width: 100%; max-height: 95px; object-fit: contain; }
            .p-name { font-size: 13px; font-weight: 800; color: #0f172a; height: 32px; overflow: hidden; display: -webkit-box; -webkit-line-clamp: 2; -webkit-box-orient: vertical; line-height: 1.25; margin-bottom: 6px; }
            .p-price { font-size: 15px; font-weight: 900; color: var(--price-blue); margin-bottom: 2px; }
            .p-code { font-size: 11px; color: var(--muted); }
            .p-stock { font-size: 11px; font-weight: 700; color: var(--stock-green); margin-top: 2px; }

            .pos-sidebar { width: 340px; background: #fff; border-left: 1px solid var(--border); padding: 18px; display: flex; flex-direction: column; flex-shrink: 0; }
            .cart-top { display: flex; justify-content: space-between; align-items: center; margin-bottom: 14px; }
            .cart-top h3 { font-size: 14px; font-weight: 900; }
            .btn-clear { background: #fee2e2; color: #ef4444; border: 1px solid #fca5a5; font-size: 11px; font-weight: 800; padding: 4px 10px; border-radius: 6px; cursor: pointer; }
            .cart-scroll { flex: 1; overflow-y: auto; display: flex; flex-direction: column; gap: 8px; }
            .empty-state { text-align: center; margin: auto 0; padding: 30px 10px; color: var(--muted); }
            .empty-state svg { width: 50px; height: 50px; stroke: #cbd5e1; margin-bottom: 10px; }

            .cart-line { display: flex; justify-content: space-between; align-items: center; padding: 8px; background: #f8fafc; border-radius: 8px; border: 1px solid var(--border); }
            .c-line-left { flex: 1; }
            .c-line-name { font-size: 12px; font-weight: 800; }
            .c-line-price { font-size: 12px; font-weight: 800; color: var(--price-blue); }
            .c-line-ctrls { display: flex; align-items: center; gap: 6px; }
            .c-btn-step { width: 24px; height: 24px; border: 1px solid var(--border); background: #fff; border-radius: 4px; font-weight: bold; cursor: pointer; }

            .cart-foot { border-top: 1px solid var(--border); padding-top: 14px; margin-top: 10px; }
            .tot-row { display: flex; justify-content: space-between; font-size: 12px; font-weight: 700; color: #475569; margin-bottom: 4px; }
            .tot-main { display: flex; justify-content: space-between; font-size: 20px; font-weight: 900; color: #0f172a; margin: 10px 0 14px; align-items: center; }
            .tot-main span:last-child { color: var(--price-blue); }
            .btn-f4-ticket { width: 100%; background: #10b981; color: #fff; border: none; padding: 14px; font-size: 14px; font-weight: 900; border-radius: 8px; cursor: pointer; display: flex; align-items: center; justify-content: center; gap: 8px; }

            .mobile-nav { display: none !important; fixed; bottom: 0; left: 0; width: 100%; height: 55px; background: #fff; border-top: 1px solid var(--border); justify-content: space-around; align-items: center; z-index: 100; }
            .m-item { background: none; border: none; font-size: 11px; font-weight: 800; color: var(--muted); display: flex; flex-direction: column; align-items: center; gap: 2px; }
            .m-item.active { color: var(--price-blue); }

            @media (max-width: 900px) {
                .login-card { flex-direction: column; }
                .login-left { padding: 30px 20px; }
                .cube-icon { width: 90px; height: 90px; margin-bottom: 12px; }
                .login-right { padding: 30px 22px; }
                .sidebar-nav { display: none; }
                .pos-sidebar { display: none; position: fixed; top: 58px; left: 0; width: 100%; height: calc(100vh - 113px); z-index: 90; }
                .pos-sidebar.mobile-open { display: flex; }
                .mobile-nav { display: flex; }
                /* Quitar display: flex aquí para que no salga en el login */
                .prod-container { grid-template-columns: repeat(2, 1fr); gap: 10px; }
                .pos-center { padding: 14px; padding-bottom: 70px; }
            }

            .modal-bg { position: fixed; inset: 0; background: rgba(0,0,0,0.5); display: none; align-items: center; justify-content: center; padding: 20px; z-index: 200; }
            .modal-bg.show { display: flex; }
            .modal-box { background: #fff; border-radius: 16px; padding: 26px; text-align: center; width: 100%; max-width: 320px; }
            .ticket-num { font-size: 34px; font-weight: 900; color: #0284c7; margin: 14px 0; }
        </style>
    </head>
    <body>

        <!-- PANTALLA 1: LOGIN (IDÉNTICO A WINFORMS) -->
        <div id="pantallaLogin" class="pantalla activa login-viewport">
            <div class="login-card">
                <!-- PANEL IZQUIERDO: CUBO ISOMÉTRICO 3D + POSSYSTEM -->
                <div class="login-left">
                    <svg class="cube-icon" viewBox="0 0 100 100">
                        <!-- Cara Superior (Celeste Claro) -->
                        <polygon points="50,15 85,32 50,49 15,32" fill="#5ea2ef" />
                        <!-- Cara Izquierda (Azul Royal) -->
                        <polygon points="15,32 50,49 50,85 15,68" fill="#2563eb" />
                        <!-- Cara Derecha (Azul Marino Oscuro) -->
                        <polygon points="50,49 85,32 85,68 50,85" fill="#1d4ed8" />
                    </svg>
                    <div class="brand-logo-text">
                        <span>POS</span><span>SYSTEM</span>
                    </div>
                    <div class="brand-sub">Gestión inteligente de ventas e inventario</div>
                </div>

                <!-- PANEL DERECHO: FORMULARIO -->
                <div class="login-right">
                    <span class="close-x">✕</span>
                    <h2 class="login-title">Bienvenido de nuevo</h2>
                    <p class="login-subtitle">Ingresa tus credenciales para acceder al panel</p>

                    <form onsubmit="ejecutarLogin(event)">
                        <div class="form-group">
                            <label class="form-label">👤 USUARIO</label>
                            <div class="input-wrapper">
                                <span class="input-icon">👤</span>
                                <input type="text" id="txtUsuario" class="form-input" value="barbara" required placeholder="Ingresa tu usuario">
                            </div>
                        </div>

                        <div class="form-group">
                            <label class="form-label">🔒 CONTRASEÑA</label>
                            <div class="input-wrapper">
                                <span class="input-icon">🔒</span>
                                <input type="password" id="txtClave" class="form-input" required placeholder="••••••••">
                            </div>
                        </div>

                        <div class="login-options">
                            <label class="checkbox-label">
                                <input type="checkbox" checked> Recordarme
                            </label>
                            <a href="#" class="forgot-link">¿Olvidaste tu contraseña?</a>
                        </div>

                        <button type="submit" class="btn-login-submit">
                            INICIAR SESIÓN &nbsp;➔
                        </button>

                        <div class="ssl-notice">
                            <span>🔒</span> Acceso seguro y protegido SSL
                        </div>
                    </form>
                </div>
            </div>
        </div>

        <!-- PANTALLA 2: PUNTO DE VENTA DTE -->
        <div id="pantallaPos" class="pantalla">
            <header class="top-header">
                <h1>Punto de Venta DTE</h1>
                <div class="session-badge">
                    <span>👤 Sesión Activa:</span>
                    <span id="lblVendedor" style="color: #0f172a;">Bárbara (Administrador)</span>
                </div>
            </header>

            <div class="app-layout">
                <aside class="sidebar-nav">
                    <div class="sidebar-brand">⚡ POS SYSTEM</div>
                    <button class="nav-link active">🛒 Punto de Venta</button>
                    <button class="nav-link">🗄️ Control de Caja</button>
                    <button class="nav-link">📦 Productos</button>
                    <button class="nav-link" onclick="cerrarSesion()">🚪 Cerrar Sesión</button>
                </aside>

                <main class="pos-center">
                    <div class="search-row">
                        <div class="search-input-box">
                            <input type="text" id="txtBuscar" class="search-input" placeholder="Buscar producto..." oninput="filtrar()">
                            <span class="search-badge">F2</span>
                        </div>
                        <button class="btn-head-action">🎫 Tickets</button>
                    </div>

                    <div class="section-label">CATEGORÍAS</div>
                    <div class="cat-bar">
                        <button class="cat-btn todas active" onclick="seleccionarCat('Todas', this)">■ Todas</button>
                        <button class="cat-btn abarrotes" onclick="seleccionarCat('Abarrotes', this)">🗎 Abarrotes</button>
                        <button class="cat-btn fiambres" onclick="seleccionarCat('Fiambres', this)">🥩 Fiambres</button>
                        <button class="cat-btn general" onclick="seleccionarCat('General', this)">📦 General</button>
                        <button class="cat-btn lacteos" onclick="seleccionarCat('Lácteos', this)">🥛 Lácteos</button>
                        <button class="cat-btn otros" onclick="seleccionarCat('Otros', this)">🪄 Otros</button>
                    </div>

                    <div class="section-label" style="margin-top: 14px;">PRODUCTOS</div>
                    <div id="gridProductos" class="prod-container"></div>
                </main>

                <aside id="sidebarVenta" class="pos-sidebar">
                    <div class="cart-top">
                        <h3>VENTA ACTUAL (<span id="cantVenta">0</span>)</h3>
                        <button class="btn-clear" onclick="limpiarCarro()">🗑️ Limpiar</button>
                    </div>

                    <div id="listaCarro" class="cart-scroll">
                        <div class="empty-state">
                            <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.5">
                                <circle cx="9" cy="21" r="1"></circle>
                                <circle cx="20" cy="21" r="1"></circle>
                                <path d="M1 1h4l2.68 13.39a2 2 0 0 0 2 1.61h9.72a2 2 0 0 0 2-1.61L23 6H6"></path>
                            </svg>
                            <p>Aún no hay productos en el carrito</p>
                            <span>Seleccione productos para comenzar la venta</span>
                        </div>
                    </div>

                    <div class="cart-foot">
                        <div class="tot-row">
                            <span>Subtotal</span>
                            <span id="lblSubtotal">$ 0</span>
                        </div>
                        <div class="tot-row">
                            <span>Descuento</span>
                            <span>$ 0</span>
                        </div>
                        <div class="tot-main">
                            <span>TOTAL</span>
                            <span id="lblTotal">$ 0</span>
                        </div>
                        <button class="btn-f4-ticket" onclick="enviarACaja()">🧾 F4 GENERAR TICKET</button>
                    </div>
                </aside>
            </div>
        </div>

        <!-- MODAL TICKET GENERADO -->
        <div id="modalTicket" class="modal-bg">
            <div class="modal-box">
                <h3 style="font-size: 18px; font-weight: 900;">¡Ticket Generado!</h3>
                <p style="color: var(--muted); font-size: 13px; margin-top: 4px;">Pase a Caja con este número:</p>
                <div id="modalNumTicket" class="ticket-num">#000000</div>
                <button class="btn-f4-ticket" onclick="cerrarModal()">Aceptar</button>
            </div>
        </div>

        <!-- NAVEGACIÓN MÓVIL -->
        <nav id="mBottomNav" class="mobile-nav">
            <button id="mBtnCat" class="m-item active" onclick="mostrarVistaMovil('catalogo')">
                <span style="font-size:16px;">📦</span>Catálogo
            </button>
            <button id="mBtnCart" class="m-item" onclick="mostrarVistaMovil('carrito')">
                <span style="font-size:16px;">🛒</span>Venta (<span id="cantVentaM">0</span>)
            </button>
            <button class="m-item" onclick="cerrarSesion()">
                <span style="font-size:16px;">🔒</span>Salir
            </button>
        </nav>

        <script>
            let vendedorActivo = "";
            let categoriaActiva = "Todas";
            let carrito = [];

            async function cargarProductos() {
                try {
                    const txt = document.getElementById('txtBuscar');
                    const q = txt ? txt.value : '';
                    const res = await fetch(`/api/productos?q=${encodeURIComponent(q)}&cat=${encodeURIComponent(categoriaActiva)}`);
                    const prods = await res.json();
                    const grid = document.getElementById('gridProductos');
                    if (!grid) return;
                    grid.innerHTML = "";

                    prods.forEach(p => {
                        const card = document.createElement('div');
                        card.className = 'p-card';
                        card.onclick = () => agregarCarro(p.productoID, p.nombre, p.precio);
                        card.innerHTML = `
                            <div class="p-img-box">
                                <img src="/api/productos/${p.productoID}/imagen" loading="lazy" alt="${p.nombre}">
                            </div>
                            <div class="p-name">${p.nombre}</div>
                            <div class="p-price">$ ${Number(p.precio).toLocaleString('es-CL')}</div>
                            <div class="p-code">Cód. ${p.codigoBarra}</div>
                            <div class="p-stock">Stock: ${p.stock} un.</div>
                        `;
                        grid.appendChild(card);
                    });
                } catch(err) {
                    console.error("Error al cargar productos:", err);
                }
            }

            const buscarProductos = cargarProductos;
            const filtrar = cargarProductos;

            async function ejecutarLogin(e) {
                if (e) e.preventDefault();
                const u = document.getElementById('txtUsuario').value.trim();
                const c = document.getElementById('txtClave').value.trim();

                if (!u || !c) {
                    alert("Por favor ingresa usuario y contraseña");
                    return;
                }

                const btn = document.querySelector('.btn-login-submit');
                if (btn) {
                    btn.innerText = "VERIFICANDO...";
                    btn.disabled = true;
                }

                try {
                    const res = await fetch('/api/login', {
                        method: 'POST',
                        headers: {'Content-Type': 'application/json'},
                        body: JSON.stringify({ usuario: u, clave: c })
                    });
                    
                    const data = await res.json();
                    
                    if (data.ok) {
                        vendedorActivo = data.vendedor;
                        const lbl = document.getElementById('lblVendedor');
                        if (lbl) lbl.innerText = vendedorActivo;

                        // Cambio forzado de pantallas sin depender de clases CSS
                        const pLogin = document.getElementById('pantallaLogin');
                        const pPos = document.getElementById('pantallaPos');

                        if (pLogin) pLogin.style.display = 'none';
                        if (pPos) {
                            pPos.style.display = 'block';
                            pPos.classList.add('activa');
                        }

                        // Mostrar el menú inferior solo en pantallas móviles tras iniciar sesión
                        if (mNav && window.innerWidth <= 900) {
                            mNav.style.setProperty('display', 'flex', 'important');
                        }
                        await cargarProductos();
                    } else {
                        alert("Aviso: " + (data.mensaje || "Credenciales incorrectas"));
                    }
                } catch(err) {
                    alert("Error en el proceso de Login: " + err.message);
                } finally {
                    if (btn) {
                        btn.innerText = "INICIAR SESIÓN ➔";
                        btn.disabled = false;
                    }
                }
            }

            function seleccionarCat(cat, btn) {
                categoriaActiva = cat;
                document.querySelectorAll('.cat-btn').forEach(b => b.classList.remove('active'));
                if (btn) btn.classList.add('active');
                cargarProductos();
            }

            function agregarCarro(id, nombre, precio) {
                const item = carrito.find(c => c.productoId === id);
                if (item) item.cantidad++;
                else carrito.push({ productoId: id, nombre, precio, cantidad: 1 });
                renderCarrito();
            }

            function modificarCant(id, delta) {
                const item = carrito.find(c => c.productoId === id);
                if (!item) return;
                item.cantidad += delta;
                if (item.cantidad <= 0) carrito = carrito.filter(c => c.productoId !== id);
                renderCarrito();
            }

            function renderCarrito() {
                const cont = document.getElementById('listaCarro');
                let total = 0;
                let cuenta = 0;

                if (!cont) return;

                if (carrito.length === 0) {
                    cont.innerHTML = `
                        <div class="empty-state">
                            <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.5">
                                <circle cx="9" cy="21" r="1"></circle>
                                <circle cx="20" cy="21" r="1"></circle>
                                <path d="M1 1h4l2.68 13.39a2 2 0 0 0 2 1.61h9.72a2 2 0 0 0 2-1.61L23 6H6"></path>
                            </svg>
                            <p>Aún no hay productos en el carrito</p>
                            <span>Seleccione productos para comenzar la venta</span>
                        </div>
                    `;
                } else {
                    cont.innerHTML = "";
                    carrito.forEach(item => {
                        const sub = item.precio * item.cantidad;
                        total += sub;
                        cuenta += item.cantidad;
                        const row = document.createElement('div');
                        row.className = 'cart-line';
                        row.innerHTML = `
                            <div class="c-line-left">
                                <div class="c-line-name">${item.nombre}</div>
                                <div class="c-line-price">$ ${sub.toLocaleString('es-CL')}</div>
                            </div>
                            <div class="c-line-ctrls">
                                <button class="c-btn-step" onclick="modificarCant(${item.productoId}, -1); event.stopPropagation();">-</button>
                                <span style="font-weight:700; font-size:12px; min-width:16px; text-align:center;">${item.cantidad}</span>
                                <button class="c-btn-step" onclick="modificarCant(${item.productoId}, 1); event.stopPropagation();">+</button>
                            </div>
                        `;
                        cont.appendChild(row);
                    });
                }

                const lblSub = document.getElementById('lblSubtotal');
                const lblTot = document.getElementById('lblTotal');
                const badgeV = document.getElementById('cantVenta');
                const badgeM = document.getElementById('cantVentaM');

                if (lblSub) lblSub.innerText = `$ ${total.toLocaleString('es-CL')}`;
                if (lblTot) lblTot.innerText = `$ ${total.toLocaleString('es-CL')}`;
                if (badgeV) badgeV.innerText = cuenta;
                if (badgeM) badgeM.innerText = cuenta;
            }

            function limpiarCarro() {
                carrito = [];
                renderCarrito();
            }

            async function enviarACaja() {
                if (carrito.length === 0) return alert("Seleccione productos primero");
                const res = await fetch('/api/enviar-caja', {
                    method: 'POST',
                    headers: {'Content-Type': 'application/json'},
                    body: JSON.stringify({ vendedor: vendedorActivo, items: carrito })
                });
                const data = await res.json();
                if (data.ok) {
                    const ticketEl = document.getElementById('modalNumTicket');
                    if (ticketEl) ticketEl.innerText = `#${String(data.nroTicket).padStart(6, '0')}`;
                    document.getElementById('modalTicket').classList.add('show');
                    limpiarCarro();
                } else {
                    alert("Error: " + data.mensaje);
                }
            }

            function cerrarModal() {
                document.getElementById('modalTicket').classList.remove('show');
                if (window.innerWidth <= 900) mostrarVistaMovil('catalogo');
            }

            function mostrarVistaMovil(vista) {
                const sb = document.getElementById('sidebarVenta');
                if (!sb) return;
                if (vista === 'carrito') {
                    sb.classList.add('mobile-open');
                    document.getElementById('mBtnCart').classList.add('active');
                    document.getElementById('mBtnCat').classList.remove('active');
                } else {
                    sb.classList.remove('mobile-open');
                    document.getElementById('mBtnCat').classList.add('active');
                    document.getElementById('mBtnCart').classList.remove('active');
                }
            }

            function cerrarSesion() {
                location.reload();
            }
        </script>
    </body>
    </html>
    """;
}