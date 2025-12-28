import React, { useEffect } from 'react';
import { Box, Typography, TableContainer, Paper, Table, TableHead, TableRow, TableCell, TableBody, Alert, CircularProgress, Button } from '@mui/material';
import { useAppDispatch, useAppSelector } from '../../store/store';
import { fetchPlayerSessions, clearSessions } from '../../store/slices/playerSessionsSlice';

interface Props {
  playerId: string;
}

const PlayerSessionsTab: React.FC<Props> = ({ playerId }) => {
  const dispatch = useAppDispatch();
  const { sessions, loading, error, page, hasMore, pagination, apiErrors } = useAppSelector(state => state.playerSessions);

  useEffect(() => {
    dispatch(clearSessions());
    dispatch(fetchPlayerSessions(playerId) as any);
  }, [playerId, dispatch]);

  const handleLoadMoreSessions = () => {
    dispatch(fetchPlayerSessions(playerId, page + 1) as any);
  };

  return (
    <Box sx={{ p: 2 }}>
      <Typography variant="h6" sx={{ mb: 2 }}>Sessions</Typography>
      {error && (
        <Alert severity="error" sx={{ mb: 2 }}>{error}</Alert>
      )}
      {apiErrors && apiErrors.length > 0 && apiErrors.map((err, idx) => (
        <Alert key={idx} severity="warning" sx={{ mb: 1 }}>
          {err.code ? `${err.code}: ` : ''}{err.message}
        </Alert>
      ))}
      {loading && sessions.length === 0 ? (
        <Box sx={{ display: 'flex', justifyContent: 'center', py: 4 }}>
          <CircularProgress />
        </Box>
      ) : (
        <TableContainer component={Paper}>
          <Table size="small">
            <TableHead>
              <TableRow sx={{ backgroundColor: '#f5f5f5' }}>
                <TableCell>Join Date</TableCell>
                <TableCell>Leave Date</TableCell>
                <TableCell>Duration</TableCell>
                <TableCell>IP</TableCell>
                <TableCell>Server</TableCell>
              </TableRow>
            </TableHead>
            <TableBody>
              {sessions.map((s) => {
                const join = new Date(s.joinDate);
                const leave = s.leaveDate ? new Date(s.leaveDate) : null;
                const duration = s.durationMinutes ?? (leave ? Math.round((leave.getTime() - join.getTime()) / 60000) : undefined);
                return (
                  <TableRow key={s.id}>
                    <TableCell>{join.toLocaleString()}</TableCell>
                    <TableCell>{leave ? leave.toLocaleString() : 'Active'}</TableCell>
                    <TableCell>{duration !== undefined ? `${duration} min` : '-'}</TableCell>
                    <TableCell>{s.ipAddress}</TableCell>
                    <TableCell>{s.serverName || s.serverId}</TableCell>
                  </TableRow>
                );
              })}
              {sessions.length === 0 && (
                <TableRow>
                  <TableCell colSpan={5}>
                    <Alert severity="info">No sessions found.</Alert>
                  </TableCell>
                </TableRow>
              )}
            </TableBody>
          </Table>
        </TableContainer>
      )}
      <Box sx={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', mt: 2 }}>
        <Typography variant="body2" color="text.secondary">
          Page {(pagination?.page ?? page) + 1} • Page size {pagination?.pageSize ?? ''}
        </Typography>
        <Button variant="contained" onClick={handleLoadMoreSessions} disabled={!hasMore || loading}>
          {loading ? 'Loading…' : hasMore ? 'Load More' : 'No More'}
        </Button>
      </Box>
    </Box>
  );
};

export default PlayerSessionsTab;
