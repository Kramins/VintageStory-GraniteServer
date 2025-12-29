import React, { useEffect } from 'react';
import { Link } from 'react-router-dom';
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
    IconButton,
    Menu,
    MenuItem,
    CircularProgress,
} from '@mui/material';
import type { GridColDef, GridRenderCellParams } from '@mui/x-data-grid';
import { DataGrid } from '@mui/x-data-grid';
import {
    Refresh as RefreshIcon,
    MoreVert as MoreVertIcon,
    Check as CheckIcon,
} from '@mui/icons-material';
import { PlayerService } from '../services/PlayerService'
import type { PlayerDTO } from '../types/PlayerDTO';
import { useAppDispatch } from '../store/store';
import { fetchAllPlayers } from '../store/slices/playersSlice';
import { useToast } from '../components/ToastProvider';

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
    const toast = useToast();

    useEffect(() => {
        // Load data for All Players tab from server (paged); API is 1-based
        fetchPlayersPaged(1, 20, 'id', 'asc');
        // eslint-disable-next-line react-hooks/exhaustive-deps
    }, []);

    const handleRefresh = () => {
        if (tabValue === 0) {
            fetchPlayersPaged(paginationModel.page, paginationModel.pageSize, sortField, sortDirection);
        } else {
            dispatch(fetchAllPlayers());
        }
    };

    const handleToggleWhitelist = async (playerId: string) => {
        try {
            const isWhitelisted = whitelistedPlayers.some(p => p.id === playerId);
            
            if (isWhitelisted) {
                await PlayerService.unWhitelistPlayer(playerId);
            } else {
                await PlayerService.whitelistPlayer(playerId);
            }
            
            // Refresh data based on current tab
            dispatch(fetchAllPlayers());
            if (tabValue === 2) {
                fetchWhitelistedPlayers();
            }
        } catch (err) {
            toast.show('Failed to update whitelist status.', 'error');
        }
    };

    const [kickDialogOpen, setKickDialogOpen] = React.useState(false);
    const [kickTargetId, setKickTargetId] = React.useState<string | null>(null);
    const [kickReason, setKickReason] = React.useState('Kicked by an administrator.');
    const [menuAnchor, setMenuAnchor] = React.useState<null | HTMLElement>(null);
    const [menuPlayerId, setMenuPlayerId] = React.useState<string | null>(null);

    const handleKickClick = (playerId: string) => {
        setKickTargetId(playerId);
        setKickReason('Kicked by an administrator.');
        setKickDialogOpen(true);
    };

    const handleMenuOpen = (event: React.MouseEvent<HTMLElement>, playerId: string) => {
        setMenuAnchor(event.currentTarget);
        setMenuPlayerId(playerId);
    };

    const handleMenuClose = () => {
        setMenuAnchor(null);
        setMenuPlayerId(null);
    };

    const handleKickConfirm = async () => {
        if (kickTargetId) {
            try {
                await PlayerService.kickPlayer(kickTargetId, kickReason);
                dispatch(fetchAllPlayers());
                setKickDialogOpen(false);
            } catch (err) {
                toast.show('Failed to kick player.', 'error');
            }
        }
    };

    const handleKickCancel = () => {
        setKickDialogOpen(false);
        setKickTargetId(null);
        setKickReason('Kicked by an administrator.');
    };

    const handleToggleWhitelistFromMenu = (playerId: string) => {
        handleToggleWhitelist(playerId);
        handleMenuClose();
    };

    const handleAddWhitelistOpen = () => {
        setAddWhitelistDialogOpen(true);
        setAddWhitelistSearchQuery('');
        setAddWhitelistSubmitting(false);
        setAddWhitelistSuccess(false);
    };

    const handleAddWhitelistClose = () => {
        setAddWhitelistDialogOpen(false);
        setAddWhitelistSearchQuery('');
        setAddWhitelistSubmitting(false);
        setAddWhitelistSuccess(false);
    };

    const handleAddWhitelistConfirm = async () => {
        if (addWhitelistSearchQuery.trim() === '') {
            toast.show('Please enter a player name or ID.', 'warning');
            return;
        }

        try {
            setAddWhitelistSubmitting(true);
            setAddWhitelistSuccess(false);
            const result = await PlayerService.findPlayerByName(addWhitelistSearchQuery);
            if (result && !whitelistedPlayers.some(w => w.id === result.id)) {
                await PlayerService.whitelistPlayer(result.id);
                dispatch(fetchAllPlayers());
                fetchWhitelistedPlayers();
                setAddWhitelistSubmitting(false);
                setAddWhitelistSuccess(true);
                setTimeout(() => handleAddWhitelistClose(), 600);
            } else {
                setAddWhitelistSubmitting(false);
                toast.show('Player not found or already whitelisted.', 'info');
            }
        } catch (err) {
            console.error('Failed to add player to whitelist', err);
            setAddWhitelistSubmitting(false);
            toast.show('Failed to add player to whitelist.', 'error');
        }
    };

    const [tabValue, setTabValue] = React.useState(0);
    const [paginationModel, setPaginationModel] = React.useState({ page: 0, pageSize: 20 });
    const [sortField, setSortField] = React.useState<string>('id');
    const [sortDirection, setSortDirection] = React.useState<'asc' | 'desc'>('asc');
    const [serverPlayers, setServerPlayers] = React.useState<PlayerDTO[]>([]);
    const [serverRowCount, setServerRowCount] = React.useState<number>(0);
    const [serverLoading, setServerLoading] = React.useState<boolean>(false);
    const [onlinePlayers, setOnlinePlayers] = React.useState<PlayerDTO[]>([]);
    const [onlineRowCount, setOnlineRowCount] = React.useState<number>(0);
    const [onlineLoading, setOnlineLoading] = React.useState<boolean>(false);
    const [whitelistedPlayers, setWhitelistedPlayers] = React.useState<PlayerDTO[]>([]);
    const [whitelistedRowCount, setWhitelistedRowCount] = React.useState<number>(0);
    const [bannedPlayers, setBannedPlayers] = React.useState<PlayerDTO[]>([]);
    const [bannedRowCount, setBannedRowCount] = React.useState<number>(0);
    const [whitelistedLoading, setWhitelistedLoading] = React.useState(false);
    const [bannedLoading, setBannedLoading] = React.useState(false);
    const [addWhitelistDialogOpen, setAddWhitelistDialogOpen] = React.useState(false);
    const [addWhitelistSearchQuery, setAddWhitelistSearchQuery] = React.useState('');
    const [addWhitelistSubmitting, setAddWhitelistSubmitting] = React.useState(false);
    const [addWhitelistSuccess, setAddWhitelistSuccess] = React.useState(false);

    const fetchWhitelistedPlayers = async (page = 1, pageSize = 20, sortF = 'id', sortD: 'asc' | 'desc' = 'asc') => {
        setWhitelistedLoading(true);
        try {
            const apiPage = page < 1 ? 1 : page; // API expects 1-based
            const { players, pagination } = await PlayerService.getWhitelistedPlayers(apiPage, pageSize, sortF, sortD);
            setWhitelistedPlayers(players);
            setWhitelistedRowCount(pagination?.totalCount ?? players.length);
        } catch (err) {
            console.error('Failed to fetch whitelisted players', err);
        } finally {
            setWhitelistedLoading(false);
        }
    };

    const fetchBannedPlayers = async (page = 1, pageSize = 20, sortF = 'id', sortD: 'asc' | 'desc' = 'asc') => {
        setBannedLoading(true);
        try {
            const apiPage = page < 1 ? 1 : page; // API expects 1-based
            const { players, pagination } = await PlayerService.getBannedPlayers(apiPage, pageSize, sortF, sortD);
            setBannedPlayers(players);
            setBannedRowCount(pagination?.totalCount ?? players.length);
        } catch (err) {
            console.error('Failed to fetch banned players', err);
        } finally {
            setBannedLoading(false);
        }
    };

    const fetchPlayersPaged = async (page = 1, pageSize = 20, sortF = 'id', sortD: 'asc' | 'desc' = 'asc') => {
        setServerLoading(true);
        try {
            const apiPage = page < 1 ? 1 : page; // API expects 1-based
            const { players, pagination } = await PlayerService.getAllPlayersPaged(apiPage, pageSize, sortF, sortD);
            setServerPlayers(players);
            setServerRowCount(pagination?.totalCount ?? players.length);
        } catch (err) {
            console.error('Failed to fetch paged players', err);
        } finally {
            setServerLoading(false);
        }
    };

    const fetchOnlinePlayers = async (page = 1, pageSize = 20, sortF = 'id', sortD: 'asc' | 'desc' = 'asc') => {
        setOnlineLoading(true);
        try {
            const apiPage = page < 1 ? 1 : page; // API expects 1-based
            const { players, pagination } = await PlayerService.getOnlinePlayers(apiPage, pageSize, sortF, sortD);
            setOnlinePlayers(players);
            setOnlineRowCount(pagination?.totalCount ?? players.length);
        } catch (err) {
            console.error('Failed to fetch online players', err);
        } finally {
            setOnlineLoading(false);
        }
    };

    const getDisplayPlayers = () => {
        if (tabValue === 0) return serverPlayers;
        if (tabValue === 1) return onlinePlayers;
        if (tabValue === 2) return whitelistedPlayers;
        if (tabValue === 3) return bannedPlayers;
        return [];
    };

    const getDisplayLoading = () => {
        if (tabValue === 0) return serverLoading;
        if (tabValue === 1) return onlineLoading;
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
                <Link to={`/players/${params.row.id}`} style={{ textDecoration: 'none' }}>
                    <Box display="flex" alignItems="center" gap={1} sx={{ cursor: 'pointer', '&:hover': { opacity: 0.7 } }}>
                        <Avatar sx={{ width: 24, height: 24 }}>
                            {String(params.value)[0]}
                        </Avatar>
                        {String(params.value).substring(0, 15)}
                    </Box>
                </Link>
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
            width: 80,
            sortable: false,
            renderCell: (params: GridRenderCellParams) => {
                return (
                    <IconButton
                        size="small"
                        onClick={(e) => handleMenuOpen(e, params.row.id)}
                    >
                        <MoreVertIcon fontSize="small" />
                    </IconButton>
                );
            },
        },
    ];

        const displayPlayers = getDisplayPlayers();
        const displayLoading = getDisplayLoading();
        const tablePlayers = displayPlayers.map(mapPlayerDTOtoTable);

        const handleTabChange = (_event: React.SyntheticEvent, newValue: number) => {
            setTabValue(newValue);
            setPaginationModel({ page: 0, pageSize: 20 });
            setSortField('id');
            setSortDirection('asc');
            if (newValue === 1) {
                fetchOnlinePlayers(1, 20, 'id', 'asc');
            }
            if (newValue === 2) {
                fetchWhitelistedPlayers(1, 20, 'id', 'asc');
            }
            if (newValue === 3) {
                fetchBannedPlayers(1, 20, 'id', 'asc');
            }
            if (newValue === 0) {
                fetchPlayersPaged(1, 20, 'id', 'asc');
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
                                if (tabValue === 1) fetchOnlinePlayers(paginationModel.page + 1, paginationModel.pageSize, sortField, sortDirection);
                                if (tabValue === 2) fetchWhitelistedPlayers(paginationModel.page + 1, paginationModel.pageSize, sortField, sortDirection);
                                if (tabValue === 3) fetchBannedPlayers(paginationModel.page + 1, paginationModel.pageSize, sortField, sortDirection);
                            }}
                            disabled={displayLoading}
                        >
                            Refresh
                        </Button>
                    </Box>
                    <Box sx={{ height: 600, width: '100%' }}>
                        <Box sx={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', marginBottom: 1 }}>
                            <Tabs value={tabValue} onChange={handleTabChange}>
                                <Tab label="All Players" />
                                <Tab label="Online" />
                                <Tab label="Whitelisted" />
                                <Tab label="Banned" />
                            </Tabs>
                            {tabValue === 2 && (
                                <Button
                                    variant="contained"
                                    size="small"
                                    onClick={handleAddWhitelistOpen}
                                >
                                    Add Player
                                </Button>
                            )}
                        </Box>
                        
                        <DataGrid
                            rows={tablePlayers}
                            columns={columns}
                            loading={displayLoading}
                            pagination
                            paginationModel={paginationModel}
                            onPaginationModelChange={(model) => {
                                setPaginationModel(model);
                                if (tabValue === 0) {
                                    fetchPlayersPaged(model.page + 1, model.pageSize, sortField, sortDirection);
                                } else if (tabValue === 1) {
                                    fetchOnlinePlayers(model.page + 1, model.pageSize, sortField, sortDirection);
                                } else if (tabValue === 2) {
                                    fetchWhitelistedPlayers(model.page + 1, model.pageSize, sortField, sortDirection);
                                } else if (tabValue === 3) {
                                    fetchBannedPlayers(model.page + 1, model.pageSize, sortField, sortDirection);
                                }
                            }}
                            pageSizeOptions={[20, 50, 100]}
                            sortingMode={tabValue === 0 || tabValue === 1 || tabValue === 2 || tabValue === 3 ? 'server' : undefined}
                            sortModel={tabValue === 0 || tabValue === 1 || tabValue === 2 || tabValue === 3 ? [{ field: sortField, sort: sortDirection }] : []}
                            onSortModelChange={(model) => {
                                if (model.length > 0 && (tabValue === 0 || tabValue === 1 || tabValue === 2 || tabValue === 3)) {
                                    const { field, sort } = model[0] as any;
                                    if (sort) {
                                        setSortField(field);
                                        setSortDirection(sort);
                                        if (tabValue === 0) {
                                            fetchPlayersPaged(paginationModel.page + 1, paginationModel.pageSize, field, sort);
                                        } else if (tabValue === 1) {
                                            fetchOnlinePlayers(paginationModel.page + 1, paginationModel.pageSize, field, sort);
                                        } else if (tabValue === 2) {
                                            fetchWhitelistedPlayers(paginationModel.page + 1, paginationModel.pageSize, field, sort);
                                        } else if (tabValue === 3) {
                                            fetchBannedPlayers(paginationModel.page + 1, paginationModel.pageSize, field, sort);
                                        }
                                    }
                                }
                            }}
                            rowCount={
                                tabValue === 0 ? serverRowCount : 
                                tabValue === 1 ? onlineRowCount : 
                                tabValue === 2 ? whitelistedRowCount : 
                                tabValue === 3 ? bannedRowCount : 
                                undefined
                            }
                            paginationMode={tabValue === 0 || tabValue === 1 || tabValue === 2 || tabValue === 3 ? 'server' : undefined}
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

                <Menu
                    anchorEl={menuAnchor}
                    open={Boolean(menuAnchor)}
                    onClose={handleMenuClose}
                >
                    <MenuItem onClick={() => menuPlayerId && handleKickClick(menuPlayerId)}>
                        Kick
                    </MenuItem>
                    <MenuItem onClick={() => menuPlayerId && handleToggleWhitelistFromMenu(menuPlayerId)}>
                        {whitelistedPlayers.some(p => p.id === menuPlayerId) ? 'Remove from Whitelist' : 'Add to Whitelist'}
                    </MenuItem>
                    <MenuItem onClick={() => toast.show('Ban feature - to be implemented', 'info')}>
                        Ban
                    </MenuItem>
                </Menu>

                <Dialog open={addWhitelistDialogOpen} onClose={handleAddWhitelistClose} maxWidth="sm" fullWidth>
                    <DialogTitle>Add Player to Whitelist</DialogTitle>
                    <DialogContent>
                        <Box sx={{ pt: 2 }}>
                            <TextField
                                fullWidth
                                placeholder="Search by player name or ID..."
                                value={addWhitelistSearchQuery}
                                onChange={(e) => setAddWhitelistSearchQuery(e.target.value)}
                                variant="outlined"
                                size="small"
                            />
                        </Box>
                    </DialogContent>
                    <DialogActions>
                        <Button onClick={handleAddWhitelistClose}>Cancel</Button>
                        <Button 
                            onClick={handleAddWhitelistConfirm} 
                            variant="contained" 
                            color="primary"
                            disabled={addWhitelistSubmitting || addWhitelistSuccess}
                            startIcon={addWhitelistSuccess ? <CheckIcon /> : undefined}
                        >
                            {addWhitelistSubmitting && !addWhitelistSuccess ? (
                                <CircularProgress size={18} color="inherit" />
                            ) : addWhitelistSuccess ? (
                                'Added'
                            ) : (
                                'Add'
                            )}
                        </Button>
                    </DialogActions>
                </Dialog>
            </>
        );
};

export default PlayersPage;