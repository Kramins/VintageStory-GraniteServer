import React, { useEffect } from 'react';
import {
    Box,
    Typography,
    Chip,
    Avatar,
    Button,
    Tabs,
    Tab,
    Dialog,
    DialogTitle,
    DialogContent,
    DialogContentText,
    TextField,
    DialogActions,
} from '@mui/material';
import type { GridColDef, GridRenderCellParams } from '@mui/x-data-grid';
import { DataGrid } from '@mui/x-data-grid';
import {
    Block as BlockIcon,
    Refresh as RefreshIcon,
} from '@mui/icons-material';
import { PlayerService } from '../services/PlayerService'
import type { PlayerDTO } from '../types/PlayerDTO';
import { useAppDispatch, useAppSelector } from '../store/store';
import { fetchAllPlayers } from '../store/slices/playersSlice';

// Temporary mapping for demo, since server DTO only has Id, Name, IsAdmin
// Map PlayerDTO to table row data for display
function mapPlayerDTOtoTable(player: PlayerDTO) {
    return {
        id: player.id,
        name: String(player.name),
        isAdmin: player.isAdmin,
        ipAddress: player.ipAddress,
        languageCode: player.languageCode,
        ping: player.ping,
        firstJoinDate: player.firstJoinDate,
        lastJoinDate: player.lastJoinDate,
        privileges: player.privileges,
        connectionState: player.connectionState,
    };
}

const PlayersPage: React.FC = () => {
    const dispatch = useAppDispatch();
    const players = useAppSelector((state) => state.players.players as PlayerDTO[]);
    const loading = useAppSelector((state) => state.players.loading as boolean);

    useEffect(() => {
        dispatch(fetchAllPlayers());
    }, [dispatch]);

    const handleKick = async (playerId: string) => {
        handleKickClick(playerId);
    };

    const handleRefresh = () => {
        dispatch(fetchAllPlayers());
    };

    const [kickDialogOpen, setKickDialogOpen] = React.useState(false);
    const [kickTargetId, setKickTargetId] = React.useState<string | null>(null);
    const [kickReason, setKickReason] = React.useState('Kicked by an administrator.');

    const handleKickClick = (playerId: string) => {
        setKickTargetId(playerId);
        setKickReason('Kicked by an administrator.');
        setKickDialogOpen(true);
    };

    const handleKickConfirm = async () => {
        if (kickTargetId) {
            try {
                await PlayerService.kickPlayer(kickTargetId, kickReason);
                dispatch(fetchAllPlayers());
                setKickDialogOpen(false);
            } catch (err) {
                alert('Failed to kick player.');
            }
        }
    };

    const handleKickCancel = () => {
        setKickDialogOpen(false);
        setKickTargetId(null);
        setKickReason('Kicked by an administrator.');
    };

    const [tabValue, setTabValue] = React.useState(0);
    const [paginationModel, setPaginationModel] = React.useState({ page: 0, pageSize: 20 });
    const [whitelistedPlayers, setWhitelistedPlayers] = React.useState<PlayerDTO[]>([]);
    const [bannedPlayers, setBannedPlayers] = React.useState<PlayerDTO[]>([]);
    const [whitelistedLoading, setWhitelistedLoading] = React.useState(false);
    const [bannedLoading, setBannedLoading] = React.useState(false);

    const fetchWhitelistedPlayers = async () => {
        setWhitelistedLoading(true);
        try {
            const data = await PlayerService.getWhitelistedPlayers();
            setWhitelistedPlayers(data);
        } catch (err) {
            console.error('Failed to fetch whitelisted players', err);
        } finally {
            setWhitelistedLoading(false);
        }
    };

    const fetchBannedPlayers = async () => {
        setBannedLoading(true);
        try {
            const data = await PlayerService.getBannedPlayers();
            setBannedPlayers(data);
        } catch (err) {
            console.error('Failed to fetch banned players', err);
        } finally {
            setBannedLoading(false);
        }
    };

    const getDisplayPlayers = () => {
        if (tabValue === 0) return players;
        if (tabValue === 1) return players.filter(p => p.connectionState === 'Connected');
        if (tabValue === 2) return whitelistedPlayers;
        if (tabValue === 3) return bannedPlayers;
        return [];
    };

    const getDisplayLoading = () => {
        if (tabValue === 0) return loading;
        if (tabValue === 1) return loading;
        if (tabValue === 2) return whitelistedLoading;
        if (tabValue === 3) return bannedLoading;
        return false;
    };

    const columns: GridColDef[] = [
        {
            field: 'name',
            headerName: 'Player',
            flex: 1,
            minWidth: 140,
            renderCell: (params: GridRenderCellParams) => (
                <Box display="flex" alignItems="center" gap={1}>
                    <Avatar sx={{ width: 24, height: 24 }}>
                        {String(params.value)[0]}
                    </Avatar>
                    {String(params.value).substring(0, 15)}
                </Box>
            ),
        },
        {
            field: 'isAdmin',
            headerName: 'Role',
            width: 90,
            renderCell: (params: GridRenderCellParams) => (
                <Chip
                    label={params.value ? 'Admin' : 'Player'}
                    color={params.value ? 'error' : 'default'}
                    size="small"
                />
            ),
        },
        {
            field: 'connectionState',
            headerName: 'Status',
            width: 110,
            renderCell: (params: GridRenderCellParams) => (
                <Chip
                    label={params.value || 'Offline'}
                    color={params.value === 'Connected' ? 'success' : 'default'}
                    size="small"
                    variant="outlined"
                />
            ),
        },
        {
            field: 'ping',
            headerName: 'Ping',
            width: 70,
            align: 'center' as const,
        },
        {
            field: 'languageCode',
            headerName: 'Lang',
            width: 70,
        },
        {
            field: 'actions',
            headerName: 'Actions',
            width: 130,
            sortable: false,
            renderCell: (params: GridRenderCellParams) => (
                <Box display="flex" gap={0.5}>
                    <Button
                        size="small"
                        variant="outlined"
                        onClick={() => handleKick(params.row.id)}
                        sx={{ fontSize: '0.7rem', padding: '4px 8px' }}
                    >
                        Kick
                    </Button>
                    <Button
                        size="small"
                        variant="outlined"
                        color="error"
                        sx={{ padding: '4px 8px' }}
                    >
                        <BlockIcon fontSize="small" />
                    </Button>
                </Box>
            ),
        },
    ];

        const displayPlayers = getDisplayPlayers();
        const displayLoading = getDisplayLoading();
        const tablePlayers = displayPlayers.map(mapPlayerDTOtoTable);

        const handleTabChange = (_event: React.SyntheticEvent, newValue: number) => {
            setTabValue(newValue);
            setPaginationModel({ page: 0, pageSize: 20 });
            if (newValue === 2 && whitelistedPlayers.length === 0) {
                fetchWhitelistedPlayers();
            }
            if (newValue === 3 && bannedPlayers.length === 0) {
                fetchBannedPlayers();
            }
        };

        return (
            <>
                <Box sx={{ minWidth: '100%', maxWidth: { sm: '100%', md: '1700px' } }}>
                    <Box sx={{ display: 'flex', gap: 2, alignItems: 'center', marginBottom: 2 }}>
                        <Typography variant="h4">Players</Typography>
                        <Button
                            startIcon={<RefreshIcon />}
                            onClick={() => {
                                handleRefresh();
                                if (tabValue === 2) fetchWhitelistedPlayers();
                                if (tabValue === 3) fetchBannedPlayers();
                            }}
                            disabled={displayLoading}
                        >
                            Refresh
                        </Button>
                    </Box>
                    <Box sx={{ height: 600, width: '100%' }}>
                        <Tabs value={tabValue} onChange={handleTabChange}>
                            <Tab label="All Players" />
                            <Tab label="Online" />
                            <Tab label="Whitelisted" />
                            <Tab label="Banned" />
                        </Tabs>
                        
                        <DataGrid
                            rows={tablePlayers}
                            columns={columns}
                            loading={displayLoading}
                            pagination
                            paginationModel={paginationModel}
                            onPaginationModelChange={setPaginationModel}
                            pageSizeOptions={[20, 50, 100]}
                            disableRowSelectionOnClick
                        />
                        
                    </Box>
                </Box>

                <Dialog open={kickDialogOpen} onClose={handleKickCancel}>
                    <DialogTitle>Kick Player</DialogTitle>
                    <DialogContent>
                        <DialogContentText>
                            Enter a reason for kicking this player:
                        </DialogContentText>
                        <TextField
                            autoFocus
                            margin="dense"
                            label="Reason"
                            fullWidth
                            variant="standard"
                            value={kickReason}
                            onChange={(e) => setKickReason(e.target.value)}
                        />
                    </DialogContent>
                    <DialogActions>
                        <Button onClick={handleKickCancel}>Cancel</Button>
                        <Button onClick={handleKickConfirm} variant="contained" color="error">
                            Kick
                        </Button>
                    </DialogActions>
                </Dialog>
            </>
        );
};

export default PlayersPage;