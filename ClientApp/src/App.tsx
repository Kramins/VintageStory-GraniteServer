import { RouterProvider } from 'react-router-dom';
import { ThemeProvider, createTheme } from '@mui/material/styles';
import CssBaseline from '@mui/material/CssBaseline';

import './App.css';

import router from './routes';

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

function App() {
  return (
    <ThemeProvider theme={darkTheme}>
      <CssBaseline />
      <RouterProvider router={router} />
    </ThemeProvider>
  );
}

export default App;
