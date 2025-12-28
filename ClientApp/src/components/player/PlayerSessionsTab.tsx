import React, { useEffect } from 'react';
import { Box, Typography, Alert, CircularProgress, Button, IconButton } from '@mui/material';
import { DataGrid } from '@mui/x-data-grid';
import type { GridRenderCellParams } from '@mui/x-data-grid';
import VisibilityIcon from '@mui/icons-material/Visibility';
import { useAppDispatch, useAppSelector } from '../../store/store';
import { fetchPlayerSessions, clearSessions, setSort } from '../../store/slices/playerSessionsSlice';

interface Props {
  playerId: string;
}

const PlayerSessionsTab: React.FC<Props> = ({ playerId }) => {
  const dispatch = useAppDispatch();
  const { sessions, loading, error, page, hasMore, pagination, apiErrors, sortField, sortDirection } = useAppSelector(state => state.playerSessions);
  const pageSize = pagination?.pageSize ?? 20;

  useEffect(() => {
    dispatch(clearSessions());
    dispatch(fetchPlayerSessions(playerId) as any);
  }, [playerId, dispatch]);

  const handleLoadMoreSessions = () => {
    dispatch(fetchPlayerSessions(playerId, page + 1) as any);
  };

  const formatDate = (dateString: string): string => {
    if (!dateString) return '-';
    const date = new Date(dateString);
    return date.toLocaleString('en-US', { 
      year: 'numeric', 
      month: 'short', 
      day: 'numeric', 
      hour: '2-digit', 
      minute: '2-digit',
      second: '2-digit'
    });
  };

  const formatDuration = (seconds: number): string => {
    const hours = Math.floor(seconds / 3600);
    const minutes = Math.floor((seconds % 3600) / 60);
    const secs = Math.floor(seconds % 60);
    return `${hours.toString().padStart(2, '0')}:${minutes.toString().padStart(2, '0')}:${secs.toString().padStart(2, '0')}`;
  };

  const columns = [
    { field: 'serverName', headerName: 'Server', width: 150 },
    { 
      field: 'joinDate', 
      headerName: 'Join Date', 
      width: 200,
      valueGetter: (value: string) => formatDate(value)
    },
    { 
      field: 'leaveDate', 
      headerName: 'Leave Date', 
      width: 200,
      valueGetter: (value: string) => formatDate(value)
    },
    { 
      field: 'duration', 
      headerName: 'Duration', 
      width: 150,
      valueGetter: (value: number) => formatDuration(value)
    },
    { field: 'ipAddress', headerName: 'IP', width: 150 },
    {
      field: 'actions',
      headerName: 'Actions',
      width: 100,
      sortable: false,
      renderCell: (params: GridRenderCellParams) => (
        <IconButton 
          size="small" 
          color="primary"
          onClick={() => console.log('View session:', params.row)}
          aria-label="view session"
        >
          <VisibilityIcon />
        </IconButton>
      ),
    },
  ];

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
        <Box sx={{ height: 400, width: '100%' }}>
          <DataGrid
            rows={sessions}
            columns={columns}
            paginationModel={{ page: page, pageSize: pageSize }}
            pageSizeOptions={[20, 50, 100]}
            pagination
            onPaginationModelChange={(model) => {
              if (model.page !== page) {
                dispatch(fetchPlayerSessions(playerId, model.page + 1) as any);
              }
            }}
            sortingMode="server"
            sortModel={sortField && sortDirection ? [{ field: sortField, sort: sortDirection }] : [{ field: 'joinDate', sort: 'desc' }]}
            onSortModelChange={(model) => {
              if (model.length > 0) {
                const { field, sort } = model[0];
                if (sort && (field !== sortField || sort !== sortDirection)) {
                  dispatch(setSort({ field, direction: sort }) as any);
                  dispatch(fetchPlayerSessions(playerId, 0) as any);
                }
              }
            }}
            rowCount={pagination?.totalCount ?? 0}
            loading={loading}
            paginationMode="server"
          />
        </Box>
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
