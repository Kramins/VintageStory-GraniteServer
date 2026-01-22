import { RouterProvider } from 'react-router-dom';
// ThemeProvider not used directly
import CssBaseline from '@mui/material/CssBaseline';

import './App.css';

import router from './routes';
import AuthInitializer from './components/AuthInitializer';
// TODO: Re-enable server status monitoring after multi-server architecture is complete
// import DisconnectedModal from './components/DisconnectedModal';
// import { useServerStatusMonitor } from './hooks/useServerStatusMonitor';
// import { useAppSelector } from './store/store';
import { ToastProvider } from './components/ToastProvider';

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

function AppContent() {
  // TODO: Re-enable server status monitoring for multi-server architecture
  // const { retryConnection } = useServerStatusMonitor();
  // const { isServerConnected, disconnectionReason } = useAppSelector(state => state.ui);

  return (
    <>
      {/* TODO: Re-enable disconnected modal after multi-server support is added */}
      {/* <DisconnectedModal
        open={!isServerConnected}
        onRetry={retryConnection}
        reason={disconnectionReason}
      /> */}
      <AuthInitializer />
      <RouterProvider router={router} />
    </>
  );
}

function App(props: { disableCustomTheme?: boolean }) {
  return (
    // <ThemeProvider theme={darkTheme}>
    //   <CssBaseline />
    //   <RouterProvider router={router} />
    // </ThemeProvider>
      <AppTheme {...props} themeComponents={xThemeComponents}>
        <CssBaseline enableColorScheme />
        <ToastProvider>
          <AppContent />
        </ToastProvider>
      </AppTheme>
  );
}

export default App;
