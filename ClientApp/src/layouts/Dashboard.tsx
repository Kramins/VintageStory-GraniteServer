import { Outlet } from 'react-router-dom';
import { useState } from 'react';

import Toolbar from '@mui/material/Toolbar';
import Box from '@mui/material/Box';
import Header from '../components/Header';
import Drawer from '../components/Drawer';


export default function DashboardLayout() {
    const [drawerOpen, setDrawerOpen] = useState(false);

    return (
        <Box sx={{ display: 'flex', width: '100%' }}>
            <Header onMenuClick={() => setDrawerOpen(!drawerOpen)} />
            <Drawer open={drawerOpen} onClose={() => setDrawerOpen(false)} />

            <Box component="main" sx={{ width: '100%', flexGrow: 1, p: { xs: 2, sm: 3 } }}>
                <Toolbar sx={{ mt: 'inherit' }} />
                <Box
                    sx={{
                        ...{ px: { xs: 0, sm: 2 } },
                        position: 'relative',
                        minHeight: 'calc(100vh - 110px)',
                        display: 'flex',
                        flexDirection: 'column'
                    }}
                >
                    {/* <Breadcrumbs /> */}
                    <Outlet />
                    {/* <Footer /> */}
                </Box>
            </Box>
        </Box>
    );

}
