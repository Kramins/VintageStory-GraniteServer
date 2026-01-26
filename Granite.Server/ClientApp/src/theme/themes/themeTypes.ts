/**
 * Theme system type definitions
 * Defines the structure for creating custom themes
 */

export interface ColorPalette {
  50: string;
  100: string;
  200: string;
  300: string;
  400: string;
  500: string;
  600: string;
  700: string;
  800: string;
  900: string;
}

export interface ThemeColorPalettes {
  brand: ColorPalette;
  gray: ColorPalette;
  green: ColorPalette;
  orange: ColorPalette;
  red: ColorPalette;
}

export interface ThemeDefinition {
  id: string;
  name: string;
  description: string;
  colors: ThemeColorPalettes;
}

export interface ThemeMetadata {
  id: string;
  name: string;
  description: string;
}
