import React, { useEffect } from 'react';
import {
    Box,
    Typography,
    Chip,
    Avatar,
    Button,
    useTheme,
    useMediaQuery,
} from '@mui/material';
import type { GridColDef, GridRenderCellParams } from '@mui/x-data-grid';
import { DataGrid } from '@mui/x-data-grid';
import {
    Person as PersonIcon,
    AdminPanelSettings as AdminIcon,
    Block as BlockIcon,
} from '@mui/icons-material';
import { PlayerManagementService } from '../services/PlayerManagementService';
import type { PlayerDTO } from '../types/PlayerDTO';
import { useAppDispatch, useAppSelector } from '../store/store';
import { fetchPlayers } from '../store/slices/playersSlice';

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
    const theme = useTheme();
    const isMobile = useMediaQuery(theme.breakpoints.down('md'));

    useEffect(() => {
        dispatch(fetchPlayers());
    }, [dispatch]);

    const handleKick = async (playerId: string) => {
        const reason = window.prompt('Enter a reason for kicking this player:', 'Kicked by an administrator.');
        try {
            await PlayerManagementService.kickPlayer(playerId, reason || undefined);
            dispatch(fetchPlayers()); // Refresh player list after kick
        } catch (err) {
            alert('Failed to kick player.');
        }
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
        ...(isMobile ? [] : [
            {
                field: 'ping',
                headerName: 'Ping',
                width: 70,
                align: 'center' as const,
            },
        ]),
        ...(isMobile ? [] : [
            {
                field: 'languageCode',
                headerName: 'Lang',
                width: 70,
            },
        ]),
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

    const tablePlayers = players.map(mapPlayerDTOtoTable);


    return (
        <Box>
            {/* Header Section */}
            <Box
                sx={{
                    flexShrink: 0,
                    p: 2,
                    borderBottom: '1px solid',
                    borderColor: 'divider',
                    display: 'flex',
                    justifyContent: 'space-between',
                    alignItems: 'center',
                    backgroundColor: 'background.paper',
                }}
            >
                <Typography variant="h5" component="h1" sx={{ fontWeight: 600 }}>
                    Players
                </Typography>
                <Button variant="contained" color="primary" size="small">
                    Invite Player
                </Button>
            </Box>

            {/* Scrollable Main Area */}
            <Box
                sx={{
                    width: '100%',
                }}
            >
                {/* Stats Row */}
                <Box
                    sx={{
                        display: 'grid',
                        gridTemplateColumns: {
                            xs: '1fr',
                            sm: 'repeat(2, 1fr)',
                            md: 'repeat(3, 1fr)',
                        },
                        gap: 2,
                        flexShrink: 0,
                    }}
                >
                    <Box
                        sx={{
                            p: 1.5,
                            border: '1px solid',
                            borderColor: 'divider',
                            borderRadius: 1,
                            display: 'flex',
                            justifyContent: 'space-between',
                            alignItems: 'center',
                            backgroundColor: 'background.paper',
                        }}
                    >
                        <Box>
                            <Typography variant="caption" color="text.secondary" sx={{ display: 'block', mb: 0.5 }}>
                                Total Players
                            </Typography>
                            <Typography variant="h6" sx={{ fontWeight: 600 }}>
                                {players.length}
                            </Typography>
                        </Box>
                        <PersonIcon sx={{ fontSize: 32, color: 'primary.main', opacity: 0.7 }} />
                    </Box>

                    <Box
                        sx={{
                            p: 1.5,
                            border: '1px solid',
                            borderColor: 'divider',
                            borderRadius: 1,
                            display: 'flex',
                            justifyContent: 'space-between',
                            alignItems: 'center',
                            backgroundColor: 'background.paper',
                        }}
                    >
                        <Box>
                            <Typography variant="caption" color="text.secondary" sx={{ display: 'block', mb: 0.5 }}>
                                Staff Members
                            </Typography>
                            <Typography variant="h6" sx={{ fontWeight: 600 }}>
                                {players.filter(p => p.isAdmin).length}
                            </Typography>
                        </Box>
                        <AdminIcon sx={{ fontSize: 32, color: 'warning.main', opacity: 0.7 }} />
                    </Box>

                    <Box
                        sx={{
                            p: 1.5,
                            border: '1px solid',
                            borderColor: 'divider',
                            borderRadius: 1,
                            display: 'flex',
                            justifyContent: 'space-between',
                            alignItems: 'center',
                            backgroundColor: 'background.paper',
                        }}
                    >
                        <Box>
                            <Typography variant="caption" color="text.secondary" sx={{ display: 'block', mb: 0.5 }}>
                                Online
                            </Typography>
                            <Typography variant="h6" sx={{ fontWeight: 600 }}>
                                -
                            </Typography>
                        </Box>
                        <PersonIcon sx={{ fontSize: 32, color: 'success.main', opacity: 0.7 }} />
                    </Box>
                </Box>

                {/* Data Grid */}
                <Box
                    sx={{
                        flex: 1,
                        width: '100%',
                    }}
                >
                    <DataGrid
                        rows={tablePlayers}
                        columns={columns}
                        loading={loading}
                        pageSizeOptions={[5, 10, 25]}
                        sx={{ width: '100%' }}
                    />
                </Box>
            </Box>
        </Box>
    );
};

export default PlayersPage;