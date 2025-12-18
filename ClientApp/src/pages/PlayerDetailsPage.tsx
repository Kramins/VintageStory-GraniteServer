import React, { useEffect, useState } from 'react';
import { useParams } from 'react-router-dom';
import { Box, Typography, Card, CardContent, CircularProgress, Alert, Table, TableBody, TableCell, TableContainer, TableHead, TableRow, Paper, Button } from '@mui/material';
import { useAppDispatch, useAppSelector } from '../store/store';
import { fetchPlayerDetails } from '../store/slices/playerDetailsSlice';
import { PlayerService } from '../services/PlayerService';

const PlayerDetailsPage: React.FC = () => {
    const { playerId } = useParams<{ playerId: string }>();
    const dispatch = useAppDispatch();
    const { playerDetails, loading, error } = useAppSelector(state => state.playerDetails);
    const [removing, setRemoving] = useState<string | null>(null);
    const [removeError, setRemoveError] = useState<string | null>(null);

    useEffect(() => {
        if (playerId) {
            dispatch(fetchPlayerDetails(playerId) as any);
        }
    }, [playerId, dispatch]);

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
        <Box sx={{ p: 3 }}>
            <Typography variant="h4" component="h1" sx={{ mb: 3 }}>
                Player Details
            </Typography>

            {/* Player Info Cards */}
            <Box sx={{ display: 'grid', gridTemplateColumns: { xs: '1fr', sm: '1fr 1fr' }, gap: 3, mb: 3 }}>
                <Card>
                    <CardContent>
                        <Typography color="textSecondary" gutterBottom>
                            Player Name
                        </Typography>
                        <Typography variant="h5">{playerDetails.name}</Typography>
                    </CardContent>
                </Card>
                <Card>
                    <CardContent>
                        <Typography color="textSecondary" gutterBottom>
                            Player ID
                        </Typography>
                        <Typography variant="h5">{playerDetails.id}</Typography>
                    </CardContent>
                </Card>
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
                                            <TableCell>Class</TableCell>
                                            <TableCell align="right">Stack Size</TableCell>
                                            <TableCell align="center">Actions</TableCell>
                                        </TableRow>
                                    </TableHead>
                                    <TableBody>
                                        {inventory.slots.map((slot) => (
                                            <TableRow key={`${inventoryName}-${slot.slotIndex}`}>
                                                <TableCell>{slot.slotIndex}</TableCell>
                                                <TableCell>{slot.name || '-'}</TableCell>
                                                <TableCell>{slot.class || '-'}</TableCell>
                                                <TableCell align="right">{slot.stackSize}</TableCell>
                                                <TableCell align="center">
                                                    <Button
                                                        size="small"
                                                        variant="outlined"
                                                        color="error"
                                                        onClick={() => handleRemoveItem(inventoryName, slot.slotIndex)}
                                                        disabled={removing === `${inventoryName}-${slot.slotIndex}`}
                                                        sx={{ fontSize: '0.7rem', padding: '4px 8px' }}
                                                    >
                                                        {removing === `${inventoryName}-${slot.slotIndex}` ? 'Removing...' : 'Remove'}
                                                    </Button>
                                                </TableCell>
                                            </TableRow>
                                        ))}
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


