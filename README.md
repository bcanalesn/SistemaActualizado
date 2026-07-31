# Sistema POS Moderno

Manual de instalación, arquitectura y uso del sistema de ventas.

## 1. Tecnologías

| Componente | Tecnología |
| --- | --- |
| Lenguaje | C# |
| Framework | .NET 10 |
| Tipo de aplicación | Windows Forms (WinForms) |
| Base de datos | MySQL 8.0+ o MariaDB |
| Acceso a datos | Entity Framework Core con Pomelo MySQL |
| Interfaz gráfica | GDI+ (`System.Drawing`) |
| Impresión | `System.Drawing.Printing` |

## 2. Extensiones recomendadas para VS Code

- **C#** (Microsoft): autocompletado y depuración.
- **C# Dev Kit** (Microsoft): gestión de proyectos .NET.
- **MySQL / Database Client**: administración de la base de datos desde el editor.

## 3. Requisitos previos

- Windows.
- .NET 10 SDK.
- XAMPP con MySQL, o un servidor MySQL/MariaDB equivalente.
- Visual Studio Code o Visual Studio.

> ⚠️ **Importante sobre la conexión a la base de datos:**
>
> La pantalla de **Login** posee un respaldo de acceso que permite ingresar a la interfaz incluso si el servicio de base de datos no está iniciado. Sin embargo, todas las funciones operativas, como cargar productos demo, registrar ventas, gestionar movimientos de caja, administrar clientes y consultar reportes, requieren obligatoriamente que el servicio de MySQL esté iniciado, por ejemplo, desde el panel de XAMPP.

## 4. Instalación y configuración

### 4.1 Iniciar MySQL

1. Abre el panel de control de XAMPP.
2. Inicia el servicio **MySQL**.
3. Abre [phpMyAdmin](http://localhost/phpmyadmin).
4. Crea una base de datos llamada `sistemaepos` con cotejamiento `utf8mb4_general_ci`.

### 4.2 Crear usuarios de prueba

En phpMyAdmin, selecciona la base de datos `sistemaepos`, abre la pestaña **SQL** y ejecuta:

```sql
CREATE TABLE IF NOT EXISTS `usuarios` (
    `UsuarioID` INT AUTO_INCREMENT PRIMARY KEY,
    `NombreUsuario` VARCHAR(50) NOT NULL UNIQUE,
    `Clave` VARCHAR(100) NOT NULL,
    `NombreCompleto` VARCHAR(100) NOT NULL,
    `Rol` VARCHAR(50) NOT NULL DEFAULT 'Cajero',
    `Estado` TINYINT(1) NOT NULL DEFAULT 1
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

INSERT INTO `usuarios` (`NombreUsuario`, `Clave`, `NombreCompleto`, `Rol`, `Estado`)
VALUES
('barbara', 'admin123', 'Barbara', 'Administrador', 1),
('victor', '123456', 'Victor', 'Cajero', 1);
```
> 💡 **Nota sobre datos de prueba:** Al iniciar la aplicación y MySQL por primera vez, ingresa al módulo de **Productos** y haz clic en **`✨ Cargar Productos Demo`** para poblar automáticamente el catálogo con productos de ejemplo sin necesidad de ejecutar scripts SQL adicionales.

### 4.3 Verificar la conexión

La conexión se configura en [Data/AppDbContext.cs](Data/AppDbContext.cs):

```csharp
string connectionString = "Server=localhost;Database=sistemaepos;Uid=root;Pwd=;";
```

Si el usuario `root` tiene contraseña, escríbela después de `Pwd=`.

### 4.4 Restaurar paquetes y ejecutar

Desde la carpeta raíz del proyecto:

```powershell
dotnet restore
dotnet run
```

## 5. Funcionalidades

### Autenticación y perfiles

El sistema aplica control de acceso basado en roles:

- **Administrador:** acceso a POS, caja, historial, clientes, productos, usuarios y reportes.
- **Cajero:** acceso operativo a POS, caja e historial. Los módulos administrativos se ocultan.

### Punto de venta

- Búsqueda por código de barras o nombre.
- Aumento, reducción y eliminación de productos del carrito.
- Cálculo automático del vuelto.
- Generación e impresión de tickets térmicos.

### Caja y arqueo Z

- Apertura con declaración de fondo inicial.
- Registro de ingresos y retiros de efectivo.
- Cierre Z con comparación entre el efectivo esperado y el efectivo contado.

## 6. Diseño de la interfaz

- Login dividido en área de marca y área de acceso.
- Componentes dibujados con GDI+ y logo isométrico.
- Indicador de sesión activa en el menú principal.
- Categorías de productos diferenciadas por color: lácteos, cecinas, abarrotes, bebidas y aseo.

## 7. Usuarios de prueba

| Nombre | Usuario | Contraseña | Rol | Permisos |
| --- | --- | --- | --- | --- |
| Barbara | `barbara` o `admin` | `admin123` | Administrador | Todos los módulos |
| Victor | `victor` o `cajero` | `123456` | Cajero | POS, caja e historial |

## 8. Estructura del proyecto

```text
SISTEMAACTUALIZADO/
├── Data/
│   └── AppDbContext.cs
├── Models/
│   ├── Usuario.cs
│   ├── Producto.cs
│   ├── Venta.cs
│   ├── Cliente.cs
│   ├── CajaTurno.cs
│   └── DetalleCarrito.cs
├── FormLogin.cs
├── FormMain.cs
├── FormVenta.cs
├── FormTicket.cs
├── FormCaja.cs
├── FormHistorialVentas.cs
├── FormProductos.cs
├── FormClientes.cs
├── FormUsuarios.cs
├── FormReportes.cs
├── Program.cs
└── SistemaActualizado.csproj
```
