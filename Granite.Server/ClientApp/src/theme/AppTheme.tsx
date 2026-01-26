import * as React from 'react';
import { ThemeProvider, createTheme } from '@mui/material/styles';
import type { ThemeOptions } from '@mui/material/styles';
import { getInputsCustomizations } from './customizations/inputs';
import { getDataDisplayCustomizations } from './customizations/dataDisplay';
import { getFeedbackCustomizations } from './customizations/feedback';
import { getNavigationCustomizations } from './customizations/navigation';
import { getSurfacesCustomizations } from './customizations/surfaces';
import { getDataGridCustomizations } from './customizations/dataGrid';
import { getColorSchemes, typography, shadows, shape } from './themePrimitives';
import { useThemeSelection } from './ThemeSelectionProvider';

interface AppThemeProps {
  children: React.ReactNode;
  /**
   * This is for the docs site. You can ignore it or remove it.
   */
  disableCustomTheme?: boolean;
  themeComponents?: ThemeOptions['components'];
}

/**
 * AppTheme component that provides MUI theme with dynamic theme selection
 * 
 * The theme is built from:
 * 1. Color schemes (light/dark mode) generated from current theme colors
 * 2. Component customizations from factory functions
 * 3. Typography, shadows, and shape primitives
 * 
 * When the theme changes, React's useMemo dependency triggers a rebuild,
 * and the ThemeProvider's key prop forces a remount for clean state.
 */
export default function AppTheme(props: AppThemeProps) {
  const { children, disableCustomTheme, themeComponents } = props;
  const { currentTheme } = useThemeSelection();

  const theme = React.useMemo(() => {
    if (disableCustomTheme) {
      return {};
    }

    // Generate color schemes from the current theme
    const colorSchemes = getColorSchemes(currentTheme.colors);

    // Get fresh customizations with current theme colors
    const inputsCustomizations = getInputsCustomizations(currentTheme.colors.gray, currentTheme.colors.brand);
    const dataDisplayCustomizations = getDataDisplayCustomizations(currentTheme.colors.gray, currentTheme.colors.red, currentTheme.colors.green);
    const feedbackCustomizations = getFeedbackCustomizations(currentTheme.colors.gray, currentTheme.colors.orange);
    const navigationCustomizations = getNavigationCustomizations(currentTheme.colors.gray, currentTheme.colors.brand);
    const surfacesCustomizations = getSurfacesCustomizations(currentTheme.colors.gray);
    const dataGridCustomizations = getDataGridCustomizations(currentTheme.colors.gray, currentTheme.colors.brand);

    // Force re-evaluation by creating fresh theme
    return createTheme({
      // For more details about CSS variables configuration, see https://mui.com/material-ui/customization/css-theme-variables/configuration/
      cssVariables: {
        colorSchemeSelector: 'data-mui-color-scheme',
        cssVarPrefix: 'template',
      },
      colorSchemes, // Recently added in v6 for building light & dark mode app, see https://mui.com/material-ui/customization/palette/#color-schemes
      typography,
      shadows,
      shape,
      components: {
        ...inputsCustomizations,
        ...dataDisplayCustomizations,
        ...feedbackCustomizations,
        ...navigationCustomizations,
        ...surfacesCustomizations,
        ...dataGridCustomizations,
        ...themeComponents,
      },
    });
  }, [disableCustomTheme, themeComponents, currentTheme]);

  if (disableCustomTheme) {
    return <React.Fragment>{children}</React.Fragment>;
  }
  
  return (
    <ThemeProvider 
      key={`theme-${currentTheme.id}`}
      theme={theme} 
      disableTransitionOnChange
    >
      {children}
    </ThemeProvider>
  );
}
