# .NET MAUI Hybrid con Blazor

Guía de fundamentos, funcionamiento interno y patrones de uso de **Blazor Hybrid** sobre **.NET MAUI**.

---

## 1. Definiciones fundamentales

### 1.1 .NET MAUI
Framework multiplataforma de Microsoft para construir aplicaciones nativas con un solo código base para Windows, macOS, iOS y Android. Sucesor de Xamarin.Forms.

### 1.2 Blazor
Tecnología de UI de ASP.NET Core que permite escribir componentes web interactivos en C# y Razor (`.razor`) en lugar de JavaScript.

### 1.3 Modos de ejecución de Blazor
Blazor tiene cuatro "sabores", según dónde corre el código C#:

| Modo | Dónde corre C# | Necesita servidor |
|---|---|---|
| **Blazor Server** | En el servidor | Sí (Kestrel + SignalR) |
| **Blazor WebAssembly** | En el navegador (WASM) | Sí, para descargar la app |
| **Blazor Hybrid** | En el proceso nativo (MAUI/WPF/WinForms) | **No** |
| **Blazor Web App (.NET 8+)** | Server + WASM combinados con render modes | Sí |

### 1.4 Blazor Hybrid
Variante donde los componentes Razor se ejecutan **dentro de un proceso .NET nativo** (no en un navegador ni en un servidor) y se renderizan en un WebView embebido. Es el modelo que usa `BlazorWebView` en MAUI.

### 1.5 BlazorWebView
Control MAUI que hospeda el runtime de Blazor dentro de la app. Es la pieza clave que une Blazor con MAUI.

---

## 2. BlazorWebView vs WebView común

### 2.1 WebView común
Un `WebView` estándar es un **navegador embebido**: carga HTML/CSS/JS desde una URL externa o un archivo local. La comunicación con C# es limitada:
- `EvaluateJavaScriptAsync()` para ejecutar JS desde C#.
- Interceptar navegación o usar `postMessage` con handlers manuales.

Es **unidireccional y débilmente acoplado**: el HTML no "sabe" de C#.

### 2.2 BlazorWebView
Control especializado que **hospeda el runtime de Blazor dentro del proceso MAUI**. No carga una web remota: ejecuta componentes Razor compilados en el ensamblado .NET de la app.

### 2.3 Cuadro comparativo

| Aspecto | WebView | BlazorWebView |
|---|---|---|
| Qué ejecuta | HTML/JS remoto o local | Componentes Razor + .NET en proceso |
| Runtime | Solo el motor del navegador | Motor del navegador **+ runtime .NET** |
| Lenguaje de UI | HTML/CSS/JS | Razor (HTML + C#) |
| Comunicación con C# | Manual (JS interop ad-hoc) | Directa: los componentes **son** C# |
| Acceso a APIs nativas | Limitado | Total — DI, servicios MAUI, sensores, FS |
| Despliegue | Puede ser remoto | Todo embebido en la app |

---

## 3. Funcionamiento interno

### 3.1 ¿Hay un servidor web? **No**
No hay Kestrel, no hay HTTP, no hay puerto escuchando. Esto distingue Blazor Hybrid de Blazor Server (que sí usa SignalR) y de Blazor WebAssembly (que se descarga desde un servidor).

### 3.2 ¿Cómo se cargan los archivos de `wwwroot/`?
MAUI registra un **interceptor de esquema** en el WebView nativo. URLs como:

```
https://0.0.0.0/_framework/blazor.webview.js
```

…**no salen a la red**. El interceptor las atrapa y devuelve el archivo desde los **recursos embebidos** del ensamblado. El WebView "cree" que habla HTTP, pero del otro lado hay un handler en .NET que lee de la memoria del proceso.

### 3.3 URL scheme por plataforma

| Plataforma | Base URL |
|---|---|
| Windows (WebView2) | `https://0.0.0.0/` |
| Android | `https://0.0.0.0/` |
| iOS/Mac (WKWebView) | `app://0.0.0.0/` |

> Si hardcodeás URLs, se rompe entre plataformas — usá rutas relativas.

### 3.4 Canal de comunicación
Una vez cargado `blazor.webview.js`, este script abre un canal **IPC propio del WebView** (no HTTP, no WebSocket):

- **Windows:** `WebView2.PostWebMessage` / `window.chrome.webview.postMessage`
- **Android:** `WebView.addJavascriptInterface`
- **iOS/Mac:** `WKScriptMessageHandler`

Por ese canal viajan mensajes serializados con los **diffs del DOM** y los **eventos de UI**.

### 3.5 Diagrama mental

```
┌─────────────────────────────────────────────┐
│   Proceso de tu app MAUI (un solo .exe)     │
│                                             │
│  ┌───────────────┐      ┌────────────────┐  │
│  │  Runtime .NET │◄────►│ WebView nativo │  │
│  │  + Blazor     │ IPC  │  (WebView2/    │  │
│  │  + tus .razor │      │   WKWebView)   │  │
│  └───────────────┘      └────────────────┘  │
│         ▲                                   │
│         │ lee desde                         │
│  ┌──────┴────────┐                          │
│  │ wwwroot/      │  (recursos embebidos     │
│  │ index.html    │   en el ensamblado)      │
│  │ _framework/*  │                          │
│  └───────────────┘                          │
└─────────────────────────────────────────────┘

   ❌ No hay socket TCP
   ❌ No hay servidor HTTP
   ❌ No hay WebAssembly
   ✅ Todo en un proceso, comunicación in-memory
```

---

## 4. Anatomía de una app MAUI Hybrid

### 4.1 Declaración del control en XAML

Ejemplo extraído de [MainPage.xaml](../Ejemplo_HolaMundo/MainPage.xaml):

```xml
<ContentPage xmlns="http://schemas.microsoft.com/dotnet/2021/maui"
             xmlns:x="http://schemas.microsoft.com/winfx/2009/xaml"
             xmlns:components="clr-namespace:Ejemplo_HolaMundo.Components"
             x:Class="Ejemplo_HolaMundo.MainPage">

    <BlazorWebView x:Name="blazorWebView" HostPage="wwwroot/index.html">
        <BlazorWebView.RootComponents>
            <RootComponent Selector="#app"
                           ComponentType="{x:Type components:Routes}" />
        </BlazorWebView.RootComponents>
    </BlazorWebView>
</ContentPage>
```

### 4.2 Piezas clave

1. **`HostPage="wwwroot/index.html"`** — página HTML "shell" que carga `_framework/blazor.webview.js`. Ese script es el puente entre el WebView y el runtime Blazor.
2. **`RootComponents`** — registra qué componente Razor montar y dónde.
3. **`Selector="#app"`** — selector CSS al `<div id="app">` dentro de `index.html`.
4. **`ComponentType="{x:Type components:Routes}"`** — el componente raíz, declarado en C#.

### 4.3 `wwwroot/index.html` típico

```html
<!DOCTYPE html>
<html>
<head>
    <meta charset="utf-8" />
    <title>Mi App</title>
    <base href="/" />
    <link rel="stylesheet" href="css/app.css" />
</head>
<body>
    <div id="app">Cargando...</div>
    <script src="_framework/blazor.webview.js" autostart="false"></script>
</body>
</html>
```

### 4.4 Registro en `MauiProgram.cs`

```csharp
public static MauiApp CreateMauiApp()
{
    var builder = MauiApp.CreateBuilder();
    builder
        .UseMauiApp<App>()
        .ConfigureFonts(fonts => { ... });

    builder.Services.AddMauiBlazorWebView();   // <- imprescindible

#if DEBUG
    builder.Services.AddBlazorWebViewDeveloperTools();
#endif

    return builder.Build();
}
```

---

## 5. Ciclo de vida y arranque

Cuando MAUI instancia el `BlazorWebView`:

1. Se construye el `WebView` nativo de la plataforma.
2. Se registra un **`IFileProvider`** que apunta a los recursos embebidos de `wwwroot/`.
3. Se registra un **`BlazorWebViewHandler`** específico por plataforma (en `Microsoft.AspNetCore.Components.WebView.Maui`).
4. Se monta un **`WebViewManager`** — la pieza central que hospeda el `Renderer` de Blazor.
5. Cuando el WebView pide `index.html`, el manager lo sirve y luego procesa los `<RootComponent>` declarados en XAML.

---

## 6. Inyección de dependencias compartida

El `IServiceProvider` de tus componentes Razor **es el mismo** que el de la app MAUI. Lo que registres en `MauiProgram.cs` lo podés inyectar en cualquier `.razor`.

### 6.1 Registro

```csharp
builder.Services.AddSingleton<IMiServicio, MiServicio>();
builder.Services.AddSingleton(IGeolocation.Default);
builder.Services.AddSingleton(IConnectivity.Default);
```

### 6.2 Uso desde Razor

```razor
@page "/ubicacion"
@inject IGeolocation Geo

<button @onclick="ObtenerUbicacion">Obtener ubicación</button>

@if (lugar is not null)
{
    <p>Lat: @lugar.Latitude, Lng: @lugar.Longitude</p>
}

@code {
    Location? lugar;

    async Task ObtenerUbicacion()
    {
        lugar = await Geo.GetLocationAsync();
    }
}
```

Acceso directo a `IFileSystem`, `IConnectivity`, `IGeolocation`, `IDeviceInfo`, etc., **sin** wrappers ni JS interop.

---

## 7. Hilo de UI y rendimiento

- Los componentes Razor corren en el **mismo dispatcher de UI** que MAUI.
- Podés llamar APIs de MAUI directamente desde un `@onclick` sin marshalling.
- Para trabajo pesado (CPU-bound), seguís usando `Task.Run`.
- El renderizado se hace por **diffs**: solo los cambios viajan al WebView.

---

## 8. Múltiples RootComponents

Podés montar varios componentes Razor en distintos `<div>` de la misma página:

```xml
<BlazorWebView.RootComponents>
    <RootComponent Selector="#app"     ComponentType="{x:Type c:Routes}" />
    <RootComponent Selector="#sidebar" ComponentType="{x:Type c:Sidebar}" />
    <RootComponent Selector="#footer"  ComponentType="{x:Type c:Footer}" />
</BlazorWebView.RootComponents>
```

Útil para integrar islas Blazor en un layout HTML existente.

---

## 9. Hot Reload y debugging

- **Hot Reload** de Razor funciona (XAML hot reload es independiente).
- Breakpoints normales en C# de tus `.razor` — corre como .NET nativo, no como WASM.
- Para debuggear el JS/HTML del WebView usás las DevTools nativas:
  - **Windows (WebView2):** `F12` o click derecho → Inspect.
  - **Android:** `chrome://inspect` desde Chrome.
  - **iOS/Mac:** Safari → Develop menu.

Para habilitar las DevTools en release, necesitás:

```csharp
#if DEBUG
builder.Services.AddBlazorWebViewDeveloperTools();
#endif
```

---

## 10. ¿Se pueden usar páginas Blazor Interactive Server?

**Respuesta corta: no directamente dentro del `BlazorWebView`.**

### 10.1 Por qué no
`Interactive Server` requiere:
- Un servidor ASP.NET Core con SignalR.
- Una conexión WebSocket cliente↔servidor.
- Que el componente se renderice en el servidor y los eventos viajen por el circuito.

`BlazorWebView` no tiene servidor ni circuito SignalR. Su modo de render es **`InteractiveWebView`** (modo Hybrid), un cuarto modo aparte de Server / WebAssembly / Auto.

Si en un componente dentro de un `BlazorWebView` ponés `@rendermode InteractiveServer`, lo van a ignorar o tirar excepción según la versión: no hay infraestructura de circuito que lo soporte.

### 10.2 Qué sí podés hacer

#### Opción A — Componentes Razor compartidos (recomendado)
Crear un proyecto **Razor Class Library (RCL)** con tus `.razor` y consumirlo desde dos hosts:

```
┌──────────────────────┐    ┌──────────────────────┐
│  App MAUI Hybrid     │    │  App Blazor Server   │
│  (BlazorWebView)     │    │  (Kestrel + SignalR) │
└──────────┬───────────┘    └──────────┬───────────┘
           │                           │
           └───────────┬───────────────┘
                       ▼
              ┌────────────────┐
              │  RCL: páginas, │
              │  componentes,  │
              │  layouts       │
              └────────────────┘
```

Mismo código de UI, dos modos de despliegue: offline en MAUI, online en web.

#### Opción B — Cargar una web Blazor Server externa en un `WebView` normal
Si tu objetivo es *mostrar* una app Blazor Server existente desde MAUI:

```xml
<WebView Source="https://miapp.contoso.com" />
```

Acá sí hay servidor, SignalR, todo el modelo Server normal — pero perdés la integración nativa con MAUI.

#### Opción C — Híbrido con `BlazorWebView` apuntando a host externo
Variante avanzada: configurar el `WebViewManager` para que parte del contenido venga de un servidor remoto. Funciona pero rompe el modelo offline.

### 10.3 Resumen de decisión

| Querés… | Solución |
|---|---|
| Componentes Razor offline en MAUI | `BlazorWebView` (Hybrid) |
| Mismos componentes online y offline | RCL compartida + dos hosts |
| Mostrar una app Blazor Server existente | `WebView` clásico apuntando a la URL |
| Interactive Server *dentro* del proceso MAUI | No es posible |

> **Regla mental:** `BlazorWebView` reemplaza al servidor con el proceso local. Si necesitás circuito SignalR, necesitás un servidor real, y entonces ya no es Hybrid.

---

## 11. Cuándo usar Blazor Hybrid

### 11.1 Buenos casos
- Equipo con experiencia web (HTML/CSS/Razor) que necesita ir a mobile/desktop.
- Reutilizar componentes ya existentes de una app Blazor Server o WASM.
- App que necesita acceso pleno a APIs nativas + UI compleja basada en componentes.
- Despliegue offline-first.

### 11.2 Malos casos
- UI puramente nativa con animaciones avanzadas → conviene XAML/MAUI puro.
- App donde el bundle size es crítico → BlazorWebView agrega el runtime de Blazor.
- Juegos o gráficos intensivos → SkiaSharp o engines especializados.

---

## 12. Glosario rápido

| Término | Significado |
|---|---|
| **MAUI** | Multi-platform App UI — framework nativo multiplataforma de .NET. |
| **Blazor** | Framework de componentes Razor de ASP.NET Core. |
| **Hybrid** | Modelo donde Blazor corre en proceso nativo, no en navegador/servidor. |
| **BlazorWebView** | Control MAUI que hospeda el runtime Blazor. |
| **RootComponent** | Componente Razor raíz montado por `BlazorWebView`. |
| **HostPage** | HTML "shell" cargado por el WebView. |
| **RCL** | Razor Class Library — biblioteca reutilizable de componentes. |
| **WebViewManager** | Clase que orquesta render + IPC entre Blazor y el WebView nativo. |
| **Render mode** | Modo en que un componente se ejecuta: Server / WebAssembly / Auto / WebView. |
