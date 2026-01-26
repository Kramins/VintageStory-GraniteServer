import * as React from 'react';
import { DEFAULT_THEME_ID, getTheme, type ThemeDefinition } from './themes';

const THEME_STORAGE_KEY = 'granite-theme-preference';

interface ThemeSelectionContextValue {
  currentTheme: ThemeDefinition;
  currentThemeId: string;
  setTheme: (themeId: string) => void;
}

const ThemeSelectionContext = React.createContext<ThemeSelectionContextValue | null>(null);

/**
 * Provider for theme selection state
 * Similar to MUI's ColorSchemeProvider
 */
export function ThemeSelectionProvider({ children }: { children: React.ReactNode }) {
  // Initialize theme from localStorage or use default
  const [currentThemeId, setCurrentThemeId] = React.useState<string>(() => {
    try {
      const stored = localStorage.getItem(THEME_STORAGE_KEY);
      return stored || DEFAULT_THEME_ID;
    } catch (error) {
      console.warn('Failed to load theme preference from localStorage:', error);
      return DEFAULT_THEME_ID;
    }
  });

  // Get the current theme definition
  const currentTheme = React.useMemo(() => {
    const theme = getTheme(currentThemeId);
    return theme;
  }, [currentThemeId]);

  // Set theme and persist to localStorage
  const setTheme = React.useCallback((themeId: string) => {
    try {
      localStorage.setItem(THEME_STORAGE_KEY, themeId);
      setCurrentThemeId(themeId);
    } catch (error) {
      console.error('Failed to save theme preference to localStorage:', error);
      // Still update state even if localStorage fails
      setCurrentThemeId(themeId);
    }
  }, []);

  // Listen for storage events from other tabs/windows
  React.useEffect(() => {
    const handleStorageChange = (e: StorageEvent) => {
      if (e.key === THEME_STORAGE_KEY && e.newValue) {
        setCurrentThemeId(e.newValue);
      }
    };

    window.addEventListener('storage', handleStorageChange);
    return () => window.removeEventListener('storage', handleStorageChange);
  }, []);

  const value = React.useMemo(
    () => ({ currentTheme, currentThemeId, setTheme }),
    [currentTheme, currentThemeId, setTheme]
  );

  return (
    <ThemeSelectionContext.Provider value={value}>
      {children}
    </ThemeSelectionContext.Provider>
  );
}

/**
 * Hook to access theme selection context
 */
export function useThemeSelection() {
  const context = React.useContext(ThemeSelectionContext);
  if (!context) {
    throw new Error('useThemeSelection must be used within ThemeSelectionProvider');
  }
  return context;
}
