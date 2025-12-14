import React, { useEffect, useState } from 'react';
import {
    AppBar,
    Toolbar,
    Typography,
    Box,
    Avatar,
    Menu,
    MenuItem,
    IconButton,
    Chip,
    useMediaQuery,
    useTheme,
} from '@mui/material';
import {
    Menu as MenuIcon,
    Settings as SettingsIcon,
    Logout as LogoutIcon,
    AccountCircle as AccountIcon,
} from '@mui/icons-material';
import ServerService from '../services/ServerService';
import type { ServerStatusDTO } from '../types/ServerStatusDTO';

interface HeaderProps {
    onMenuClick?: () => void;
}

const Header: React.FC<HeaderProps> = ({ onMenuClick }) => {
    const [status, setStatus] = useState<ServerStatusDTO | null>(null);
    const [anchorEl, setAnchorEl] = useState<null | HTMLElement>(null);
    const theme = useTheme();
    const isMobile = useMediaQuery(theme.breakpoints.down('sm'));

    useEffect(() => {
        ServerService.getStatus()
            .then(setStatus)
            .catch(err => console.error('Failed to load server status:', err));

        // Refresh status every 30 seconds
        const interval = setInterval(() => {
            ServerService.getStatus()
                .then(setStatus)
                .catch(err => console.error('Failed to load server status:', err));
        }, 30000);

        return () => clearInterval(interval);
    }, []);

    const handleMenuOpen = (event: React.MouseEvent<HTMLElement>) => {
        setAnchorEl(event.currentTarget);
    };

    const handleMenuClose = () => {
        setAnchorEl(null);
    };

    return (
        <AppBar
            position="fixed"
            sx={{
                zIndex: (theme) => theme.zIndex.drawer + 1,
                background: theme.palette.background.paper,
                borderBottom: '1px solid',
                borderColor: 'divider',
                boxShadow: 'none',
            }}
        >
            <Toolbar sx={{ minHeight: { xs: 56, sm: 64 } }}>
                {/* Menu Icon for Mobile */}
                {isMobile && onMenuClick && (
                    <IconButton
                        color="inherit"
                        onClick={onMenuClick}
                        sx={{ mr: 2 }}
                    >
                        <MenuIcon />
                    </IconButton>
                )}

                {/* Logo / Title */}
                <Box sx={{ display: 'flex', alignItems: 'center', gap: 1 }}>
                    <Typography
                        variant="h6"
                        sx={{
                            fontWeight: 700,
                            color: 'primary.main',
                            display: { xs: 'none', sm: 'block' },
                        }}
                    >
                        Granite Server
                    </Typography>
                </Box>

                {/* Status Info - Desktop only */}
                {!isMobile && status && (
                    <Box sx={{ display: 'flex', gap: 2, ml: 'auto', mr: 3 }}>
                        <Box sx={{ display: 'flex', alignItems: 'center', gap: 0.5 }}>
                            <Typography variant="caption" color="text.secondary">
                                Players:
                            </Typography>
                            <Typography variant="body2" sx={{ fontWeight: 600 }}>
                                {status.currentPlayers}/{status.maxPlayers}
                            </Typography>
                        </Box>

                        <Box sx={{ display: 'flex', alignItems: 'center', gap: 0.5 }}>
                            <Typography variant="caption" color="text.secondary">
                                Status:
                            </Typography>
                            <Chip
                                label={status.isOnline ? 'Online' : 'Offline'}
                                size="small"
                                color={status.isOnline ? 'success' : 'error'}
                                variant="outlined"
                            />
                        </Box>
                    </Box>
                )}

                {/* Mobile Status - Compact */}
                {isMobile && status && (
                    <Box sx={{ display: 'flex', gap: 1, ml: 'auto', mr: 2 }}>
                        <Chip
                            label={status.isOnline ? 'Online' : 'Offline'}
                            size="small"
                            color={status.isOnline ? 'success' : 'error'}
                            variant="outlined"
                        />
                    </Box>
                )}

                {/* User Menu */}
                <Box sx={{ display: 'flex', alignItems: 'center', gap: 1 }}>
                    <IconButton
                        onClick={handleMenuOpen}
                        size="small"
                        sx={{
                            padding: 0.5,
                            backgroundColor: 'action.hover',
                            '&:hover': {
                                backgroundColor: 'action.selected',
                            },
                        }}
                    >
                        <Avatar
                            sx={{
                                width: 32,
                                height: 32,
                                backgroundColor: 'primary.main',
                                fontSize: '0.875rem',
                            }}
                        >
                            G
                        </Avatar>
                    </IconButton>
                    <Menu
                        anchorEl={anchorEl}
                        open={Boolean(anchorEl)}
                        onClose={handleMenuClose}
                        transformOrigin={{ horizontal: 'right', vertical: 'top' }}
                        anchorOrigin={{ horizontal: 'right', vertical: 'bottom' }}
                    >
                        <MenuItem disabled>
                            <AccountIcon sx={{ mr: 1 }} />
                            Admin Account
                        </MenuItem>
                        <MenuItem onClick={handleMenuClose}>
                            <SettingsIcon sx={{ mr: 1 }} />
                            Settings
                        </MenuItem>
                        <MenuItem onClick={handleMenuClose}>
                            <LogoutIcon sx={{ mr: 1 }} />
                            Logout
                        </MenuItem>
                    </Menu>
                </Box>
            </Toolbar>
        </AppBar>
    );
};

export default Header;
