import React from 'react';
import { Box, Typography, Alert } from '@mui/material';

interface Props {
  playerId: string;
}

const PlayerPermissionsTab: React.FC<Props> = ({ playerId }) => {
  return (
    <Box sx={{ p: 2 }}>
      <Typography variant="h6">Permissions</Typography>
      <Alert severity="info" sx={{ mt: 2 }}>
        Permissions management UI coming soon for player {playerId}.
      </Alert>
    </Box>
  );
};

export default PlayerPermissionsTab;
