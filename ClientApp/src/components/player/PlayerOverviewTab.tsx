import React from 'react';
import { Box, Card, CardContent, Typography } from '@mui/material';
import type { PlayerDetailsDTO } from '../../types/PlayerDetailsDTO';

interface Props {
  playerDetails: PlayerDetailsDTO;
}

const PlayerOverviewTab: React.FC<Props> = ({ playerDetails }) => {
  return (
    <Box sx={{ display: 'grid', gridTemplateColumns: { xs: '1fr', sm: '1fr 1fr' }, gap: 3, mb: 3 }}>
      <Card>
        <CardContent>
          <Typography color="textSecondary" gutterBottom>
            Connection State
          </Typography>
          <Typography variant="h5">{playerDetails.connectionState}</Typography>
        </CardContent>
      </Card>
      <Card>
        <CardContent>
          <Typography color="textSecondary" gutterBottom>
            Last Join Date
          </Typography>
          <Typography variant="h5">{new Date(playerDetails.lastJoinDate).toLocaleString()}</Typography>
        </CardContent>
      </Card>
      <Card>
        <CardContent>
          <Typography color="textSecondary" gutterBottom>
            IP Address
          </Typography>
          <Typography variant="h5">{playerDetails.ipAddress}</Typography>
        </CardContent>
      </Card>
      <Card>
        <CardContent>
          <Typography color="textSecondary" gutterBottom>
            Ping
          </Typography>
          <Typography variant="h5">{playerDetails.ping}ms</Typography>
        </CardContent>
      </Card>
    </Box>
  );
};

export default PlayerOverviewTab;
