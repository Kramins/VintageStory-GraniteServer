import React, { useEffect } from 'react';
import { useParams } from 'react-router-dom';
import { Box, Typography, Card, CardContent, CircularProgress, Alert, Table, TableBody, TableCell, TableContainer, TableHead, TableRow, Paper } from '@mui/material';
import { useAppDispatch, useAppSelector } from '../store/store';
import { fetchPlayerDetails } from '../store/slices/playerDetailsSlice';

const PlayerDetailsPage: React.FC = () => {
    const { playerId } = useParams<{ playerId: string }>();
    const dispatch = useAppDispatch();
    const { playerDetails, loading, error } = useAppSelector(state => state.playerDetails);

    useEffect(() => {
        if (playerId) {
            dispatch(fetchPlayerDetails(playerId) as any);
        }
    }, [playerId, dispatch]);

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
                                        </TableRow>
                                    </TableHead>
                                    <TableBody>
                                        {inventory.slots.map((slot) => (
                                            <TableRow key={slot.id}>
                                                <TableCell>{slot.slotIndex}</TableCell>
                                                <TableCell>{slot.name || '-'}</TableCell>
                                                <TableCell>{slot.class || '-'}</TableCell>
                                                <TableCell align="right">{slot.stackSize}</TableCell>
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


