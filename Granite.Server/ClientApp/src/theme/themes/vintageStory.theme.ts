import type { ThemeDefinition } from './themeTypes';

/**
 * Vintage Story theme - Warm earth tones with medieval aesthetic
 * Inspired by the natural world, with browns, ambers, and muted greens
 * Perfect for the Vintage Story server management interface
 */
export const vintageStoryTheme: ThemeDefinition = {
  id: 'vintageStory',
  name: 'Vintage Story',
  description: 'Warm medieval theme inspired by Vintage Story with earthy tones',
  colors: {
    // Primary brand: Warm amber/torchlight with ochre tones
    brand: {
      50: 'hsl(35, 60%, 96%)',   // Very light parchment
      100: 'hsl(35, 55%, 90%)',  // Light parchment
      200: 'hsl(35, 50%, 80%)',  // Pale amber
      300: 'hsl(35, 50%, 65%)',  // Light amber
      400: 'hsl(35, 55%, 50%)',  // Torchlight amber
      500: 'hsl(35, 60%, 42%)',  // Rich amber
      600: 'hsl(32, 55%, 38%)',  // Deep amber
      700: 'hsl(30, 50%, 30%)',  // Dark ochre
      800: 'hsl(28, 45%, 20%)',  // Very dark ochre
      900: 'hsl(25, 40%, 12%)',  // Near-black brown
    },
    // Gray: Blue-tinged stone with cooler tones
    gray: {
      50: 'hsl(30, 20%, 96%)',   // Light stone
      100: 'hsl(30, 15%, 92%)',  // Pale stone
      200: 'hsl(28, 12%, 85%)',  // Light gray-brown
      300: 'hsl(25, 10%, 72%)',  // Medium stone
      400: 'hsl(25, 8%, 58%)',   // Gray stone
      500: 'hsl(25, 8%, 42%)',   // Dark stone
      600: 'hsl(25, 10%, 32%)',  // Darker stone
      700: 'hsl(25, 12%, 22%)',  // Charcoal
      800: 'hsl(25, 15%, 12%)',  // Deep charcoal
      900: 'hsl(25, 18%, 8%)',   // Near-black
    },
    // Success: Earthy forest green
    green: {
      50: 'hsl(100, 40%, 96%)',
      100: 'hsl(100, 38%, 90%)',
      200: 'hsl(100, 36%, 82%)',
      300: 'hsl(100, 34%, 68%)',
      400: 'hsl(100, 32%, 52%)',
      500: 'hsl(100, 35%, 38%)',  // Forest green
      600: 'hsl(100, 40%, 30%)',
      700: 'hsl(100, 45%, 22%)',
      800: 'hsl(100, 50%, 14%)',
      900: 'hsl(100, 55%, 8%)',
    },
    // Warning: Warm fire orange
    orange: {
      50: 'hsl(25, 80%, 96%)',
      100: 'hsl(25, 75%, 90%)',
      200: 'hsl(25, 72%, 80%)',
      300: 'hsl(25, 70%, 65%)',
      400: 'hsl(25, 68%, 52%)',
      500: 'hsl(25, 70%, 42%)',  // Fire orange
      600: 'hsl(25, 72%, 35%)',
      700: 'hsl(25, 75%, 26%)',
      800: 'hsl(25, 78%, 18%)',
      900: 'hsl(25, 80%, 10%)',
    },
    // Error: Muted red-brown
    red: {
      50: 'hsl(0, 60%, 96%)',
      100: 'hsl(0, 55%, 90%)',
      200: 'hsl(0, 52%, 80%)',
      300: 'hsl(0, 50%, 65%)',
      400: 'hsl(0, 48%, 50%)',
      500: 'hsl(0, 50%, 38%)',   // Muted red
      600: 'hsl(0, 52%, 30%)',
      700: 'hsl(0, 55%, 22%)',
      800: 'hsl(0, 58%, 14%)',
      900: 'hsl(0, 60%, 8%)',
    },
  },
};
