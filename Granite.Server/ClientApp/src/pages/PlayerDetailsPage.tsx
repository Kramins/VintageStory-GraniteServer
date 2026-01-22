import React, { useEffect, useState } from 'react';
import { useParams } from 'react-router-dom';
import { Box, Typography, CircularProgress, Alert, ButtonBase, Tabs, Tab } from '@mui/material';
import RefreshIcon from '@mui/icons-material/Refresh';
import PowerSettingsNewIcon from '@mui/icons-material/PowerSettingsNew';
import BlockIcon from '@mui/icons-material/Block';
import LockOpenIcon from '@mui/icons-material/LockOpen';
import PlaylistAddCheckCircleIcon from '@mui/icons-material/PlaylistAddCheckCircle';
import PlaylistRemoveIcon from '@mui/icons-material/PlaylistRemove';
import { useAppDispatch, useAppSelector } from '../store/store';
import { fetchPlayerDetails } from '../store/slices/playerDetailsSlice';
import { fetchCollectibles } from '../store/slices/collectiblesSlice';
import { PlayerService } from '../services/PlayerService';
import PlayerOverviewTab from '../components/player/PlayerOverviewTab';
import PlayerInventoryTab from '../components/player/PlayerInventoryTab';
import PlayerPermissionsTab from '../components/player/PlayerPermissionsTab';
import PlayerSessionsTab from '../components/player/PlayerSessionsTab';

interface TabPanelProps {
    children?: React.ReactNode;
    index: number;
    value: number;
}

const TabPanel: React.FC<TabPanelProps> = ({ children, value, index }) => {
    return (
        <div role="tabpanel" hidden={value !== index}>
            {value === index && <Box sx={{ pt: 2 }}>{children}</Box>}
        </div>
    );
};

const PlayerDetailsPage: React.FC = () => {
    const { playerId, serverId } = useParams<{ playerId: string; serverId: string }>();
    const dispatch = useAppDispatch();
    const { playerDetails, loading, error } = useAppSelector(state => state.playerDetails);
    const { items: collectibles, loading: collectiblesLoading, error: collectiblesError } = useAppSelector(state => state.world.collectibles);
    const [tabValue, setTabValue] = useState(0);
    const [actionLoading, setActionLoading] = useState<string | null>(null);
    const [actionError, setActionError] = useState<string | null>(null);

    useEffect(() => {
        if (playerId && serverId) {
            dispatch(fetchPlayerDetails(serverId, playerId) as any);
        }
        // Load collectibles data (will only fetch if not already loaded)
        dispatch(fetchCollectibles() as any);
    }, [playerId, serverId, dispatch]);

    const runAction = async (key: string, action: () => Promise<void>) => {
        if (!playerId || !serverId) return;
        setActionLoading(key);
        setActionError(null);
        try {
            await action();
            await dispatch(fetchPlayerDetails(serverId, playerId) as any);
        } catch (err: any) {
            setActionError(err.message || 'Action failed');
        } finally {
            setActionLoading(null);
        }
    };

    const handleRefresh = async () => {
        if (!playerId || !serverId) return;
        await runAction('refresh', async () => {
            await dispatch(fetchPlayerDetails(serverId, playerId) as any);
        });
    };

    const handleKick = async () => {
        if (!playerId || !serverId) return;
        await runAction('kick', async () => {
            await PlayerService.kickPlayer(serverId, playerId, 'Kicked via UI');
        });
    };

    const handleBanToggle = async () => {
        if (!playerId || !serverId) return;
        if (playerDetails?.isBanned) {
            await runAction('unban', async () => {
                await PlayerService.unBanPlayer(serverId, playerId);
            });
        } else {
            await runAction('ban', async () => {
                await PlayerService.banPlayer(serverId, playerId, 'Banned via UI');
            });
        }
    };

    const handleWhitelistToggle = async () => {
        if (!playerId || !serverId) return;
        if (playerDetails?.isWhitelisted) {
            await runAction('unwhitelist', async () => {
                await PlayerService.unWhitelistPlayer(serverId, playerId);
            });
        } else {
            await runAction('whitelist', async () => {
                await PlayerService.whitelistPlayer(serverId, playerId);
            });
        }
    };

    if (loading) {
        return (
            <Box sx={{ display: 'flex', justifyContent: 'center', alignItems: 'center', height: '100vh' }}>
                <CircularProgress />
            </Box>
        );
    }

    if (error) {
        return (
            <Box sx={{ p: 3 }}>
                <Alert severity="error">{error}</Alert>
            </Box>
        );
    }

    if (!playerDetails) {
        return (
            <Box sx={{ p: 3 }}>
                <Alert severity="info">No player details found.</Alert>
            </Box>
        );
    }

    return (
        <Box sx={{ p: 3, width: '100%' }}>
            <Box sx={{ display: 'flex', alignItems: 'center', gap: 1, mb: 2 }}>
                <Box sx={{ flex: 1 }}>
                    <Typography variant="h4" component="h1">
                        {playerDetails.name}
                    </Typography>
                    <Typography variant="caption" color="textSecondary">
                        ID: {playerDetails.id}
                    </Typography>
                </Box>
            </Box>

            {actionError && (
                <Box sx={{ mb: 2 }}>
                    <Alert severity="error" onClose={() => setActionError(null)}>
                        {actionError}
                    </Alert>
                </Box>
            )}

            <Box
                sx={{
                    background: 'linear-gradient(180deg, #2d2f33 0%, #22232a 100%)',
                    border: '1px solid #3a3c43',
                    borderRadius: 1,
                    px: 2,
                    py: 1.25,
                    display: 'flex',
                    alignItems: 'center',
                    gap: 1.5,
                    flexWrap: 'wrap',
                    mb: 3,
                    boxShadow: '0 4px 12px rgba(0,0,0,0.35)',
                }}
            >
                <ButtonBase
                    onClick={handleRefresh}
                    disabled={!!actionLoading}
                    sx={{
                        color: '#e8e8f0',
                        px: 1.5,
                        py: 1,
                        borderRadius: 1,
                        flexDirection: 'column',
                        gap: 0.5,
                        textAlign: 'center',
                        typography: 'caption',
                        textTransform: 'none',
                        minWidth: 84,
                        backgroundColor: actionLoading === 'refresh' ? 'rgba(255,255,255,0.08)' : 'transparent',
                        '&:hover': { backgroundColor: 'rgba(255,255,255,0.08)' },
                        opacity: actionLoading && actionLoading !== 'refresh' ? 0.65 : 1,
                    }}
                >
                    <RefreshIcon fontSize="medium" />
                    <span>Refresh</span>
                </ButtonBase>

                <ButtonBase
                    onClick={handleKick}
                    disabled={!!actionLoading}
                    sx={{
                        color: '#e8e8f0',
                        px: 1.5,
                        py: 1,
                        borderRadius: 1,
                        flexDirection: 'column',
                        gap: 0.5,
                        textAlign: 'center',
                        typography: 'caption',
                        textTransform: 'none',
                        minWidth: 84,
                        backgroundColor: actionLoading === 'kick' ? 'rgba(255,255,255,0.08)' : 'transparent',
                        '&:hover': { backgroundColor: 'rgba(255,255,255,0.08)' },
                        opacity: actionLoading && actionLoading !== 'kick' ? 0.65 : 1,
                    }}
                >
                    <PowerSettingsNewIcon fontSize="medium" />
                    <span>Kick</span>
                </ButtonBase>

                <ButtonBase
                    onClick={handleBanToggle}
                    disabled={!!actionLoading}
                    sx={{
                        color: '#e8e8f0',
                        px: 1.5,
                        py: 1,
                        borderRadius: 1,
                        flexDirection: 'column',
                        gap: 0.5,
                        textAlign: 'center',
                        typography: 'caption',
                        textTransform: 'none',
                        minWidth: 84,
                        backgroundColor:
                            actionLoading === 'ban' || actionLoading === 'unban'
                                ? 'rgba(255,255,255,0.08)'
                                : 'transparent',
                        '&:hover': { backgroundColor: 'rgba(255,255,255,0.08)' },
                        opacity:
                            actionLoading && !['ban', 'unban'].includes(actionLoading)
                                ? 0.65
                                : 1,
                    }}
                >
                    {playerDetails.isBanned ? (
                        <LockOpenIcon fontSize="medium" />
                    ) : (
                        <BlockIcon fontSize="medium" />
                    )}
                    <span>{playerDetails.isBanned ? 'Unban' : 'Ban'}</span>
                </ButtonBase>

                <ButtonBase
                    onClick={handleWhitelistToggle}
                    disabled={!!actionLoading}
                    sx={{
                        color: '#e8e8f0',
                        px: 1.5,
                        py: 1,
                        borderRadius: 1,
                        flexDirection: 'column',
                        gap: 0.5,
                        textAlign: 'center',
                        typography: 'caption',
                        textTransform: 'none',
                        minWidth: 84,
                        backgroundColor:
                            actionLoading === 'whitelist' || actionLoading === 'unwhitelist'
                                ? 'rgba(255,255,255,0.08)'
                                : 'transparent',
                        '&:hover': { backgroundColor: 'rgba(255,255,255,0.08)' },
                        opacity:
                            actionLoading && !['whitelist', 'unwhitelist'].includes(actionLoading)
                                ? 0.65
                                : 1,
                    }}
                >
                    {playerDetails.isWhitelisted ? (
                        <PlaylistRemoveIcon fontSize="medium" />
                    ) : (
                        <PlaylistAddCheckCircleIcon fontSize="medium" />
                    )}
                    <span>{playerDetails.isWhitelisted ? 'Remove WL' : 'Whitelist'}</span>
                </ButtonBase>

                <Box sx={{ ml: 'auto', color: '#b8b8c5', typography: 'caption', display: 'flex', alignItems: 'center', gap: 0.5 }}>
                    <Typography variant="caption" sx={{ letterSpacing: 0.5 }}>
                        Actions
                    </Typography>
                </Box>
            </Box>

            {/* Tabs */}
            <Box sx={{ borderBottom: 1, borderColor: 'divider', mb: 2 }}>
                <Tabs value={tabValue} onChange={(_, newValue) => setTabValue(newValue)}>
                    <Tab label="Overview" />
                    <Tab label="Inventory" />
                    <Tab label="Permissions" />
                    <Tab label="Sessions" />
                </Tabs>
            </Box>

            {/* Overview Tab */}
            <TabPanel value={tabValue} index={0}>
                <PlayerOverviewTab playerDetails={playerDetails} />
            </TabPanel>

            {/* Inventory Tab */}
            <TabPanel value={tabValue} index={1}>
                {playerDetails.inventories && Object.keys(playerDetails.inventories).length > 0 ? (
                    <PlayerInventoryTab
                        playerId={playerId as string}
                        serverId={serverId as string}
                        inventories={playerDetails.inventories}
                        collectibles={collectibles}
                        collectiblesLoading={collectiblesLoading}
                        collectiblesError={collectiblesError}
                    />
                ) : (
                    <Alert severity="info">No inventories found.</Alert>
                )}
            </TabPanel>

            {/* Permissions Tab */}
            <TabPanel value={tabValue} index={2}>
                <PlayerPermissionsTab playerId={playerId as string} />
            </TabPanel>

            {/* Sessions Tab */}
            <TabPanel value={tabValue} index={3}>
                <PlayerSessionsTab playerId={playerId as string} />
            </TabPanel>
        </Box>
    );
};

export default PlayerDetailsPage;