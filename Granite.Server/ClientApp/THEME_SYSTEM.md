# Multi-Theme System

## Overview

The Granite Server ClientApp now supports multiple themes with easy switching. Users can select from available themes and their preference is saved in the browser's localStorage.

## Architecture

### File Structure

```
/theme/
  ├── themes/
  │   ├── index.ts                  # Theme registry and exports
  │   ├── themeTypes.ts             # TypeScript interfaces
  │   ├── default.theme.ts          # Default blue theme
  │   └── vintageStory.theme.ts     # Vintage Story earth-tones theme
  ├── themePrimitives.ts            # Core theme building blocks
  ├── AppTheme.tsx                  # Theme provider with dynamic selection
  ├── useThemeSelection.ts          # Hook for theme state management
  ├── ThemeSelector.tsx             # UI component for theme selection
  ├── ColorModeSelect.tsx           # Light/Dark mode selector
  └── customizations/               # MUI component overrides
```

## How It Works

### 1. Theme Definitions

Each theme is defined in its own file with:
- **ID**: Unique identifier
- **Name**: Display name for UI
- **Description**: Brief description
- **Colors**: Complete color palettes (brand, gray, green, orange, red)

Example:
```typescript
export const myTheme: ThemeDefinition = {
  id: 'myTheme',
  name: 'My Theme',
  description: 'A custom theme',
  colors: {
    brand: { 50: '...', 100: '...', /* ... */ 900: '...' },
    gray: { /* ... */ },
    green: { /* ... */ },
    orange: { /* ... */ },
    red: { /* ... */ },
  },
};
```

### 2. Theme Registry

The `themes/index.ts` file maintains a registry of all available themes. To add a new theme:

1. Create theme definition file
2. Import and add to registry
3. Theme becomes available automatically

### 3. Theme Selection

- `useThemeSelection()` hook manages theme state
- Persists selection to localStorage (`granite-theme-preference`)
- Syncs across browser tabs/windows
- Falls back to default theme if needed

### 4. Dynamic Application

- `AppTheme` component reads current theme from hook
- Generates MUI `colorSchemes` for light/dark modes
- Updates `brand`, `gray`, etc. variables for customization files
- MUI rebuilds theme with new colors

## Available Themes

### Default
- Modern blue color scheme
- Clean, professional design
- Original theme

### Vintage Story
- Warm earth tones (browns, ambers, ochres)
- Medieval aesthetic
- Inspired by Vintage Story game
- Muted, natural palette

## Usage

### For Users

1. Click the menu icon in the user profile section (bottom of sidebar)
2. Select your preferred theme from the "Theme" dropdown
3. Select light/dark mode from the "Mode" dropdown
4. Settings are saved automatically

### For Developers

#### Using the Theme Hook

```typescript
import { useThemeSelection } from '../theme/useThemeSelection';

function MyComponent() {
  const { currentTheme, currentThemeId, setTheme } = useThemeSelection();
  
  // Access current theme
  console.log(currentTheme.name);
  
  // Switch theme
  setTheme('vintageStory');
}
```

#### Creating a New Theme

1. Create `themes/myTheme.theme.ts`:

```typescript
import type { ThemeDefinition } from './themeTypes';

export const myTheme: ThemeDefinition = {
  id: 'myTheme',
  name: 'My Theme',
  description: 'Description of my theme',
  colors: {
    brand: {
      50: 'hsl(...)',
      // ... define all 10 shades
      900: 'hsl(...)',
    },
    // ... define gray, green, orange, red
  },
};
```

2. Add to registry in `themes/index.ts`:

```typescript
import { myTheme } from './myTheme.theme';

export const themes: Record<string, ThemeDefinition> = {
  [defaultTheme.id]: defaultTheme,
  [vintageStory.id]: vintageStory,
  [myTheme.id]: myTheme, // Add here
};
```

3. Export it:

```typescript
export { myTheme } from './myTheme.theme';
```

That's it! Your theme is now available in the UI.

## Color Palette Guidelines

Each color palette should have 10 shades (50-900):
- **50-200**: Very light shades (backgrounds, hover states)
- **300-400**: Light-medium shades (borders, disabled states)
- **500-600**: Main colors (primary actions, text)
- **700-800**: Dark shades (active states, emphasis)
- **900**: Very dark (high contrast, dark backgrounds)

Use HSL color format for easier manipulation:
- `hsl(hue, saturation%, lightness%)`

## Theme and Mode are Independent

- **Theme**: Color scheme (Default, Vintage Story, etc.)
- **Mode**: Light or Dark
- Both can be combined: "Vintage Story theme in dark mode"
- Each theme supports both light and dark modes

## Storage

Theme preference is stored in:
- **Location**: Browser localStorage
- **Key**: `granite-theme-preference`
- **Value**: Theme ID string (e.g., "vintageStory")
- **Scope**: Per browser, per domain

## Future Enhancements

Potential improvements:
- [ ] User account theme preferences (when user system is implemented)
- [ ] Theme preview/thumbnails
- [ ] Custom theme builder UI
- [ ] Import/export themes
- [ ] Per-server theme preferences
- [ ] More built-in themes
