import React, { useEffect, useState } from 'react';
import { useParams } from 'react-router-dom';
import { Box, Typography, Card, CardContent, CircularProgress, Alert, Table, TableBody, TableCell, TableContainer, TableHead, TableRow, Paper, TextField, IconButton, ButtonBase } from '@mui/material';
import EditIcon from '@mui/icons-material/Edit';
import SaveIcon from '@mui/icons-material/Save';
import CancelIcon from '@mui/icons-material/Cancel';
import DeleteIcon from '@mui/icons-material/Delete';
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
import CollectibleAutocomplete from '../components/CollectibleAutocomplete';
import type { InventorySlotDTO } from '../types/PlayerDetailsDTO';
import type { UpdateInventorySlotRequestDTO } from '../types/UpdateInventorySlotRequestDTO';
import type { CollectibleObjectDTO } from '../types/CollectibleObjectDTO';

interface EditingState {
    inventoryName: string;
    slotIndex: number;
}

interface EditFormData {
    name: string;
    entityId: number;
    entityClass: string;
    stackSize: number;
}

const PlayerDetailsPage: React.FC = () => {
    const { playerId } = useParams<{ playerId: string }>();
    const dispatch = useAppDispatch();
    const { playerDetails, loading, error } = useAppSelector(state => state.playerDetails);
    const { items: collectibles, loading: collectiblesLoading, error: collectiblesError } = useAppSelector(state => state.world.collectibles);
    const [removing, setRemoving] = useState<string | null>(null);
    const [removeError, setRemoveError] = useState<string | null>(null);
    const [editingSlot, setEditingSlot] = useState<EditingState | null>(null);
    const [editFormData, setEditFormData] = useState<EditFormData>({ name: '', entityClass: '', entityId: 0, stackSize: 0 });
    const [selectedCollectible, setSelectedCollectible] = useState<CollectibleObjectDTO | null>(null);
    const [saving, setSaving] = useState(false);
    const [actionLoading, setActionLoading] = useState<string | null>(null);
    const [actionError, setActionError] = useState<string | null>(null);

    useEffect(() => {
        if (playerId) {
            dispatch(fetchPlayerDetails(playerId) as any);
        }
        // Load collectibles data (will only fetch if not already loaded)
        dispatch(fetchCollectibles() as any);
    }, [playerId, dispatch]);

    const handleEditClick = (slot: InventorySlotDTO, inventoryName: string) => {
        setEditingSlot({ inventoryName, slotIndex: slot.slotIndex });
        setEditFormData({
            name: slot.name || '',
            entityClass: slot.entityClass || '',
            entityId: slot.entityId,
            stackSize: slot.stackSize,
        });
        // Try to find and select the matching collectible
        const matchingCollectible = collectibles.find(c => c.id === slot.entityId);
        setSelectedCollectible(matchingCollectible || null);
    };

    const handleSaveEdit = async () => {
        if (!playerId || !editingSlot || !selectedCollectible) return;
        
        setSaving(true);
        try {
            await PlayerService.updatePlayerInventorySlot(playerId, editingSlot.inventoryName, {
                slotIndex: editingSlot.slotIndex,
                entityClass: selectedCollectible.type,
                entityId: selectedCollectible.id,
                stackSize: editFormData.stackSize,
            } as UpdateInventorySlotRequestDTO);
            
            dispatch(fetchPlayerDetails(playerId) as any);
            setEditingSlot(null);
            setSelectedCollectible(null);
        } catch (err: any) {
            setRemoveError(err.message || 'Failed to update item');
        } finally {
            setSaving(false);
        }
    };

    const handleCancelEdit = () => {
        setEditingSlot(null);
        setSelectedCollectible(null);
    };

    const isEditing = (inventoryName: string, slotIndex: number) => 
        editingSlot?.inventoryName === inventoryName && editingSlot?.slotIndex === slotIndex;

    const handleRemoveItem = async (inventoryName: string, slotIndex: number) => {
        if (!playerId) return;
        
        const key = `${inventoryName}-${slotIndex}`;
        setRemoving(key);
        setRemoveError(null);
        try {
            await PlayerService.removeItemFromInventory(playerId, inventoryName, slotIndex);
            // Refresh player details after removal
            dispatch(fetchPlayerDetails(playerId) as any);
        } catch (err: any) {
            setRemoveError(err.message || 'Failed to remove item');
        } finally {
            setRemoving(null);
        }
    };

    const runAction = async (key: string, action: () => Promise<void>) => {
        if (!playerId) return;
        setActionLoading(key);
        setActionError(null);
        try {
            await action();
            await dispatch(fetchPlayerDetails(playerId) as any);
        } catch (err: any) {
            setActionError(err.message || 'Action failed');
        } finally {
            setActionLoading(null);
        }
    };

    const handleRefresh = async () => {
        if (!playerId) return;
        await runAction('refresh', async () => {
            await dispatch(fetchPlayerDetails(playerId) as any);
        });
    };

    const handleKick = async () => {
        if (!playerId) return;
        await runAction('kick', async () => {
            await PlayerService.kickPlayer(playerId, 'Kicked via UI');
        });
    };

    const handleBanToggle = async () => {
        if (!playerId) return;
        if (playerDetails?.isBanned) {
            await runAction('unban', async () => {
                await PlayerService.unBanPlayer(playerId);
            });
        } else {
            await runAction('ban', async () => {
                await PlayerService.banPlayer(playerId, 'Banned via UI');
            });
        }
    };

    const handleWhitelistToggle = async () => {
        if (!playerId) return;
        if (playerDetails?.isWhitelisted) {
            await runAction('unwhitelist', async () => {
                await PlayerService.unWhitelistPlayer(playerId);
            });
        } else {
            await runAction('whitelist', async () => {
                await PlayerService.whitelistPlayer(playerId);
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

    if (removeError) {
        return (
            <Box sx={{ p: 3 }}>
                <Alert severity="error" onClose={() => setRemoveError(null)}>{removeError}</Alert>
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

            {actionError && (
                <Box sx={{ mb: 2 }}>
                    <Alert severity="error" onClose={() => setActionError(null)}>
                        {actionError}
                    </Alert>
                </Box>
            )}

            {/* Player Info Cards */}
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

            {/* Inventory Section */}
            {playerDetails.inventories && Object.keys(playerDetails.inventories).length > 0 && (
                <Box sx={{ mt: 4 }}>
                    <Typography variant="h5" sx={{ mb: 2 }}>
                        Inventory
                    </Typography>
                    {Object.entries(playerDetails.inventories).map(([inventoryName, inventory]) => (
                        <Box key={inventoryName} sx={{ mb: 3 }}>
                            <Typography variant="h6" sx={{ mb: 1 }}>
                                {inventory.name}
                            </Typography>
                            <TableContainer component={Paper}>
                                <Table size="small">
                                    <TableHead>
                                        <TableRow sx={{ backgroundColor: '#f5f5f5' }}>
                                            <TableCell>Slot</TableCell>
                                            <TableCell>Item Name</TableCell>
                                            <TableCell align="right">Stack Size</TableCell>
                                            <TableCell align="center">Actions</TableCell>
                                        </TableRow>
                                    </TableHead>
                                    <TableBody>
                                        {inventory.slots.map((slot) => {
                                            const isCurrentlyEditing = isEditing(inventoryName, slot.slotIndex);
                                            return (
                                                <TableRow key={`${inventoryName}-${slot.slotIndex}`} sx={{ backgroundColor: isCurrentlyEditing ? '#f5f5f5' : 'inherit' }}>
                                                    <TableCell>{slot.slotIndex}</TableCell>
                                                    <TableCell>
                                                        {isCurrentlyEditing ? (
                                                            <CollectibleAutocomplete
                                                                value={selectedCollectible}
                                                                onChange={(value) => {
                                                                    setSelectedCollectible(value);
                                                                    if (value) {
                                                                        setEditFormData({ 
                                                                            ...editFormData, 
                                                                            name: value.name,
                                                                            entityClass: value.type,
                                                                            entityId: value.id,
                                                                        });
                                                                    }
                                                                }}
                                                                collectibles={collectibles}
                                                                loading={collectiblesLoading}
                                                                error={collectiblesError}
                                                                disabled={saving}
                                                                label="Item"
                                                                placeholder="Search items..."
                                                            />
                                                        ) : (
                                                            slot.name || '-'
                                                        )}
                                                    </TableCell>
                                                    <TableCell align="right" sx={{ width: '80px' }}>
                                                        {isCurrentlyEditing ? (
                                                            <TextField
                                                                size="small"
                                                                type="number"
                                                                inputProps={{ min: 0, max: 64 }}
                                                                value={editFormData.stackSize}
                                                                onChange={(e) => setEditFormData({ ...editFormData, stackSize: parseInt(e.target.value) || 0 })}
                                                                variant="outlined"
                                                            />
                                                        ) : (
                                                            slot.stackSize
                                                        )}
                                                    </TableCell>
                                                    <TableCell align="center">
                                                        <Box sx={{ display: 'flex', gap: 0.5, justifyContent: 'center' }}>
                                                            {isCurrentlyEditing ? (
                                                                <>
                                                                    <IconButton
                                                                        size="small"
                                                                        onClick={handleSaveEdit}
                                                                        disabled={saving || !selectedCollectible}
                                                                        color="success"
                                                                        title={!selectedCollectible ? 'Select an item first' : 'Save'}
                                                                    >
                                                                        <SaveIcon fontSize="small" />
                                                                    </IconButton>
                                                                    <IconButton
                                                                        size="small"
                                                                        onClick={handleCancelEdit}
                                                                        disabled={saving}
                                                                        title="Cancel"
                                                                    >
                                                                        <CancelIcon fontSize="small" />
                                                                    </IconButton>
                                                                </>
                                                            ) : (
                                                                <>
                                                                    <IconButton
                                                                        size="small"
                                                                        onClick={() => handleEditClick(slot, inventoryName)}
                                                                        title="Edit"
                                                                    >
                                                                        <EditIcon fontSize="small" />
                                                                    </IconButton>
                                                                    <IconButton
                                                                        size="small"
                                                                        onClick={() => handleRemoveItem(inventoryName, slot.slotIndex)}
                                                                        disabled={removing === `${inventoryName}-${slot.slotIndex}`}
                                                                        color="error"
                                                                        title="Delete"
                                                                    >
                                                                        <DeleteIcon fontSize="small" />
                                                                    </IconButton>
                                                                </>
                                                            )}
                                                        </Box>
                                                    </TableCell>
                                                </TableRow>
                                            );
                                        })}
                                    </TableBody>
                                </Table>
                            </TableContainer>
                        </Box>
                    ))}
                </Box>
            )}
        </Box>
    );
};

export default PlayerDetailsPage;


