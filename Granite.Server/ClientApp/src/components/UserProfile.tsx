import { useState } from 'react';
import Avatar from '@mui/material/Avatar';
import Box from '@mui/material/Box';
import Stack from '@mui/material/Stack';
import Typography from '@mui/material/Typography';
import IconButton from '@mui/material/IconButton';
import Menu from '@mui/material/Menu';
import MenuItem from '@mui/material/MenuItem';
import Divider from '@mui/material/Divider';
import ListItemIcon from '@mui/material/ListItemIcon';
import ListItemText from '@mui/material/ListItemText';
import MoreVertIcon from '@mui/icons-material/MoreVert';
import LogoutIcon from '@mui/icons-material/Logout';
import PaletteIcon from '@mui/icons-material/Palette';
import Brightness4Icon from '@mui/icons-material/Brightness4';
import { useNavigate } from 'react-router-dom';
import { useAppSelector } from '../store/store';
import { AuthService } from '../services/AuthService';
import ThemeSelector from '../theme/ThemeSelector';
import ColorModeSelect from '../theme/ColorModeSelect';

export default function UserProfile() {
  const userInfo = useAppSelector((state) => state.auth.user);
  const [anchorEl, setAnchorEl] = useState<null | HTMLElement>(null);
  const navigate = useNavigate();
  const open = Boolean(anchorEl);

  const handleMenuOpen = (event: React.MouseEvent<HTMLElement>) => {
    setAnchorEl(event.currentTarget);
  };

  const handleMenuClose = () => {
    setAnchorEl(null);
  };

  const handleLogout = () => {
    AuthService.logout();
    handleMenuClose();
    navigate('/login');
  };

  // If no user info, don't render anything
  if (!userInfo) {
    return null;
  }

  const initials = userInfo.username
    .split(' ')
    .map(n => n[0])
    .join('')
    .toUpperCase()
    .substring(0, 2);

  return (
    <Stack
      direction="row"
      sx={{
        p: 2,
        gap: 1,
        alignItems: 'center',
        borderTop: '1px solid',
        borderColor: 'divider',
      }}
    >
      <Avatar
        sx={{ 
          width: 36, 
          height: 36,
          bgcolor: 'primary.main'
        }}
      >
        {initials}
      </Avatar>
      <Box sx={{ mr: 'auto' }}>
        <Typography variant="body2" sx={{ fontWeight: 500, lineHeight: '16px' }}>
          {userInfo.username}
        </Typography>
        <Typography variant="caption" sx={{ color: 'text.secondary' }}>
          {userInfo.role}
        </Typography>
      </Box>
      <IconButton
        size="small"
        onClick={handleMenuOpen}
        aria-label="user menu"
      >
        <MoreVertIcon fontSize="small" />
      </IconButton>
      <Menu
        anchorEl={anchorEl}
        open={open}
        onClose={handleMenuClose}
        anchorOrigin={{
          vertical: 'top',
          horizontal: 'right',
        }}
        transformOrigin={{
          vertical: 'bottom',
          horizontal: 'right',
        }}
        slotProps={{
          paper: {
            sx: { minWidth: 200 }
          }
        }}
      >
        <Box sx={{ px: 2, py: 1.5 }}>
          <Stack spacing={1.5}>
            <Box>
              <Stack direction="row" spacing={1} alignItems="center" sx={{ mb: 0.5 }}>
                <PaletteIcon fontSize="small" color="action" />
                <Typography variant="caption" color="text.secondary">
                  Theme
                </Typography>
              </Stack>
              <ThemeSelector size="small" fullWidth />
            </Box>
            <Box>
              <Stack direction="row" spacing={1} alignItems="center" sx={{ mb: 0.5 }}>
                <Brightness4Icon fontSize="small" color="action" />
                <Typography variant="caption" color="text.secondary">
                  Mode
                </Typography>
              </Stack>
              <ColorModeSelect size="small" fullWidth />
            </Box>
          </Stack>
        </Box>
        <Divider />
        <MenuItem onClick={handleLogout}>
          <ListItemIcon>
            <LogoutIcon fontSize="small" />
          </ListItemIcon>
          <ListItemText>Logout</ListItemText>
        </MenuItem>
      </Menu>
    </Stack>
  );
}
