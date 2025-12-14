import React from 'react';
import { useLocation, useNavigate } from 'react-router-dom';
import {
    Drawer as MuiDrawer,
    List,
    ListItem,
    ListItemIcon,
    ListItemText,
    Box,
    useTheme,
    useMediaQuery,
} from '@mui/material';
import {
    Dashboard as DashboardIcon,
    People as PlayersIcon,
    Public as WorldIcon,
    Settings as SettingsIcon,
    Gavel as ModerationIcon,
} from '@mui/icons-material';

interface NavItem {
    path: string;
    label: string;
    icon: React.ReactNode;
}

interface DrawerProps {
    open: boolean;
    onClose: () => void;
    variant?: 'temporary' | 'permanent';
}

const DRAWER_WIDTH = 260;

const NAV_ITEMS: NavItem[] = [
    {
        path: '/',
        label: 'Overview',
        icon: <DashboardIcon />,
    },
    {
        path: '/players',
        label: 'Players',
        icon: <PlayersIcon />,
    },
    {
        path: '/world',
        label: 'World',
        icon: <WorldIcon />,
    },
    {
        path: '/moderation',
        label: 'Moderation',
        icon: <ModerationIcon />,
    },
    {
        path: '/settings',
        label: 'Settings',
        icon: <SettingsIcon />,
    },
];

const Drawer: React.FC<DrawerProps> = ({ open, onClose }) => {
    const navigate = useNavigate();
    const location = useLocation();
    const theme = useTheme();
    const isMobile = useMediaQuery(theme.breakpoints.down('md'));

    const handleNavigation = (path: string) => {
        navigate(path);
        if (isMobile) {
            onClose();
        }
    };

    const drawerContent = (
        <Box sx={{ display: 'flex', flexDirection: 'column', height: '100%' }}>
            {/* Logo Section */}
            {/* <Box
                sx={{
                    p: 2.5,
                    display: 'flex',
                    alignItems: 'center',
                    gap: 1,
                    borderBottom: '1px solid',
                    borderColor: 'divider',
                }}
            >
                <Box
                    sx={{
                        width: 40,
                        height: 40,
                        borderRadius: 1,
                        background: 'linear-gradient(135deg, #8B4513 0%, #D2691E 100%)',
                        display: 'flex',
                        alignItems: 'center',
                        justifyContent: 'center',
                        fontSize: '1.5rem',
                        fontWeight: 700,
                        color: 'white',
                    }}
                >
                    G
                </Box>
                <Box>
                    <div style={{ fontSize: '0.875rem', fontWeight: 600, lineHeight: 1.2 }}>
                        Granite
                    </div>
                    <div style={{ fontSize: '0.75rem', color: 'rgba(255,255,255,0.6)' }}>
                        Server
                    </div>
                </Box>
            </Box> */}

            {/* Navigation Items */}
            <List sx={{ flex: 1, pt: 1 }}>
                {NAV_ITEMS.map((item) => {
                    const isActive = location.pathname === item.path;
                    return (
                        <ListItem
                            key={item.path}
                            onClick={() => handleNavigation(item.path)}
                            sx={{
                                px: 1.5,
                                py: 1,
                                mx: 1,
                                mb: 0.5,
                                borderRadius: 1,
                                backgroundColor: isActive
                                    ? 'action.selected'
                                    : 'transparent',
                                color: isActive ? 'primary.main' : 'text.primary',
                                fontWeight: isActive ? 600 : 400,
                                '&:hover': {
                                    backgroundColor: isActive
                                        ? 'action.selected'
                                        : 'action.hover',
                                },
                                transition: 'all 0.2s ease',
                                cursor: 'pointer',
                            }}
                        >
                            <ListItemIcon
                                sx={{
                                    minWidth: 40,
                                    color: isActive ? 'primary.main' : 'inherit',
                                }}
                            >
                                {item.icon}
                            </ListItemIcon>
                            <ListItemText
                                primary={item.label}
                                primaryTypographyProps={{
                                    fontSize: '0.95rem',
                                    fontWeight: isActive ? 600 : 400,
                                }}
                            />
                        </ListItem>
                    );
                })}
            </List>

            {/* Footer */}
            <Box
                sx={{
                    p: 2,
                    borderTop: '1px solid',
                    borderColor: 'divider',
                    fontSize: '0.75rem',
                    color: 'text.secondary',
                    textAlign: 'center',
                }}
            >
                <div>Granite Server</div>
                <div>v1.0.0</div>
            </Box>
        </Box>
    );

    if (isMobile) {
        return (
            <MuiDrawer
                anchor="left"
                open={open}
                onClose={onClose}
                variant="temporary"
                sx={{
                    '& .MuiDrawer-paper': {
                        width: DRAWER_WIDTH,
                        boxSizing: 'border-box',
                        backgroundColor: 'background.paper',
                    },
                }}
            >
                {drawerContent}
            </MuiDrawer>
        );
    }

    return (
        <MuiDrawer
            variant="permanent"
            sx={{
                width: DRAWER_WIDTH,
                flexShrink: 0,
                '& .MuiDrawer-paper': {
                    width: DRAWER_WIDTH,
                    boxSizing: 'border-box',
                    backgroundColor: 'background.paper',
                    borderRight: '1px solid',
                    borderColor: 'divider',
                    mt: '64px',
                },
            }}
        >
            {drawerContent}
        </MuiDrawer>
    );
};

export { DRAWER_WIDTH };
export default Drawer;
