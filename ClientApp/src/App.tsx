import { RouterProvider } from 'react-router-dom';
import { ThemeProvider, createTheme } from '@mui/material/styles';
import CssBaseline from '@mui/material/CssBaseline';

import './App.css';

import router from './routes';

// import type {} from '@mui/x-date-pickers/themeAugmentation';
// import type {} from '@mui/x-charts/themeAugmentation';
// import type {} from '@mui/x-data-grid-pro/themeAugmentation';
// import type {} from '@mui/x-tree-view/themeAugmentation';
// import { alpha } from '@mui/material/styles';
// import CssBaseline from '@mui/material/CssBaseline';
// import Box from '@mui/material/Box';
// import Stack from '@mui/material/Stack';
// import AppNavbar from './components/AppNavbar';
// import Header from './components/Header';
// import MainGrid from './components/MainGrid';
// import SideMenu from './components/SideMenu';
import AppTheme from './theme/AppTheme';
// import {
//   chartsCustomizations,
//   dataGridCustomizations,
//   datePickersCustomizations,
//   treeViewCustomizations,
// } from './theme/customizations';

const xThemeComponents = {
  // ...chartsCustomizations,
  // ...dataGridCustomizations,
  // ...datePickersCustomizations,
  // ...treeViewCustomizations,
};

const darkTheme = createTheme({
  palette: {
    mode: 'dark',
    primary: {
      main: '#8B4513', // Vintage brown color
    },
    secondary: {
      main: '#D2691E', // Orange accent
    },
    background: {
      default: '#1a1a1a',
      paper: '#2d2d2d',
    },
  },
});

function App(props: { disableCustomTheme?: boolean }) {
  return (
    // <ThemeProvider theme={darkTheme}>
    //   <CssBaseline />
    //   <RouterProvider router={router} />
    // </ThemeProvider>
      <AppTheme {...props} themeComponents={xThemeComponents}>
        <CssBaseline enableColorScheme />
        <RouterProvider router={router} />
      </AppTheme>
  );
}

export default App;
