import * as React from 'react';
import MenuItem from '@mui/material/MenuItem';
import Select from '@mui/material/Select';
import type { SelectProps } from '@mui/material/Select';
import FormControl from '@mui/material/FormControl';
import InputLabel from '@mui/material/InputLabel';
import { getAvailableThemes } from './themes';
import { useThemeSelection } from './ThemeSelectionProvider';

interface ThemeSelectorProps extends Omit<SelectProps, 'value' | 'onChange'> {
  showLabel?: boolean;
}

/**
 * Theme selector component
 * Allows users to switch between available themes
 */
export default function ThemeSelector({ showLabel = false, ...props }: ThemeSelectorProps) {
  const { currentThemeId, setTheme } = useThemeSelection();
  const availableThemes = getAvailableThemes();

  const handleChange = (event: any) => {
    setTheme(event.target.value as string);
  };

  const selector = (
    <Select
      value={currentThemeId}
      onChange={handleChange}
      {...props}
    >
      {availableThemes.map((theme) => (
        <MenuItem key={theme.id} value={theme.id}>
          {theme.name}
        </MenuItem>
      ))}
    </Select>
  );

  if (showLabel) {
    return (
      <FormControl size="small">
        <InputLabel>Theme</InputLabel>
        {selector}
      </FormControl>
    );
  }

  return selector;
}
