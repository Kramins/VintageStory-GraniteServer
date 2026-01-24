import { 
  FormControl, 
  Select, 
  MenuItem, 
  Box, 
  Typography,
  CircularProgress,
  Alert
} from '@mui/material';
import type { SelectChangeEvent } from '@mui/material';
import { Storage as StorageIcon } from '@mui/icons-material';
import { useNavigate } from 'react-router-dom';
import { useAppDispatch, useAppSelector } from '../store/store';
import { selectServer } from '../store/slices/serversSlice';

export default function ServerSelector() {
  const dispatch = useAppDispatch();
  const navigate = useNavigate();
  const servers = useAppSelector(state => state.servers.servers);
  const selectedServerId = useAppSelector(state => state.servers.selectedServerId);
  const loading = useAppSelector(state => state.servers.loading);
  const error = useAppSelector(state => state.servers.error);

  const handleChange = (event: SelectChangeEvent<string>) => {
    const serverId = event.target.value;
    dispatch(selectServer(serverId));
    // Navigate to the overview page for the selected server
    navigate(`/${serverId}`);
  };

  if (loading) {
    return (
      <Box sx={{ px: 2, py: 1, display: 'flex', alignItems: 'center', gap: 1 }}>
        <StorageIcon fontSize="small" />
        <CircularProgress size={16} />
        <Typography variant="caption" color="text.secondary">
          Loading servers...
        </Typography>
      </Box>
    );
  }

  if (error) {
    return (
      <Box sx={{ px: 2, py: 1 }}>
        <Alert severity="error" sx={{ py: 0 }}>
          {error}
        </Alert>
      </Box>
    );
  }

  if (servers.length === 0) {
    return (
      <Box sx={{ px: 2, py: 1 }}>
        <Alert severity="info" sx={{ py: 0 }}>
          No game servers connected. Start a Vintage Story server with Granite.Mod to begin.
        </Alert>
      </Box>
    );
  }

  return (
    <Box sx={{ px: 2, py: 1 }}>
      <Box sx={{ display: 'flex', alignItems: 'center', gap: 1, mb: 0.5 }}>
        <StorageIcon fontSize="small" color="action" />
        <Typography variant="caption" color="text.secondary">
          Game Server
        </Typography>
      </Box>
      <FormControl fullWidth size="small">
        <Select
          value={selectedServerId || ''}
          onChange={handleChange}
          displayEmpty
          renderValue={(value) => {
            if (!value) return <em>Select a server</em>;
            const server = servers.find(s => s.id === value);
            return server ? server.name : <em>Unknown server</em>;
          }}
        >
          {servers.map((server) => (
            <MenuItem key={server.id} value={server.id}>
              <Box>
                <Typography variant="body2">{server.name}</Typography>
                {server.description && (
                  <Typography variant="caption" color="text.secondary">
                    {server.description}
                  </Typography>
                )}
              </Box>
            </MenuItem>
          ))}
        </Select>
      </FormControl>
    </Box>
  );
}
