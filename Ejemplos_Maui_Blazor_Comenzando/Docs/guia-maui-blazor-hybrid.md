# De MAUI a MAUI Blazor Hybrid: guía comparativa con Bootstrap y MudBlazor

> Documento de referencia para desarrolladores que vienen de MAUI tradicional (XAML)
> y están adoptando MAUI Blazor Hybrid, ya sea con Bootstrap o con MudBlazor como
> librería de UI.

## Tabla de contenidos

1. [Conceptos fundamentales](#1-conceptos-fundamentales)
2. [Configuración inicial del proyecto](#2-configuración-inicial-del-proyecto)
3. [Estructura del proyecto](#3-estructura-del-proyecto)
4. [Servicios y registro en `MauiProgram.cs`](#4-servicios-y-registro-en-mauiprogramcs)
5. [Shell y navegación](#5-shell-y-navegación)
6. [Layouts (contenedores)](#6-layouts-contenedores)
7. [Views (controles)](#7-views-controles)
8. [Modales y feedback al usuario](#8-modales-y-feedback-al-usuario)
9. [Temas y estilos](#9-temas-y-estilos)
10. [Cuándo elegir cada stack](#10-cuándo-elegir-cada-stack)
11. [Apéndice: snippets útiles](#11-apéndice-snippets-útiles)

---

## 1. Conceptos fundamentales

### MAUI puro
Aplicación nativa multiplataforma que usa **XAML** para la UI y **C#** para la lógica.
Cada `View` se renderiza con un control nativo del sistema operativo (un `Button`
es un `UIButton` en iOS, un `android.widget.Button` en Android, etc.).

### MAUI Blazor Hybrid
Aplicación MAUI que en lugar de XAML usa **componentes Blazor** (Razor) renderizados
dentro de un `BlazorWebView`. La UI se renderiza con HTML/CSS/JS dentro de un WebView,
pero la lógica corre en .NET nativo (no es un servidor web ni un Blazor Server).

- **Ventajas**: reutilización de componentes web, ecosistema HTML/CSS, librerías web maduras, theming con CSS.
- **Trade-offs**: menos "nativo" en sensación; renderizado dentro de un WebView (overhead mínimo pero existe).

### Bootstrap vs MudBlazor

| | Bootstrap | MudBlazor |
|---|---|---|
| Tipo | Librería CSS clásica | Librería de componentes Blazor |
| Filosofía | HTML pelado + clases utilitarias | Componentes "altos" en C# |
| Inspiración | Sistema propio | Material Design |
| Trabajás con | Etiquetas HTML estándar | Componentes `<MudXxx>` |
| Theming | Variables CSS | API C# (`MudTheme`) |
| Ideal para | Apps con mucho contenido, landings, control fino del HTML | Dashboards, CRUDs, apps internas |

---

## 2. Configuración inicial del proyecto

### 2.1 Crear el proyecto

Desde Visual Studio: *.NET MAUI Blazor Hybrid App*.
Desde CLI:

```bash
dotnet new maui-blazor -n MiApp
```

La plantilla viene **con Bootstrap incluido** por defecto.

### 2.2 Librerías a instalar

#### Bootstrap (ya viene)
Bootstrap está en `wwwroot/css/bootstrap/bootstrap.min.css`. Si querés actualizar a la última versión, descargalo de [getbootstrap.com](https://getbootstrap.com) y reemplazalo.

Recomendación adicional para iconos:
```html
<link rel="stylesheet"
      href="https://cdn.jsdelivr.net/npm/bootstrap-icons/font/bootstrap-icons.css">
```

#### MudBlazor

```bash
dotnet add package MudBlazor
```

Y también referenciar sus assets en `wwwroot/index.html` (siguiente sección).

### 2.3 Configurar `wwwroot/index.html`

#### Versión Bootstrap (default de la plantilla)

```html
<!DOCTYPE html>
<html>
<head>
    <meta charset="utf-8" />
    <meta name="viewport" content="width=device-width, initial-scale=1.0" />
    <title>MiApp</title>
    <base href="/" />
    <link rel="stylesheet" href="css/bootstrap/bootstrap.min.css" />
    <link rel="stylesheet" href="css/app.css" />
</head>
<body>
    <div id="app">Loading...</div>
    <div id="blazor-error-ui">...</div>
    <script src="_framework/blazor.webview.js" autostart="false"></script>
</body>
</html>
```

#### Versión MudBlazor

Agregar las refs CSS/JS de MudBlazor (y opcionalmente quitar Bootstrap si no lo combinás):

```html
<head>
    <meta charset="utf-8" />
    <meta name="viewport" content="width=device-width, initial-scale=1.0" />
    <title>MiApp</title>
    <base href="/" />
    <link href="_content/MudBlazor/MudBlazor.min.css" rel="stylesheet" />
    <link rel="stylesheet" href="css/app.css" />
</head>
<body>
    <div id="app">Loading...</div>
    <div id="blazor-error-ui">...</div>
    <script src="_content/MudBlazor/MudBlazor.min.js"></script>
    <script src="_framework/blazor.webview.js" autostart="false"></script>
</body>
</html>
```

---

## 3. Estructura del proyecto

```
MiApp/
├── Platforms/                   # Código específico (Android, iOS, Windows, Mac)
├── Resources/                   # Assets compartidos
│   ├── AppIcon/
│   ├── Fonts/
│   ├── Images/
│   ├── Splash/
│   └── Styles/                  # Colors.xaml, Styles.xaml (MAUI nativo)
├── wwwroot/                     # ⬅️ específico de Blazor Hybrid
│   ├── css/
│   │   ├── bootstrap/
│   │   └── app.css
│   └── index.html
├── Components/                  # Componentes Blazor
│   ├── Layout/
│   │   ├── MainLayout.razor
│   │   ├── MainLayout.razor.css
│   │   └── NavMenu.razor
│   ├── Pages/                   # Páginas con @page
│   │   ├── Home.razor
│   │   ├── GridLayoutPage.razor
│   │   └── StackLayoutPage.razor
│   ├── Routes.razor
│   ├── _Imports.razor
│   └── App.razor
├── App.xaml / App.xaml.cs
├── MainPage.xaml                # ⬅️ host del BlazorWebView
├── MainPage.xaml.cs
└── MauiProgram.cs               # configuración y DI
```

### Diferencia clave con MAUI puro
- En **MAUI puro**, las páginas son archivos `.xaml` con su `.cs` code-behind, y la navegación se gestiona con `Shell` o `NavigationPage`.
- En **MAUI Blazor Hybrid**, hay UNA sola página MAUI (`MainPage.xaml`) que contiene un `BlazorWebView`, y el resto de la UI son componentes `.razor`. La "navegación de páginas" es enrutamiento de Blazor (`@page`).

### `MainPage.xaml`: el host del WebView

```xml
<?xml version="1.0" encoding="utf-8" ?>
<ContentPage xmlns="http://schemas.microsoft.com/dotnet/2021/maui"
             xmlns:x="http://schemas.microsoft.com/winfx/2009/xaml"
             xmlns:b="clr-namespace:Microsoft.AspNetCore.Components.WebView.Maui;assembly=Microsoft.AspNetCore.Components.WebView.Maui"
             xmlns:local="clr-namespace:MiApp"
             x:Class="MiApp.MainPage"
             BackgroundColor="{DynamicResource PageBackgroundColor}">

    <b:BlazorWebView HostPage="wwwroot/index.html">
        <b:BlazorWebView.RootComponents>
            <b:RootComponent Selector="#app"
                             ComponentType="{x:Type local:Components.Routes}" />
        </b:BlazorWebView.RootComponents>
    </b:BlazorWebView>

</ContentPage>
```

Esto es **exactamente el mismo XAML para Bootstrap o MudBlazor** — la diferencia está en lo que pasa adentro del `Routes`/`MainLayout`.

---

## 4. Servicios y registro en `MauiProgram.cs`

### MAUI puro

```csharp
public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();
        builder
            .UseMauiApp<App>()
            .ConfigureFonts(fonts =>
            {
                fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
            });

        // Servicios propios
        builder.Services.AddSingleton<IMyService, MyService>();

        return builder.Build();
    }
}
```

### MAUI Blazor Hybrid + Bootstrap

```csharp
public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();
        builder
            .UseMauiApp<App>()
            .ConfigureFonts(fonts =>
            {
                fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
            });

        // ⬅️ habilita el BlazorWebView
        builder.Services.AddMauiBlazorWebView();

#if DEBUG
        builder.Services.AddBlazorWebViewDeveloperTools();
        builder.Logging.AddDebug();
#endif

        return builder.Build();
    }
}
```

### MAUI Blazor Hybrid + MudBlazor

```csharp
using MudBlazor.Services;  // ⬅️

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();
        builder
            .UseMauiApp<App>()
            .ConfigureFonts(fonts =>
            {
                fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
            });

        builder.Services.AddMauiBlazorWebView();

        // ⬅️ registra todos los servicios de MudBlazor
        // (Snackbar, Dialog, Popover, etc.)
        builder.Services.AddMudServices();

#if DEBUG
        builder.Services.AddBlazorWebViewDeveloperTools();
        builder.Logging.AddDebug();
#endif

        return builder.Build();
    }
}
```

### `_Imports.razor` (con MudBlazor)

```csharp
@using System.Net.Http
@using Microsoft.AspNetCore.Components.Forms
@using Microsoft.AspNetCore.Components.Routing
@using Microsoft.AspNetCore.Components.Web
@using Microsoft.AspNetCore.Components.Web.Virtualization
@using Microsoft.JSInterop
@using MudBlazor                    @* ⬅️ *@
@using MiApp.Components
```

---

## 5. Shell y navegación

### MAUI puro: `AppShell`

En MAUI, la navegación se gestiona con `AppShell.xaml`:

```xml
<Shell xmlns="http://schemas.microsoft.com/dotnet/2021/maui"
       xmlns:x="http://schemas.microsoft.com/winfx/2009/xaml"
       xmlns:pages="clr-namespace:MiApp.Pages"
       x:Class="MiApp.AppShell">

    <ShellContent Title="Inicio"
                  ContentTemplate="{DataTemplate pages:MainPage}" />
    <ShellContent Title="Grid"
                  ContentTemplate="{DataTemplate pages:GridLayoutPage}" />
    <ShellContent Title="Stack"
                  ContentTemplate="{DataTemplate pages:StackLayoutPage}" />
</Shell>
```

Navegación programática:
```csharp
await Shell.Current.GoToAsync("//GridLayoutPage");
```

### Blazor Hybrid: `Routes.razor` + `MainLayout.razor`

**`Components/Routes.razor`** (router):

```razor
<Router AppAssembly="@typeof(MauiProgram).Assembly">
    <Found Context="routeData">
        <RouteView RouteData="@routeData"
                   DefaultLayout="@typeof(Layout.MainLayout)" />
    </Found>
</Router>
```

**`Components/Layout/MainLayout.razor`** (estructura común a todas las páginas):

#### Versión Bootstrap

```razor
@inherits LayoutComponentBase

<div class="page">
    <div class="sidebar">
        <NavMenu />
    </div>

    <main>
        <article class="content px-4">
            @Body
        </article>
    </main>
</div>
```

#### Versión MudBlazor

```razor
@inherits LayoutComponentBase

<MudThemeProvider />
<MudPopoverProvider />
<MudDialogProvider />
<MudSnackbarProvider />

<MudLayout>
    <MudAppBar Elevation="1">
        <MudIconButton Icon="@Icons.Material.Filled.Menu"
                       Color="Color.Inherit"
                       Edge="Edge.Start"
                       OnClick="@((e) => DrawerToggle())" />
        <MudText Typo="Typo.h5">MiApp</MudText>
    </MudAppBar>

    <MudDrawer @bind-Open="_drawerOpen" Elevation="2">
        <NavMenu />
    </MudDrawer>

    <MudMainContent>
        <MudContainer Class="my-4">
            @Body
        </MudContainer>
    </MudMainContent>
</MudLayout>

@code {
    private bool _drawerOpen = true;

    private void DrawerToggle() => _drawerOpen = !_drawerOpen;
}
```

> ⚠️ **Importante con MudBlazor**: los 4 *providers* (`MudThemeProvider`, `MudPopoverProvider`, `MudDialogProvider`, `MudSnackbarProvider`) son **obligatorios** y van una sola vez en el `MainLayout`. Sin ellos, los diálogos, snackbars y tooltips no funcionan.

### Navegación programática en Blazor

```razor
@inject NavigationManager Nav

<button @onclick="@(() => Nav.NavigateTo("/GridLayoutPage"))">
    Ir
</button>
```

O lo más idiomático para web: `<a href="/GridLayoutPage">Ir</a>`.

---

## 6. Layouts (contenedores)

### 6.1 `StackLayout`

#### Tabla de equivalencias

| MAUI | Bootstrap | MudBlazor |
|---|---|---|
| `Orientation="Vertical"` (default) | `d-flex flex-column` | `MudStack` (default) |
| `Orientation="Horizontal"` | `d-flex flex-row` | `MudStack Row="true"` |
| `Spacing="10"` | `gap-2` (8px) o `style="gap: 10px;"` | `Spacing="2"` |
| `HorizontalOptions="Start"` | `align-self-start` | `align-self-start` |
| `HorizontalOptions="Center"` | `align-self-center` | `align-self-center` |
| `HorizontalOptions="End"` | `align-self-end` | `align-self-end` |

#### Ejemplo: 3 cajas con alineaciones distintas

**MAUI:**
```xml
<StackLayout Spacing="10" Margin="10">
    <BoxView BackgroundColor="#ADCBFF" HeightRequest="100" WidthRequest="100"
             HorizontalOptions="Start"/>
    <BoxView BackgroundColor="#CCFFAD" HeightRequest="100" WidthRequest="100"
             HorizontalOptions="Center"/>
    <BoxView BackgroundColor="#ADCBFF" HeightRequest="100" WidthRequest="100"
             HorizontalOptions="End"/>
</StackLayout>
```

**Bootstrap:**
```razor
<div class="d-flex flex-column m-2" style="gap: 10px;">
    <div class="align-self-start"
         style="background-color: #ADCBFF; width: 100px; height: 100px;"></div>
    <div class="align-self-center"
         style="background-color: #CCFFAD; width: 100px; height: 100px;"></div>
    <div class="align-self-end"
         style="background-color: #ADCBFF; width: 100px; height: 100px;"></div>
</div>
```

**MudBlazor:**
```razor
<MudStack Spacing="2" Class="ma-2">
    <MudPaper Class="align-self-start"
              Style="background-color: #ADCBFF; width: 100px; height: 100px;"
              Square="true" Elevation="0" />
    <MudPaper Class="align-self-center"
              Style="background-color: #CCFFAD; width: 100px; height: 100px;"
              Square="true" Elevation="0" />
    <MudPaper Class="align-self-end"
              Style="background-color: #ADCBFF; width: 100px; height: 100px;"
              Square="true" Elevation="0" />
</MudStack>
```

### 6.2 `Grid`

#### Tabla de equivalencias

| MAUI | CSS Grid (universal) | MudGrid (12 cols, sin row-span) |
|---|---|---|
| `RowDefinitions="*,*,*"` | `grid-template-rows: 1fr 1fr 1fr` | (no aplica, es row-based) |
| `ColumnDefinitions="*,*,*"` | `grid-template-columns: 1fr 1fr 1fr` | divisiones de 12: `xs="4"` |
| `RowSpacing` / `ColumnSpacing` | `row-gap` / `column-gap` / `gap` | `Spacing="N"` |
| `Grid.Row="N"` | `grid-row: N+1` | (no aplica) |
| `Grid.Column="N"` | `grid-column: N+1` | (proporción `xs`) |
| `Grid.RowSpan="2"` | `grid-row: N / span 2` | ⚠️ **no soportado**, requiere anidamiento |
| `Grid.ColumnSpan="2"` | `grid-column: N / span 2` | sumar al `xs` |

> ⚠️ **Importante**: ni Bootstrap ni MudBlazor tienen un grid 2D real. Para layouts con
> row-span, **CSS Grid puro es la mejor opción** en ambos stacks. `MudGrid` es 12-col
> basado en filas; replicar row-span requiere anidar `MudStack` dentro de un `MudItem`.

#### Ejemplo: Grid 3×3 con celda 2×2 abajo a la derecha

```
[ A ][ B ][ C ]
[ D ][         ]
[ E ][    F    ]
```

**MAUI:**
```xml
<Grid ColumnSpacing="10" RowSpacing="10" Margin="10">
    <Grid.RowDefinitions>
        <RowDefinition Height="*" />
        <RowDefinition Height="*" />
        <RowDefinition Height="*" />
    </Grid.RowDefinitions>
    <Grid.ColumnDefinitions>
        <ColumnDefinition Width="*" />
        <ColumnDefinition Width="*" />
        <ColumnDefinition Width="*" />
    </Grid.ColumnDefinitions>

    <BoxView Grid.Column="0" Grid.Row="0" BackgroundColor="#ADCBFF"/>
    <BoxView Grid.Column="1" Grid.Row="0" BackgroundColor="#CCFFAD"/>
    <BoxView Grid.Column="2" Grid.Row="0" BackgroundColor="#ADCBFF"/>
    <BoxView Grid.Column="0" Grid.Row="1" BackgroundColor="#CCFFAD"/>
    <BoxView Grid.Column="0" Grid.Row="2" BackgroundColor="#ADCBFF"/>
    <BoxView Grid.Column="1" Grid.Row="1" Grid.ColumnSpan="2" Grid.RowSpan="2"
             BackgroundColor="#ADCBFF"/>
</Grid>
```

**Bootstrap (con CSS Grid puro):**
```razor
<div class="m-2"
     style="display: grid;
            grid-template-columns: 1fr 1fr 1fr;
            grid-template-rows: 150px 150px 150px;
            gap: 10px;">
    <div style="background-color: #ADCBFF; grid-column: 1; grid-row: 1;"></div>
    <div style="background-color: #CCFFAD; grid-column: 2; grid-row: 1;"></div>
    <div style="background-color: #ADCBFF; grid-column: 3; grid-row: 1;"></div>
    <div style="background-color: #CCFFAD; grid-column: 1; grid-row: 2;"></div>
    <div style="background-color: #ADCBFF; grid-column: 1; grid-row: 3;"></div>
    <div style="background-color: #ADCBFF;
                grid-column: 2 / span 2;
                grid-row: 2 / span 2;"></div>
</div>
```

> 💡 **Tip**: usar alturas fijas (`150px`) en `grid-template-rows` evita problemas de colapso de filas. Si querés `1fr` (proporcional), asegurate de que `html, body, #app, main` tengan `height: 100%` (ver apéndice).

**MudBlazor (CSS Grid + `MudPaper`):**
```razor
<div class="grid-layout ma-2">
    <MudPaper Style="background-color: #ADCBFF; grid-column: 1; grid-row: 1;"
              Square="true" Elevation="0" />
    <MudPaper Style="background-color: #CCFFAD; grid-column: 2; grid-row: 1;"
              Square="true" Elevation="0" />
    <MudPaper Style="background-color: #ADCBFF; grid-column: 3; grid-row: 1;"
              Square="true" Elevation="0" />
    <MudPaper Style="background-color: #CCFFAD; grid-column: 1; grid-row: 2;"
              Square="true" Elevation="0" />
    <MudPaper Style="background-color: #ADCBFF; grid-column: 1; grid-row: 3;"
              Square="true" Elevation="0" />
    <MudPaper Style="background-color: #ADCBFF;
                     grid-column: 2 / span 2;
                     grid-row: 2 / span 2;"
              Square="true" Elevation="0" />
</div>

<style>
    .grid-layout {
        display: grid;
        grid-template-columns: 1fr 1fr 1fr;
        grid-template-rows: 150px 150px 150px;
        gap: 10px;
    }
</style>
```

**MudBlazor (alternativa con `MudGrid` + nesting):**
```razor
<MudGrid Spacing="2" Class="ma-2">
    @* Fila superior: A, B, C cada uno xs="4" (4/12 = 1/3) *@
    <MudItem xs="4">
        <MudPaper Style="background-color: #ADCBFF; height: 100px;"
                  Square="true" Elevation="0" />
    </MudItem>
    <MudItem xs="4">
        <MudPaper Style="background-color: #CCFFAD; height: 100px;"
                  Square="true" Elevation="0" />
    </MudItem>
    <MudItem xs="4">
        <MudPaper Style="background-color: #ADCBFF; height: 100px;"
                  Square="true" Elevation="0" />
    </MudItem>

    @* Fila inferior: D+E apilados (xs="4") + F que ocupa xs="8" *@
    <MudItem xs="4">
        <MudStack Spacing="2">
            <MudPaper Style="background-color: #CCFFAD; height: 100px;"
                      Square="true" Elevation="0" />
            <MudPaper Style="background-color: #ADCBFF; height: 100px;"
                      Square="true" Elevation="0" />
        </MudStack>
    </MudItem>
    <MudItem xs="8">
        <MudPaper Style="background-color: #ADCBFF; height: 208px;"
                  Square="true" Elevation="0" />
    </MudItem>
</MudGrid>
```

> 💡 La altura `208px` de F = 100px + 8px (gap del Stack) + 100px, para que coincida con la suma de D+E.

### 6.3 Otros layouts

| MAUI | Bootstrap | MudBlazor |
|---|---|---|
| `FlexLayout` | `d-flex flex-wrap` con utilidades | CSS flex en `Style` |
| `AbsoluteLayout` | `position-absolute` + coords | igual, CSS plano |
| `ScrollView` | `<div class="overflow-auto">` | `<div style="overflow:auto">` |
| `RefreshView` (pull to refresh) | ⚠️ no aplica en web | ⚠️ no hay equivalente |

---

## 7. Views (controles)

### 7.1 Texto

| MAUI | Bootstrap | MudBlazor |
|---|---|---|
| `Label` | `<span>` / `<p>` / `<h1>`–`<h6>` con `text-*` / `fs-*` | `MudText Typo="..."` |

```xml
<!-- MAUI -->
<Label Text="Hola" FontSize="20" TextColor="Black" />
```
```razor
<!-- Bootstrap -->
<p class="fs-5">Hola</p>

<!-- MudBlazor -->
<MudText Typo="Typo.h5">Hola</MudText>
```

### 7.2 Inputs

| MAUI | Bootstrap | MudBlazor |
|---|---|---|
| `Entry` | `<input class="form-control">` | `MudTextField` |
| `Editor` | `<textarea class="form-control">` | `MudTextField Lines="3"` |
| `Picker` | `<select class="form-select">` | `MudSelect` |
| `DatePicker` | `<input type="date" class="form-control">` | `MudDatePicker` |
| `TimePicker` | `<input type="time" class="form-control">` | `MudTimePicker` |
| `CheckBox` | `<input type="checkbox" class="form-check-input">` | `MudCheckBox` |
| `Switch` | `<input type="checkbox" role="switch" class="form-check-input">` | `MudSwitch` |
| `RadioButton` | `<input type="radio" class="form-check-input">` | `MudRadio` en `MudRadioGroup` |
| `Slider` | `<input type="range" class="form-range">` | `MudSlider` |
| `Stepper` | `<input type="number">` con botones | `MudNumericField` |
| `SearchBar` | `<input type="search" class="form-control">` | `MudTextField` con `Adornment` |

#### Ejemplo: Entry con label

**MAUI:**
```xml
<VerticalStackLayout>
    <Label Text="Nombre" />
    <Entry Placeholder="Tu nombre" Text="{Binding Nombre}" />
</VerticalStackLayout>
```

**Bootstrap:**
```razor
<div class="mb-3">
    <label class="form-label">Nombre</label>
    <input class="form-control" placeholder="Tu nombre" @bind="nombre" />
</div>

@code { private string nombre = ""; }
```

**MudBlazor:**
```razor
<MudTextField @bind-Value="nombre"
              Label="Nombre"
              Placeholder="Tu nombre" />

@code { private string nombre = ""; }
```

### 7.3 Botones

| MAUI | Bootstrap | MudBlazor |
|---|---|---|
| `Button` | `<button class="btn btn-primary">` | `MudButton Variant="Variant.Filled" Color="Color.Primary"` |
| `ImageButton` | `<button class="btn"><img/></button>` | `MudIconButton` |
| FAB | (manual con CSS) | `MudFab` |

```xml
<!-- MAUI -->
<Button Text="Guardar" Clicked="OnGuardar" BackgroundColor="Blue" />
```
```razor
<!-- Bootstrap -->
<button class="btn btn-primary" @onclick="OnGuardar">Guardar</button>

<!-- MudBlazor -->
<MudButton Variant="Variant.Filled" Color="Color.Primary" OnClick="OnGuardar">
    Guardar
</MudButton>
```

### 7.4 Imágenes e íconos

| MAUI | Bootstrap | MudBlazor |
|---|---|---|
| `Image` | `<img class="img-fluid">` | `MudImage` |
| `FontImageSource` | `<i class="bi bi-...">` (Bootstrap Icons) | `MudIcon Icon="@Icons.Material.Filled..."` |
| Avatar redondeado | `<img class="rounded-circle">` | `MudAvatar` |

```razor
<!-- Bootstrap Icons -->
<i class="bi bi-house"></i>
<i class="bi bi-person-circle fs-1"></i>

<!-- MudBlazor Icons -->
<MudIcon Icon="@Icons.Material.Filled.Home" />
<MudIcon Icon="@Icons.Material.Outlined.Person" Size="Size.Large" />
```

### 7.5 Superficies y tarjetas

| MAUI | Bootstrap | MudBlazor |
|---|---|---|
| `BoxView` (rectángulo plano) | `<div style="background-color:...">` | `MudPaper Square="true" Elevation="0"` |
| `Border` (con bordes/curvas) | `<div class="border rounded-3">` | `MudPaper` con `Style` |
| `Frame` (deprecada) | `<div class="card shadow-sm">` | `MudCard` o `MudPaper Elevation="2"` |
| Card con secciones | `card` + `card-header/body/footer` | `MudCard` + `MudCardHeader` + `MudCardContent` + `MudCardActions` |

### 7.6 Listas y colecciones

| MAUI | Bootstrap | MudBlazor |
|---|---|---|
| `ListView` simple | `<ul class="list-group">` | `MudList` con `MudListItem` |
| `CollectionView` | `@foreach` + cards | `@foreach` + `MudCard`, o `MudDataGrid` |
| `CarouselView` | Bootstrap Carousel | `MudCarousel` |
| `TableView` (settings) | `list-group` con secciones | `MudList` con `MudDivider` |

#### Ejemplo: lista de items

**MAUI:**
```xml
<CollectionView ItemsSource="{Binding Items}">
    <CollectionView.ItemTemplate>
        <DataTemplate>
            <Label Text="{Binding Nombre}" />
        </DataTemplate>
    </CollectionView.ItemTemplate>
</CollectionView>
```

**Bootstrap:**
```razor
<ul class="list-group">
    @foreach (var item in items)
    {
        <li class="list-group-item">@item.Nombre</li>
    }
</ul>
```

**MudBlazor:**
```razor
<MudList T="Item">
    @foreach (var item in items)
    {
        <MudListItem T="Item" Text="@item.Nombre" />
    }
</MudList>
```

### 7.7 Progress y actividad

| MAUI | Bootstrap | MudBlazor |
|---|---|---|
| `ActivityIndicator` | `<div class="spinner-border">` | `MudProgressCircular Indeterminate="true"` |
| `ProgressBar` | `<div class="progress"><div class="progress-bar">` | `MudProgressLinear Value="..."` |

```razor
<!-- Bootstrap -->
<div class="spinner-border text-primary" role="status"></div>
<div class="progress">
    <div class="progress-bar" style="width: 60%"></div>
</div>

<!-- MudBlazor -->
<MudProgressCircular Indeterminate="true" Color="Color.Primary" />
<MudProgressLinear Value="60" Color="Color.Primary" />
```

---

## 8. Modales y feedback al usuario

### 8.1 Alertas simples (`DisplayAlert`)

#### MAUI
```csharp
await DisplayAlert("Título", "Mensaje", "OK");

bool result = await DisplayAlert("Confirmar", "¿Estás seguro?", "Sí", "No");
```

#### Bootstrap

Bootstrap **no** tiene API programática para modales — son HTML estructural.
Para algo similar a `DisplayAlert`, podés usar el `confirm()` de JavaScript (poco elegante)
o un componente Modal controlado con estado:

```razor
@if (_show)
{
    <div class="modal fade show d-block" tabindex="-1"
         style="background-color: rgba(0,0,0,0.5);">
        <div class="modal-dialog">
            <div class="modal-content">
                <div class="modal-header">
                    <h5 class="modal-title">Confirmar</h5>
                </div>
                <div class="modal-body">
                    ¿Estás seguro?
                </div>
                <div class="modal-footer">
                    <button class="btn btn-secondary" @onclick="@(() => Cerrar(false))">No</button>
                    <button class="btn btn-primary" @onclick="@(() => Cerrar(true))">Sí</button>
                </div>
            </div>
        </div>
    </div>
}

@code {
    private bool _show;
    private TaskCompletionSource<bool>? _tcs;

    public Task<bool> Mostrar()
    {
        _tcs = new TaskCompletionSource<bool>();
        _show = true;
        StateHasChanged();
        return _tcs.Task;
    }

    private void Cerrar(bool result)
    {
        _show = false;
        _tcs?.SetResult(result);
    }
}
```

> 💡 Por la verbosidad, lo común es **encapsular esto en un servicio reutilizable** o usar una librería como Blazored.Modal.

#### MudBlazor

Mucho más simple gracias al `IDialogService`:

```razor
@inject IDialogService DialogService

<MudButton OnClick="ConfirmarAccion">Borrar</MudButton>

@code {
    private async Task ConfirmarAccion()
    {
        bool? result = await DialogService.ShowMessageBox(
            "Confirmar",
            "¿Estás seguro de borrar este registro?",
            yesText: "Sí",
            noText: "No"
        );

        if (result == true)
        {
            // borrar
        }
    }
}
```

### 8.2 Modales personalizados (con contenido custom)

#### MAUI
```csharp
await Navigation.PushModalAsync(new MiPaginaModal());
```

#### Bootstrap
Mismo Modal de Bootstrap que arriba, pero con tu contenido custom adentro.

#### MudBlazor

Crear un componente para el contenido del diálogo:

**`Components/Dialogs/MiDialog.razor`:**
```razor
<MudDialog>
    <DialogContent>
        <MudText>@Mensaje</MudText>
        <MudTextField @bind-Value="_respuesta" Label="Tu respuesta" />
    </DialogContent>
    <DialogActions>
        <MudButton OnClick="Cancelar">Cancelar</MudButton>
        <MudButton Color="Color.Primary" OnClick="Aceptar">Aceptar</MudButton>
    </DialogActions>
</MudDialog>

@code {
    [CascadingParameter] IMudDialogInstance MudDialog { get; set; } = default!;
    [Parameter] public string Mensaje { get; set; } = "";

    private string _respuesta = "";

    private void Aceptar() => MudDialog.Close(DialogResult.Ok(_respuesta));
    private void Cancelar() => MudDialog.Cancel();
}
```

**Mostrar el diálogo desde donde sea:**
```razor
@inject IDialogService DialogService

@code {
    private async Task AbrirDialog()
    {
        var parameters = new DialogParameters
        {
            { nameof(MiDialog.Mensaje), "Ingresá tu respuesta" }
        };

        var dialog = await DialogService.ShowAsync<MiDialog>("Encuesta", parameters);
        var result = await dialog.Result;

        if (result is not null && !result.Canceled)
        {
            string respuesta = (string)result.Data!;
            // usar respuesta
        }
    }
}
```

### 8.3 ActionSheet (menú de acciones)

#### MAUI
```csharp
string action = await DisplayActionSheet(
    "Elegir acción", "Cancelar", null,
    "Editar", "Borrar", "Compartir");
```

#### Bootstrap
Bootstrap Offcanvas o Dropdown personalizado. No hay equivalente directo idiomático.

#### MudBlazor
```razor
<MudMenu Label="Acciones" Variant="Variant.Filled">
    <MudMenuItem OnClick="Editar">Editar</MudMenuItem>
    <MudMenuItem OnClick="Borrar">Borrar</MudMenuItem>
    <MudMenuItem OnClick="Compartir">Compartir</MudMenuItem>
</MudMenu>
```

### 8.4 Toasts / Snackbars

#### MAUI
Necesita el [Community Toolkit](https://github.com/CommunityToolkit/Maui):
```csharp
await Toast.Make("Guardado").Show();
```

#### Bootstrap
Toasts estructurales, requieren JS para mostrar/ocultar:
```html
<div class="toast" role="alert">
    <div class="toast-body">Guardado</div>
</div>
```

#### MudBlazor
Limpio y declarativo via `ISnackbar`:
```razor
@inject ISnackbar Snackbar

@code {
    private void Notificar()
    {
        Snackbar.Add("Guardado correctamente", Severity.Success);
    }
}
```

### 8.5 Tabla resumen de feedback

| Necesidad | MAUI | Bootstrap | MudBlazor |
|---|---|---|---|
| Alerta simple | `DisplayAlert` | Modal manual | `IDialogService.ShowMessageBox` |
| Modal custom | `PushModalAsync` | Modal estructural + estado | `IDialogService.ShowAsync<T>` |
| ActionSheet | `DisplayActionSheet` | Offcanvas / Dropdown | `MudMenu` |
| Toast | Community Toolkit | Toast estructural + JS | `ISnackbar.Add` |
| Loading global | `IsBusy` + `ActivityIndicator` | Spinner manual | `MudOverlay` + `MudProgressCircular` |

---

## 9. Temas y estilos

### MAUI: ResourceDictionary
```xml
<!-- App.xaml -->
<Application.Resources>
    <ResourceDictionary>
        <Color x:Key="PrimaryColor">#5C2D91</Color>
        <Style TargetType="Button">
            <Setter Property="BackgroundColor" Value="{StaticResource PrimaryColor}" />
        </Style>
    </ResourceDictionary>
</Application.Resources>
```

### Bootstrap: variables CSS y clases
```css
/* wwwroot/css/app.css */
:root {
    --bs-primary: #5C2D91;
}

.btn-primary {
    --bs-btn-bg: #5C2D91;
    --bs-btn-border-color: #5C2D91;
}
```

### MudBlazor: `MudTheme` (programático)
```razor
<!-- MainLayout.razor -->
<MudThemeProvider Theme="@_theme" IsDarkMode="_darkMode" />

@code {
    private bool _darkMode = false;

    private MudTheme _theme = new()
    {
        PaletteLight = new PaletteLight()
        {
            Primary = "#5C2D91",
            Secondary = Colors.Pink.Accent4,
        },
        PaletteDark = new PaletteDark()
        {
            Primary = "#9D6BD3",
        },
        Typography = new Typography()
        {
            Default = new DefaultTypography()
            {
                FontFamily = new[] { "Roboto", "sans-serif" }
            }
        }
    };
}
```

> Cambiar `_darkMode = true` y todos los componentes Mud cambian al toque, sin recargar.

---

## 10. Cuándo elegir cada stack

### Elegí MAUI puro (XAML) si:
- Tu app es **fuertemente nativa** (cámara, sensores, gestos, performance pico).
- Tu equipo ya domina XAML / WPF.
- Querés acceso directo a toda la API de MAUI sin capas extra.
- No necesitás reutilizar componentes web.

### Elegí MAUI Blazor Hybrid + Bootstrap si:
- Querés **reutilizar UI web** que ya tenés en otro proyecto.
- Querés control fino sobre HTML/CSS.
- Tu app tiene mucho contenido informativo (estilo landing).
- Tu equipo viene de web con mentalidad CSS-first.

### Elegí MAUI Blazor Hybrid + MudBlazor si:
- Tu app es tipo **dashboard, CRUD, herramienta interna**.
- Querés **componentes ricos out of the box** (DataGrid, formularios con validación, DatePicker).
- Querés **dark mode y theming sin pelearte con CSS**.
- Querés trabajar 100% en C# sin escribir JS.

### Resumen rápido

| Criterio | MAUI puro | Hybrid + Bootstrap | Hybrid + MudBlazor |
|---|---|---|---|
| Look "nativo" | ★★★ | ★ | ★★ |
| Velocidad de desarrollo CRUD | ★ | ★★ | ★★★ |
| Acceso a hardware | ★★★ | ★★ | ★★ |
| Reutilización con app web | ★ | ★★★ | ★★★ |
| Curva (viniendo de XAML) | suave | media | media |
| Ecosistema de componentes | medio | grande | grande |
| Theming / dark mode | manual | manual | trivial |
| Tamaño de la app | menor | medio | mayor |

---

## 11. Apéndice: snippets útiles

### Checklist para inicializar MudBlazor desde cero
1. `dotnet add package MudBlazor`
2. En `MauiProgram.cs`: `builder.Services.AddMudServices();`
3. En `_Imports.razor`: `@using MudBlazor`
4. En `wwwroot/index.html`: agregar refs a `MudBlazor.min.css` y `MudBlazor.min.js`
5. En `MainLayout.razor`: agregar los 4 providers (`MudThemeProvider`, `MudPopoverProvider`, `MudDialogProvider`, `MudSnackbarProvider`)

### Estructura mínima de `MainLayout` con MudBlazor
```razor
@inherits LayoutComponentBase

<MudThemeProvider />
<MudPopoverProvider />
<MudDialogProvider />
<MudSnackbarProvider />

<MudLayout>
    <MudAppBar>
        <MudText Typo="Typo.h6">Mi App</MudText>
    </MudAppBar>
    <MudMainContent>
        <MudContainer Class="my-4">
            @Body
        </MudContainer>
    </MudMainContent>
</MudLayout>
```

### Asegurar que el body llene la pantalla (necesario para CSS Grid con `1fr`)
En `wwwroot/css/app.css`:
```css
html, body {
    height: 100%;
    margin: 0;
}

#app, .page, main {
    height: 100%;
}
```

### Diagnóstico de problemas comunes con CSS Grid

| Síntoma | Causa probable | Solución |
|---|---|---|
| Las celdas se solapan o "encimadas" | Filas con `1fr` colapsan a 0 porque el padre no tiene altura | Usar alturas fijas (`grid-template-rows: 150px 150px 150px`) |
| El grid no llena la pantalla | El `BlazorWebView` no propaga `100%` | Setear `height: 100%` en `html, body, #app, main` |
| Tamaños raros con padding | Box-sizing inconsistente | Agregar `* { box-sizing: border-box; }` |

### Mapeo de espaciado entre stacks

| Concepto | Bootstrap | MudBlazor |
|---|---|---|
| Padding general | `p-N` (N: 0–5) | `pa-N` (N: 0–16) |
| Padding horizontal | `px-N` | `px-N` |
| Padding vertical | `py-N` | `py-N` |
| Margin general | `m-N` | `ma-N` |
| Gap (en flex/grid) | `gap-N` | usar `Spacing` o `style` |

> Cada paso en Bootstrap es ~`0.25rem * N`. En MudBlazor cada paso es 4px (hasta el 16, que son 64px). **No son intercambiables**: `p-2` de Bootstrap (8px) ≠ `pa-2` de MudBlazor (8px) por casualidad pero conceptualmente son escalas distintas.

### Combinar MudBlazor y Bootstrap en el mismo proyecto

Es técnicamente posible pero **no recomendable**:
- Las clases utilitarias chocan (`p-2` significa cosas distintas en cada uno).
- Los componentes pueden tener conflictos de z-index.
- Aumenta el tamaño del bundle.
- Confunde al equipo: "¿uso `MudButton` o `<button class="btn">`?"

Si necesitás algunos componentes específicos de uno mientras usás el otro, mejor extraer ese componente y replicarlo a mano con el sistema dominante.

---

*Documento de referencia generado a partir de una conversación de aprendizaje
sobre MAUI Blazor Hybrid.*
