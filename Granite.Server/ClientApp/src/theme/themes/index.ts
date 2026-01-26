import type { ThemeDefinition, ThemeMetadata } from './themeTypes';
import { defaultTheme } from './default.theme';
import { vintageStoryTheme } from './vintageStory.theme';

/**
 * Theme Registry
 * Central registry of all available themes in the application
 */

// All available themes
export const themes: Record<string, ThemeDefinition> = {
  [defaultTheme.id]: defaultTheme,
  [vintageStoryTheme.id]: vintageStoryTheme,
};

// Get list of theme metadata for UI selection
export const getAvailableThemes = (): ThemeMetadata[] => {
  return Object.values(themes).map(theme => ({
    id: theme.id,
    name: theme.name,
    description: theme.description,
  }));
};

// Get a specific theme by ID
export const getTheme = (themeId: string): ThemeDefinition => {
  return themes[themeId] || themes[defaultTheme.id];
};

// Default theme ID
export const DEFAULT_THEME_ID = defaultTheme.id;

// Export individual themes
export { defaultTheme } from './default.theme';
export { vintageStoryTheme } from './vintageStory.theme';
export * from './themeTypes';
