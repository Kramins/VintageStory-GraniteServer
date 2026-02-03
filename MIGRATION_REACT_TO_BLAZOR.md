# React to Blazor Web App Migration Guide

**Status**: Phase 1 In Progress - Foundation Complete  
**Last Updated**: January 28, 2026  
**Target**: Complete migration from React/Redux/MUI to Blazor/Fluxor/MudBlazor

---

## Table of Contents

1. [Overview](#overview)
2. [Current Architecture](#current-architecture)
3. [Target Architecture](#target-architecture)
4. [Technology Mapping](#technology-mapping)
5. [Migration Strategy](#migration-strategy)
6. [Implementation Roadmap](#implementation-roadmap)
7. [Detailed Migration Steps](#detailed-migration-steps)
8. [Component Mapping Reference](#component-mapping-reference)
9. [Progress Tracking](#progress-tracking)

---

## Overview

### Goal
Migrate the existing Granite Server ClientApp from a React/TypeScript/Redux/MUI stack to a Blazor Web App (Server-side Rendering) with Fluxor state management and MudBlazor UI components.

### Rationale
- **Technology Consolidation**: Move to .NET-based frontend to align with backend (C#)
- **Unified Development**: Single language (.NET/C#) across backend and frontend
- **Improved Type Safety**: Leverage C# type system instead of TypeScript
- **Rich Component Library**: MudBlazor provides Material Design components similar to MUI
- **Unified State Management**: Fluxor provides Redux-like state management in C#

### Timeline
Phased approach - not a single cutover. Multiple features will be migrated in parallel to maintain project stability.

> **Note**: These phases and implementation steps are designed for **AI agents to work on with human in the loop**. Each task is structured to be actionable and verifiable, allowing AI to handle implementation while developers review and provide direction at key checkpoints.

---

## Current Architecture

### Technology Stack
| Component | Technology | Version |
|-----------|-----------|---------|
| **Framework** | React | 19.2.0 |
| **Language** | TypeScript | 5.9.3 |
| **Build Tool** | Vite | 7.2.4 |
| **State Management** | Redux Toolkit | 2.11.1 |
| **UI Components** | Material-UI (MUI) | 7.3.6 |
| **API Communication** | Axios | 1.13.2 |
| **Real-time Communication** | SignalR | 10.0.0 |
| **Routing** | React Router | 7.10.1 |
| **Icons** | MUI Icons | 7.3.6 |
| **Data Grid** | MUI X Data Grid | 8.22.0 |

### Directory Structure
```
Granite.Server/ClientApp/
├── src/
│   ├── components/       # Reusable React components
│   ├── pages/           # Page-level components
│   ├── layouts/         # Layout components
│   ├── store/           # Redux store configuration & slices
│   ├── hooks/           # Custom React hooks
│   ├── services/        # API & business logic services
│   ├── types/           # TypeScript type definitions
│   ├── theme/           # MUI theme configuration
│   ├── assets/          # Static assets
│   ├── routes.tsx       # Route definitions
│   ├── App.tsx          # Main App component
│   └── main.tsx         # Entry point
├── public/              # Static files
├── dist/                # Build output
├── package.json         # Dependencies
├── vite.config.ts       # Vite configuration
└── tsconfig.json        # TypeScript configuration
```

### Key Features Currently Implemented
- ✅ Player management (list, details, bans, kicks)
- ✅ Mod management (list, install, uninstall)
- ✅ Server status monitoring
- ✅ World management
- ✅ Authentication (login, token management)
- ✅ Real-time updates via SignalR
- ✅ Responsive Material Design UI
- ✅ Theme system (light/dark mode)

---

## Target Architecture

### Technology Stack
| Component | Technology | Version |
|-----------|-----------|---------|
| **Framework** | Blazor Web App (Client-side) | .NET 9 |
| **Language** | C# | 13 |
| **Rendering** | WebAssembly (Client-side) |  |
| **State Management** | Fluxor | Latest |
| **UI Components** | MudBlazor | Latest |
| **API Communication** | HttpClient | Built-in |
| **Real-time Communication** | SignalR | 10.0.0+ |
| **Routing** | Blazor Router |  |
| **Icons** | MudBlazor Icons | Built-in |
| **Data Grid** | MudDataGrid |  |

### Planned Directory Structure
```
Granite.Server/
├── Granite.Web/              # New Blazor Web App project
│   ├── Components/
│   │   ├── Pages/           # Page components
│   │   ├── Layout/          # Layout components
│   │   ├── Features/        # Feature-specific components
│   │   │   ├── Players/
│   │   │   ├── Mods/
│   │   │   ├── Server/
│   │   │   ├── World/
│   │   │   └── Auth/
│   │   └── Shared/          # Shared/reusable components
│   ├── Services/
│   │   ├── API/             # API service clients
│   │   ├── StateManagement/ # Fluxor actions, reducers, effects
│   │   ├── SignalR/         # SignalR hub connections
│   │   └── Theme/           # Theme management
│   ├── Models/              # C# models/DTOs
│   ├── Helpers/             # Utility functions
│   ├── App.razor            # Root component
│   ├── Program.cs           # DI & startup configuration
│   └── appsettings.json     # Configuration
└── ClientApp/               # Legacy React app (to be removed)
```

---

## Technology Mapping

### Framework & Templating

| React Concept | Blazor Equivalent |
|---------------|-------------------|
| React Component (`.tsx`) | Razor Component (`.razor`) |
| JSX | Razor Syntax (`@code`, `@if`, `@foreach`) |
| Props | Component Parameters (`[Parameter]`) |
| State (useState) | Component State (fields/properties) |
| Context API | Cascading Parameters or Fluxor |
| useEffect | `OnInitializedAsync()`, `OnParametersSetAsync()` |
| Custom Hooks | Custom Services or Cascading Components |

**Example: Component Migration**

React:
```tsx
interface PlayerListProps {
  players: Player[];
  onSelect: (player: Player) => void;
}

export const PlayerList: React.FC<PlayerListProps> = ({ players, onSelect }) => {
  const [loading, setLoading] = useState(false);
  
  useEffect(() => {
    // Load players
  }, []);
  
  return (
    <div>
      {players.map(p => (
        <div key={p.id} onClick={() => onSelect(p)}>
          {p.name}
        </div>
      ))}
    </div>
  );
};
```

Blazor:
```razor
@page "/players"
@inject PlayerService PlayerService

<div>
  @foreach (var player in Players)
  {
    <div @onclick="() => OnSelect(player)">
      @player.Name
    </div>
  }
</div>

@code {
  private List<Player> Players { get; set; } = [];
  
  [Parameter]
  public EventCallback<Player> OnSelect { get; set; }
  
  protected override async Task OnInitializedAsync()
  {
    Players = await PlayerService.GetPlayersAsync();
  }
}
```

### State Management

| Redux Concept | Fluxor Equivalent |
|--------------|-------------------|
| Action | `IAction` interface |
| Reducer | `Reducer` class |
| Selector | `Feature` state + properties |
| Slice | `Feature` class |
| Store | `IStore` (injected) |
| useDispatch | `IDispatcher` (injected) |
| useSelector | `IState<T>` (subscription) |
| Thunk (async) | `Effect` class |

**Example: Redux to Fluxor**

Redux:
```typescript
// Action
export const fetchPlayers = createAsyncThunk(
  'players/fetchPlayers',
  async (_, { rejectWithValue }) => {
    const response = await api.get('/api/players');
    return response.data;
  }
);

// Slice
const playersSlice = createSlice({
  name: 'players',
  initialState: { items: [], loading: false },
  extraReducers: (builder) => {
    builder.addCase(fetchPlayers.fulfilled, (state, action) => {
      state.items = action.payload;
      state.loading = false;
    });
  }
});
```

Fluxor:
```csharp
// Action
public record FetchPlayersAction();

// State
public record PlayersState
{
    public List<PlayerDto> Items { get; init; } = [];
    public bool Loading { get; init; } = false;
}

// Reducer
[ReducerMethod]
public static PlayersState OnFetchPlayers(PlayersState state, FetchPlayersAction action)
{
    return state with { Loading = true };
}

// Effect (async handler)
[EffectMethod]
public async Task HandleFetchPlayers(FetchPlayersAction action, IDispatcher dispatcher)
{
    var players = await _playerService.GetPlayersAsync();
    dispatcher.Dispatch(new FetchPlayersSuccessAction(players));
}
```

### UI Components

| MUI Component | MudBlazor Equivalent |
|---------------|----------------------|
| `<Button>` | `<MudButton>` |
| `<TextField>` | `<MudTextField>` |
| `<Select>` | `<MudSelect>` |
| `<DataGrid>` | `<MudDataGrid>` |
| `<Dialog>` | `<MudDialog>` |
| `<AppBar>` | `<MudAppBar>` |
| `<Drawer>` | `<MudDrawer>` |
| `<Card>` | `<MudCard>` |
| `<Snackbar>` | `<MudSnackBar>` |
| `<Progress>` | `<MudProgressLinear>` |
| `<Table>` | `<MudTable>` or `<MudDataGrid>` |
| `<Typography>` | `<MudText>` |
| `<Container>` | `<MudContainer>` |
| `<Grid>` | `<MudGrid>` |
| `<Paper>` | `<MudPaper>` |

### Routing

| React Router | Blazor Router |
|-------------|---------------|
| `<BrowserRouter>` | Built into Blazor |
| `<Routes>` | `<Routes>` in `App.razor` |
| `<Route>` | `@page` directive |
| `useNavigate()` | `NavigationManager` service |
| `useParams()` | Route parameters in component |
| `Link` | `<a href="...">` or NavLink |
| Dynamic route: `/players/:id` | Dynamic route: `/players/@id` |

### HTTP & Real-time Communication

| React/Axios | Blazor |
|------------|--------|
| Axios instance | `HttpClient` (injected) |
| Interceptors | Delegating handlers in DI |
| SignalR Client | `HubConnectionBuilder` |
| Event handlers | Delegate subscriptions |
| TypeScript errors | C# compilation errors |

---

## Migration Strategy

### Phased Approach

Instead of a complete rewrite, we'll migrate in phases to maintain stability:

#### Phase 1: Foundation (Weeks 1-2) ✅ **COMPLETE**
- [x] Create new Blazor Web App project
- [x] Set up Fluxor state management
- [x] Configure MudBlazor
- [x] Create API service clients
- [x] Set up SignalR connection
- [x] Create base layouts and navigation

#### Phase 2: Core Features (Weeks 3-4) - IN PROGRESS
- [ ] Migrate authentication pages
- [ ] Migrate player management features
- [ ] Migrate server status page
- [ ] Migrate mod management

#### Phase 3: Secondary Features (Weeks 5-6)
- [ ] Migrate world management
- [ ] Migrate remaining pages
- [ ] Migrate dialogs and modals

#### Phase 4: Polish & Testing (Weeks 7-8)
- [ ] Theme system implementation
- [ ] Responsive design verification
- [ ] Performance optimization
- [ ] User acceptance testing
- [ ] Bug fixes

#### Phase 5: Cleanup & Deployment (Week 9)
- [ ] Remove React dependencies
- [ ] Update build process
- [ ] Deploy and monitor
- [ ] Remove legacy ClientApp directory

### Parallel Development Strategy

- **Keep React app running** during migration
- **Create new Blazor project** alongside existing app
- **New routes/features** in Blazor
- **Gradual cutover** of pages
- **Final cleanup** after verification

### Testing Strategy

Unit tests should be written **alongside implementation**, not after. Each feature must include:

1. **Unit Tests**: C# unit tests for services and state management (xUnit/Moq)
   - Service method tests with mocked dependencies
   - Fluxor reducer and action tests
   - Effect handler tests
   - Utility function tests

2. **Integration Tests**: Test API interactions
   - API client integration tests
   - SignalR connection and message handling tests
   - State management integration tests

3. **Component Tests**: Validate Razor component behavior
   - Component parameter tests
   - Event handler tests
   - Rendering logic tests

4. **UI Testing**: Manual testing with MudBlazor components
   - User interaction flows
   - Responsive design
   - Accessibility compliance

5. **Performance Testing**: Compare React vs Blazor rendering
   - WebAssembly bundle size
   - Initial load times
   - Component render performance

---

## Implementation Roadmap

### Week 1-2: Foundation Setup ✅ **COMPLETE**

```
[x] Create Blazor Web App project
    [x] New solution structure (Granite.Web.Client)
    [x] Project file configuration (.NET 9, WebAssembly)
    [x] Package references for Fluxor, MudBlazor, SignalR
    [x] Add xUnit and Moq test project (Granite.Web.Tests)
    
[x] Configure Fluxor
    [x] Install Fluxor packages
    [x] Create state feature classes (Players, Mods, Server, Auth, World)
    [x] Set up root reducer
    [x] Configure DI in Program.cs
    [x] Unit tests for initial state
    [x] Unit tests for reducers (14 tests passing)
    
[x] Set up MudBlazor
    [x] Install MudBlazor
    [x] Configure layout with MudThemeProvider
    [x] Import CSS and JavaScript
    [x] Configure theme provider with dark mode support
    [x] Fixed MudPopoverProvider placement
    
[x] API Service Layer
    [x] Create HttpClient configuration with BaseApiClient
    [x] API service clients (Players, Mods, Server, Auth, World)
    [x] Error handling & response mapping (JsonApiDocument<T>)
    [x] Add to DI container
    [x] Unit tests for all service methods (42 tests passing)
    [x] Integration tests for API client error handling
    
[x] SignalR Integration
    [x] HubConnection setup with auto-reconnection
    [x] Event handlers (ReceiveEvent)
    [x] Auto-reconnection logic with exponential backoff
    [x] State dispatch on events
    [x] Unit tests for SignalR service (12 tests passing)
    [x] Connection state notifications
    [x] Integration with MainLayout (connection indicator badge)
    
[x] Core Components
    [x] App.razor (root)
    [x] MainLayout.razor with MudAppBar, MudDrawer, MudMainContent
    [x] NavMenu component with organized sections
    [x] Theme provider setup with dark mode toggle
    [x] Fixed AppBar alignment with MudSpacer
    [x] Created placeholder pages for all routes (13 pages)
    [x] Client-side routing working properly
    
**Test Results: 54/54 tests passing**
- 42 API client tests
- 12 SignalR service tests

**Build Status: ✅ Succeeded**
```

### Week 3-4: Core Features - READY TO START

```
[ ] Authentication
    [ ] LoginPage.razor
    [ ] Auth service
    [ ] AuthFlux (state management)
    [ ] Token storage
    [ ] Protected routes
    [ ] Unit tests for auth service methods
    [ ] Unit tests for auth reducers and actions
    [ ] Component tests for LoginPage
    [ ] Integration tests for token refresh flow
    
[ ] Player Management
    [ ] PlayersPage.razor (list)
    [ ] PlayerDetailsPage.razor
    [ ] PlayerDialog component
    [ ] BanPlayer component
    [ ] KickPlayer component
    [ ] PlayerFlux (state)
    [ ] Unit tests for PlayerService methods
    [ ] Unit tests for player reducers
    [ ] Unit tests for player effects
    [ ] Component tests for all pages and dialogs
    [ ] Integration tests for player API calls
    
[ ] Server Status
    [ ] ServerStatusPage.razor
    [ ] Health monitoring
    [ ] Real-time updates via SignalR
    [ ] Unit tests for server service
    [ ] Unit tests for status effects and state
    [ ] Component tests for status page
    
[ ] Mod Management
    [ ] ModsPage.razor (list)
    [ ] ModDetailsPage.razor
    [ ] InstallMod dialog
    [ ] ModFlux (state)
    [ ] Unit tests for ModService
    [ ] Unit tests for mod reducers and effects
    [ ] Component tests for mod pages
```

### Week 5-6: Secondary Features

```
[ ] World Management
    [ ] WorldPage.razor
    [ ] World settings
    [ ] WorldFlux (state)
    [ ] Unit tests for WorldService
    [ ] Unit tests for world state and effects
    [ ] Component tests for world pages
    
[ ] Inventory & Items
    [ ] Migrate item management
    [ ] Inventory display
    [ ] Unit tests for inventory service
    [ ] Component tests for inventory pages
    
[ ] Settings & Configuration
    [ ] Server settings page
    [ ] Auth settings
    [ ] Unit tests for settings service
    [ ] Component tests for settings pages
    
[ ] Remaining Pages
    [ ] Any other pages not covered above
    [ ] Error pages
    [ ] 404 handling
    [ ] Unit tests for all remaining services
    [ ] Component tests for all pages
```

### Week 7-8: Polish & Testing

```
[ ] Test Coverage Analysis
    [ ] Run code coverage reports
    [ ] Target 80%+ unit test coverage
    [ ] Identify gaps and add missing tests
    [ ] Update test documentation
    
[ ] Theme System
    [ ] Light/dark mode toggle
    [ ] Theme persistence
    [ ] Color scheme configuration
    [ ] Unit tests for theme service
    [ ] Component tests for theme toggle
    
[ ] Responsive Design
    [ ] Test on mobile
    [ ] Test on tablet
    [ ] Test on desktop
    [ ] Breakpoint verification
    [ ] Manual component testing
    
[ ] Performance
    [ ] Bundle size analysis
    [ ] Load time optimization
    [ ] Memory usage review
    [ ] Rendering performance tests
    [ ] Performance regression tests
    
[ ] Final Testing
    [ ] Component testing verification
    [ ] E2E testing (manual)
    [ ] User acceptance testing
    [ ] Cross-browser compatibility testing
```

### Week 9: Cleanup & Deployment

```
[ ] Finalization
    [ ] Remove React app
    [ ] Update build scripts
    [ ] Update Docker configuration
    [ ] Final integration testing
    
[ ] Deployment
    [ ] Deploy to staging
    [ ] Performance monitoring
    [ ] User monitoring
    [ ] Bug fixes from production
```

---

## Detailed Migration Steps

### Step 1: Create New Blazor Web App Project

```bash
# Create new Blazor project
dotnet new blazor -n Granite.Web -o Granite.Server/../Granite.Web

# Create test project
dotnet new xunit -n Granite.Web.Tests -o Granite.Server/../Granite.Web.Tests

# Add NuGet packages to Granite.Web
cd Granite.Web
dotnet add package Fluxor
dotnet add package Fluxor.Blazor.Web
dotnet add package MudBlazor
dotnet add package Microsoft.AspNetCore.SignalRClient

# Add test packages to Granite.Web.Tests
cd ../Granite.Web.Tests
dotnet add package xunit
dotnet add package xunit.runner.visualstudio
dotnet add package Moq
dotnet add package Microsoft.NET.Test.Sdk
dotnet add reference ../Granite.Web/Granite.Web.csproj
```

### Step 2: Configure Program.cs (DI & Services)

Key configurations needed:
```csharp
// Add Fluxor
builder.Services.AddFluxor(options =>
{
    options.ScanAssemblies(typeof(Program).Assembly);
});

// Add MudBlazor
builder.Services.AddMudServices();

// Add API clients
builder.Services.AddScoped<IPlayerService, PlayerService>();
builder.Services.AddScoped<IModService, ModService>();
builder.Services.AddScoped<IServerService, ServerService>();

// Add HttpClient
builder.Services.AddScoped(sp => 
    new HttpClient { BaseAddress = new Uri("https://localhost:5001") });
```

### Step 3: Create State Management (Fluxor)

**Structure:**
```
Services/
  StateManagement/
    Features/
      Players/
        State.cs
        Reducer.cs
        Effects.cs
        Actions.cs
      Mods/
        [same structure]
      Auth/
        [same structure]
      Server/
        [same structure]
```

**Example - Players Feature:**
```csharp
// PlayerState.cs
public record PlayerState
{
    public ImmutableList<PlayerDto> Players { get; init; } = [];
    public bool Loading { get; init; } = false;
    public string? Error { get; init; } = null;
}

// FetchPlayersAction.cs
public record FetchPlayersAction;

// FetchPlayersSuccessAction.cs
public record FetchPlayersSuccessAction(List<PlayerDto> Players);

// PlayerReducer.cs
[ReducerMethod]
public static PlayerState OnFetchPlayers(PlayerState state, FetchPlayersAction _)
    => state with { Loading = true, Error = null };

[ReducerMethod]
public static PlayerState OnFetchPlayersSuccess(PlayerState state, FetchPlayersSuccessAction action)
    => state with { Players = action.Players.ToImmutableList(), Loading = false, Error = null };

// PlayerEffect.cs
[EffectMethod]
public async Task HandleFetchPlayers(FetchPlayersAction _, IDispatcher dispatcher)
{
    try
    {
        var players = await _playerService.GetPlayersAsync();
        dispatcher.Dispatch(new FetchPlayersSuccessAction(players));
    }
    catch (Exception ex)
    {
        dispatcher.Dispatch(new FetchPlayersFailureAction(ex.Message));
    }
}

// PlayerReducer.Tests.cs - UNIT TESTS
public class PlayerReducerTests
{
    [Fact]
    public void OnFetchPlayers_ShouldSetLoading()
    {
        // Arrange
        var state = new PlayerState();
        var action = new FetchPlayersAction();

        // Act
        var result = PlayerReducer.OnFetchPlayers(state, action);

        // Assert
        Assert.True(result.Loading);
        Assert.Null(result.Error);
    }

    [Fact]
    public void OnFetchPlayersSuccess_ShouldUpdatePlayers()
    {
        // Arrange
        var state = new PlayerState { Loading = true };
        var players = new List<PlayerDto> { new("1", "Player1") };
        var action = new FetchPlayersSuccessAction(players);

        // Act
        var result = PlayerReducer.OnFetchPlayersSuccess(state, action);

        // Assert
        Assert.False(result.Loading);
        Assert.Equal(1, result.Players.Count);
        Assert.Equal("Player1", result.Players[0].Name);
    }
}

// PlayerService.Tests.cs - SERVICE UNIT TESTS
public class PlayerServiceTests
{
    [Fact]
    public async Task GetPlayersAsync_ReturnsPlayers()
    {
        // Arrange
        var mockHttp = new Mock<HttpMessageHandler>();
        var httpClient = new HttpClient(mockHttp.Object);
        var service = new PlayerService(httpClient);
        
        var expectedPlayers = new List<PlayerDto> 
        { 
            new("1", "Player1")
        };
        var json = JsonSerializer.Serialize(expectedPlayers);
        
        mockHttp
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent(json)
            });

        // Act
        var result = await service.GetPlayersAsync();

        // Assert
        Assert.NotNull(result);
        Assert.Equal(1, result.Count);
        Assert.Equal("Player1", result[0].Name);
    }

    [Fact]
    public async Task GetPlayersAsync_ThrowsOnError()
    {
        // Arrange
        var mockHttp = new Mock<HttpMessageHandler>();
        var httpClient = new HttpClient(mockHttp.Object);
        var service = new PlayerService(httpClient);
        
        mockHttp
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.InternalServerError
            });

        // Act & Assert
        await Assert.ThrowsAsync<HttpRequestException>(() => service.GetPlayersAsync());
    }
}
```

### Step 4: Create API Service Clients

```csharp
public interface IPlayerService
{
    Task<List<PlayerDto>> GetPlayersAsync();
    Task<PlayerDto> GetPlayerAsync(string playerId);
    Task BanPlayerAsync(string playerId, BanRequestDto request);
    Task KickPlayerAsync(string playerId, KickRequestDto request);
}

public class PlayerService : IPlayerService
{
    private readonly HttpClient _http;
    
    public PlayerService(HttpClient http) => _http = http;
    
    public async Task<List<PlayerDto>> GetPlayersAsync()
    {
        var response = await _http.GetAsync("/api/players");
        // Map response to dto
        return response.Content.ReadAsAsync<List<PlayerDto>>();
    }
    
    // ... other methods
}
```

### Step 5: Create Base Components

**App.razor:**
```razor
@implements IAsyncDisposable
@inject NavigationManager Navigation
@inject IDispatcher Dispatcher

<MudThemeProvider>
    <MudDialogProvider/>
    <MudSnackbarProvider/>
    <MudLayout>
        <MainLayout/>
    </MudLayout>
</MudThemeProvider>

@code {
    protected override async Task OnInitializedAsync()
    {
        await base.OnInitializedAsync();
        Dispatcher.Dispatch(new InitializeAppAction());
    }
    
    async ValueTask IAsyncDisposable.DisposeAsync()
    {
        // Cleanup
    }
}
```

**MainLayout.razor:**
```razor
@inherits LayoutComponentBase
@inject IState<NavigationState> NavState
@inject NavigationManager Navigation

<MudAppBar>
    <MudIconButton Icon="@Icons.Material.Filled.Menu" />
    <MudSpacer />
    <MudText Typo="Typo.H6">Granite Server</MudText>
</MudAppBar>

<MudDrawer>
    <NavMenu />
</MudDrawer>

<MudMainContent>
    @Body
</MudMainContent>
```

### Step 6: Migrate Pages

For each page in React, create corresponding `.razor` component:

**Example: PlayersPage.razor**
```razor
@page "/players"
@inject IState<PlayerState> PlayerState
@inject IDispatcher Dispatcher
@inject NavigationManager Nav

<MudContainer MaxWidth="MaxWidth.Large" Class="py-8">
    <MudText Typo="Typo.H3" GutterBottom="true">Players</MudText>
    
    @if (PlayerState.Value.Loading)
    {
        <MudProgressLinear Indeterminate="true" />
    }
    else if (PlayerState.Value.Players.Count > 0)
    {
        <MudDataGrid Items="PlayerState.Value.Players"
                     Hover="true"
                     @onclick="(rowClick) => RowClicked((PlayerDto)rowClick.Item)">
            <Columns>
                <PropertyColumn Property="x => x.Name" Title="Name" />
                <PropertyColumn Property="x => x.LastSeen" Title="Last Seen" />
                <TemplateColumn CellClass="d-flex gap-2">
                    <MudButton Size="Size.Small" Color="Color.Primary" 
                               @onclick="() => ViewPlayer(context)">
                        View
                    </MudButton>
                </TemplateColumn>
            </Columns>
        </MudDataGrid>
    }
    else
    {
        <MudText>No players found</MudText>
    }
</MudContainer>

@code {
    protected override async Task OnInitializedAsync()
    {
        Dispatcher.Dispatch(new FetchPlayersAction());
    }
    
    private void RowClicked(PlayerDto player)
    {
        Nav.NavigateTo($"/players/{player.Id}");
    }
    
    private void ViewPlayer(PlayerDto player)
    {
        Nav.NavigateTo($"/players/{player.Id}");
    }
}
```

### Step 7: Set up Real-time Communication (SignalR)

**SignalRService.cs:**
```csharp
public class SignalRService : IAsyncDisposable
{
    private HubConnection? _hubConnection;
    
    public async Task StartAsync(IDispatcher dispatcher)
    {
        _hubConnection = new HubConnectionBuilder()
            .WithUrl("https://localhost:5001/graniteHub")
            .WithAutomaticReconnect()
            .Build();
        
        _hubConnection.On<PlayerDto>("PlayerUpdated", 
            player => dispatcher.Dispatch(new PlayerUpdatedAction(player)));
        
        await _hubConnection.StartAsync();
    }
    
    public async ValueTask DisposeAsync()
    {
        if (_hubConnection is not null)
            await _hubConnection.DisposeAsync();
    }
}
```

### Step 8: Configure Theme System

**ThemeService.cs:**
```csharp
public class ThemeService
{
    public MudTheme GetDarkTheme()
    {
        return new MudTheme
        {
            Palette = new PaletteLight
            {
                Primary = "#1976d2",
                Secondary = "#f50057"
            }
        };
    }
}
```

---

## Component Mapping Reference

This table helps map React components to Blazor equivalents:

### Layout Components

| React Component | Blazor Component | Notes |
|-----------------|------------------|-------|
| `<div className="flex">` | `<MudStack Row="true">` | Use MudStack for flexbox |
| `<Grid container>` | `<MudGrid>` | Same grid system |
| `<Paper>` | `<MudPaper>` | Direct equivalent |
| `<AppBar>` | `<MudAppBar>` | Direct equivalent |
| `<Drawer>` | `<MudDrawer>` | Direct equivalent |
| `<Container>` | `<MudContainer>` | Direct equivalent |

### Form Components

| React Component | Blazor Component | Notes |
|-----------------|------------------|-------|
| `<TextField>` | `<MudTextField>` | Two-way binding: `@bind-Value` |
| `<Select>` | `<MudSelect>` | Use `T` for generic type |
| `<Checkbox>` | `<MudCheckBox>` | Two-way binding: `@bind-Checked` |
| `<Switch>` | `<MudSwitch>` | Toggle component |
| `<Radio>` | `<MudRadioGroup>` | Radio group |
| `<Autocomplete>` | `<MudAutocomplete>` | Autocomplete support |
| `<DatePicker>` | `<MudDatePicker>` | Date selection |
| `<TimePicker>` | `<MudTimePicker>` | Time selection |

### Display Components

| React Component | Blazor Component | Notes |
|-----------------|------------------|-------|
| `<Typography>` | `<MudText>` | Use `Typo` for variants |
| `<Table>` | `<MudTable>` or `<MudDataGrid>` | DataGrid recommended for complex tables |
| `<List>` | `<MudList>` | List display |
| `<Card>` | `<MudCard>` | Direct equivalent |
| `<Chip>` | `<MudChip>` | Tag/chip display |
| `<Avatar>` | `<MudAvatar>` | User avatars |
| `<Icon>` | `<MudIcon>` | Icon rendering |
| `<Progress>` | `<MudProgressLinear>` or `<MudProgressCircular>` | Progress indicators |

### Interaction Components

| React Component | Blazor Component | Notes |
|-----------------|------------------|-------|
| `<Button>` | `<MudButton>` | Direct equivalent |
| `<IconButton>` | `<MudIconButton>` | Icon button |
| `<Dialog>` | `<MudDialog>` | Modal dialogs |
| `<Snackbar>` | `<MudSnackBar>` | Toast notifications |
| `<Tooltip>` | `<MudTooltip>` | Hover tooltips |
| `<Menu>` | `<MudMenu>` | Dropdown menus |
| `<Popover>` | `<MudPopover>` | Popover content |

### Advanced Components

| React Component | Blazor Component | Notes |
|-----------------|------------------|-------|
| `<DataGrid>` | `<MudDataGrid>` | Full-featured data grid |
| `<Pagination>` | Built into `<MudDataGrid>` | DataGrid handles pagination |
| `<TreeView>` | `<MudTreeView>` | Hierarchical data |
| `<Stepper>` | `<MudStepper>` | Step-by-step forms |
| `<Tabs>` | `<MudTabs>` | Tab navigation |
| `<ExpansionPanel>` | `<MudExpansionPanels>` | Collapsible content |
| `<Accordion>` | `<MudExpansionPanels>` | Same as expansion panels |

---

## Progress Tracking

### Current Status: **Planning Phase**

Last Updated: January 27, 2026

### Completed
- [x] Define migration strategy
- [x] Document technology mapping
- [x] Create implementation roadmap
- [x] Document component mapping

### In Progress
- [ ] Create Blazor Web App project
- [ ] Set up Fluxor
- [ ] Configure MudBlazor

### Not Started
- [ ] Phase 1: Foundation
- [ ] Phase 2: Core Features
- [ ] Phase 3: Secondary Features
- [ ] Phase 4: Polish & Testing
- [ ] Phase 5: Cleanup & Deployment

### Metrics & Tracking

#### Build Progress

| Phase | Component | Status | Completion % | Test Coverage |
|-------|-----------|--------|-------------|---------------|
| 1 | Project Setup | Not Started | 0% | N/A |
| 1 | Fluxor Setup | Not Started | 0% | 0% |
| 1 | MudBlazor Setup | Not Started | 0% | 0% |
| 1 | API Services | Not Started | 0% | 0% |
| 1 | SignalR Integration | Not Started | 0% | 0% |
| 2 | Authentication | Not Started | 0% | 0% |
| 2 | Player Management | Not Started | 0% | 0% |
| 2 | Server Status | Not Started | 0% | 0% |
| 2 | Mod Management | Not Started | 0% | 0% |
| 3 | World Management | Not Started | 0% | 0% |
| 3 | Settings | Not Started | 0% | 0% |
| 4 | Polish & Testing | Not Started | 0% | Target 80%+ |
| 5 | Cleanup & Deployment | Not Started | 0% | N/A |

#### Feature Coverage

| Feature | React | Blazor | Status |
|---------|-------|--------|--------|
| Authentication | ✅ | ❌ | Pending |
| Player Management | ✅ | ❌ | Pending |
| Player Details | ✅ | ❌ | Pending |
| Ban/Kick Players | ✅ | ❌ | Pending |
| Mod Management | ✅ | ❌ | Pending |
| Server Status | ✅ | ❌ | Pending |
| Server Settings | ✅ | ❌ | Pending |
| World Management | ✅ | ❌ | Pending |
| Real-time Updates | ✅ | ❌ | Pending |
| Theme System | ✅ | ❌ | Pending |

### Notes & Blockers

#### Considerations
- **SignalR Integration**: Ensure SignalR hub is compatible with Blazor
- **Authentication**: Plan token refresh strategy in Blazor
- **Performance**: Monitor Blazor WebAssembly bundle size and load times
- **Testing**: Establish testing strategy for Fluxor state management
- **AI Agent Workflow**: Each phase includes specific, measurable deliverables for AI agents to implement with human verification

#### Potential Challenges
1. **Learning Curve**: Team needs to learn Fluxor patterns if Redux is familiar
2. **Migration Duration**: Estimate 6-8 weeks for complete migration
3. **Backwards Compatibility**: React app will run alongside Blazor during transition
4. **Data Consistency**: Ensure both apps stay in sync during phased rollout
5. **Browser Compatibility**: Blazor WebAssembly requires modern browsers with WebAssembly support
6. **AI Agent Coordination**: Ensure clear specifications and approval gates between AI implementation cycles

#### Risk Mitigation
- Keep React app running in parallel
- Test Blazor pages with beta users before full rollout
- Have rollback plan (revert to React)
- Automated tests for critical paths
- Monitor performance metrics
- **AI Agent Checkpoints**: Schedule human review at end of each phase to validate implementation quality and direction

---

## Resource Links

### Blazor Documentation
- [Blazor Official Docs](https://learn.microsoft.com/en-us/aspnet/core/blazor/)
- [Blazor Web App (Latest)](https://learn.microsoft.com/en-us/aspnet/core/blazor/hybrid)

### State Management
- [Fluxor Documentation](https://github.com/mrpmorris/Fluxor)
- [Redux → Fluxor Mapping](https://fluxor.io/)

### UI Components
- [MudBlazor Documentation](https://mudblazor.com/)
- [MudBlazor Component Library](https://mudblazor.com/components/)

### SignalR
- [Blazor SignalR Integration](https://learn.microsoft.com/en-us/aspnet/core/blazor/tutorials/signalr-blazor)

---

## Phase 1 Completion Summary

### Achievements (January 28, 2026)

**Project Structure**
- ✅ Created `Granite.Web.Client` project (.NET 9 Blazor WebAssembly)
- ✅ Created `Granite.Web.Tests` project (xUnit + Moq)
- ✅ Configured NuGet dependencies:
  - Fluxor.Blazor.Web 6.1.0
  - MudBlazor 8.15.0
  - Microsoft.AspNetCore.SignalR.Client 9.0.1

**State Management (Fluxor)**
- ✅ 5 Feature state structures: Players, Mods, Server, Auth, World
- ✅ 14+ reducers with full unit test coverage
- ✅ Configured in Program.cs with assembly scanning

**API Service Layer**
- ✅ BaseApiClient with HTTP methods (GET, POST, PUT, DELETE)
- ✅ 5 API clients: IPlayersApiClient, IModsApiClient, IServerApiClient, IAuthApiClient, IWorldApiClient
- ✅ 50+ API methods returning `JsonApiDocument<T>` wrapper
- ✅ 42 unit tests with mocked HttpClient

**SignalR Integration**
- ✅ ISignalRService with auto-reconnection (exponential backoff)
- ✅ Connection state management and change notifications
- ✅ Event handler registration and publishing
- ✅ 12 unit tests covering all scenarios
- ✅ Visual connection indicator in AppBar

**UI Components & Layout**
- ✅ MudBlazor properly configured (CSS, JS, fonts)
- ✅ MainLayout with MudAppBar, MudDrawer, MudMainContent
- ✅ NavMenu with hierarchical navigation (MudNavGroup)
- ✅ Dark mode toggle functionality
- ✅ Responsive drawer navigation
- ✅ 13 placeholder pages for all routes
- ✅ Client-side routing working without page refreshes

**Test Coverage**
- ✅ 54/54 unit tests passing
- ✅ Build succeeds with no errors
- ✅ All services properly registered in DI container

**Known Issues Resolved**
- ✅ Fixed MudPopoverProvider placement (must be service provider, not wrapper)
- ✅ Fixed AppBar alignment with MudSpacer
- ✅ Fixed navigation causing page reloads (created all route pages)
- ✅ Fixed MudBlazor Typo and Icon references

### Next Phase Readiness

**Ready for Phase 2: Core Features**
- Authentication pages implementation
- Player management with data grid
- Server status monitoring with real data
- Mod management with install/uninstall

All foundation infrastructure is in place and tested. Moving to feature implementation.

---

## Next Steps

1. ✅ ~~**Review this document** with the team~~
2. ✅ ~~**Identify any missing requirements** or features~~
3. ✅ ~~**Begin Phase 1** implementation~~
4. ✅ ~~**Update progress tracking** section as work progresses~~
5. **Begin Phase 2** - Core feature implementation
5. **Schedule weekly reviews** to track progress and adjust roadmap

---

**Document Owner**: Development Team  
**Last Review**: January 27, 2026  
**Next Review**: [To be scheduled]
