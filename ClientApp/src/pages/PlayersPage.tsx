import React, { useEffect, useState } from 'react';
import {
    Box,
    Card,
    CardContent,
    Typography,
    Table,
    TableHead,
    TableBody,
    TableRow,
    TableCell,
    Chip,
    Avatar,
    Button,
} from '@mui/material';
import {
    Person as PersonIcon,
    AdminPanelSettings as AdminIcon,
    Block as BlockIcon,
} from '@mui/icons-material';

import { PlayerManagementService } from '../services/PlayerManagementService';
import type { PlayerDTO } from '../types/PlayerDTO';

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
    };
}

const PlayersPage: React.FC = () => {
    const [players, setPlayers] = useState<PlayerDTO[]>([]);
    const [loading, setLoading] = useState(true);

    useEffect(() => {
        PlayerManagementService.getPlayers()
            .then(setPlayers)
            .finally(() => setLoading(false));
    }, []);

    const handleKick = async (playerId: string) => {
        const reason = window.prompt('Enter a reason for kicking this player:', 'Kicked by an administrator.');
        try {
            await PlayerManagementService.kickPlayer(playerId, reason || undefined);
            // Refresh player list after kick
            setLoading(true);
            const updated = await PlayerManagementService.getPlayers();
            setPlayers(updated);
        } catch (err) {
            alert('Failed to kick player.');
        } finally {
            setLoading(false);
        }
    };

    const tablePlayers = players.map(mapPlayerDTOtoTable);

    return (
        <Box>
            <Box display="flex" justifyContent="space-between" alignItems="center" mb={3}>
                <Typography variant="h4" component="h1">
                    Player Management
                </Typography>
                <Button variant="contained" color="primary">
                    Invite Player
                </Button>
            </Box>

            <Box display="flex" flexWrap="wrap" gap={2} mb={3}>
                <Card sx={{ minWidth: 200 }}>
                    <CardContent>
                        <Box display="flex" alignItems="center" justifyContent="space-between">
                            <Box>
                                <Typography variant="h4" component="div" color="success.main">
                                    {/* No online status in DTO */}
                                    -
                                </Typography>
                                <Typography variant="subtitle1" color="text.secondary">
                                    Online Players
                                </Typography>
                            </Box>
                            <PersonIcon sx={{ fontSize: 40, color: 'success.main' }} />
                        </Box>
                    </CardContent>
                </Card>

                <Card sx={{ minWidth: 200 }}>
                    <CardContent>
                        <Box display="flex" alignItems="center" justifyContent="space-between">
                            <Box>
                                <Typography variant="h4" component="div" color="primary.main">
                                    {players.length}
                                </Typography>
                                <Typography variant="subtitle1" color="text.secondary">
                                    Total Players
                                </Typography>
                            </Box>
                            <PersonIcon sx={{ fontSize: 40, color: 'primary.main' }} />
                        </Box>
                    </CardContent>
                </Card>

                <Card sx={{ minWidth: 200 }}>
                    <CardContent>
                        <Box display="flex" alignItems="center" justifyContent="space-between">
                            <Box>
                                <Typography variant="h4" component="div" color="warning.main">
                                    {players.filter(p => p.isAdmin).length}
                                </Typography>
                                <Typography variant="subtitle1" color="text.secondary">
                                    Staff Members
                                </Typography>
                            </Box>
                            <AdminIcon sx={{ fontSize: 40, color: 'warning.main' }} />
                        </Box>
                    </CardContent>
                </Card>
            </Box>

            <Card>
                <CardContent>
                    <Typography variant="h6" gutterBottom>
                        Player List
                    </Typography>
                    <Table>
                        <TableHead>
                            <TableRow>
                                <TableCell>Player</TableCell>
                                <TableCell>Admin</TableCell>
                                <TableCell>IP Address</TableCell>
                                <TableCell>Language</TableCell>
                                <TableCell>Ping</TableCell>
                                <TableCell>First Join</TableCell>
                                <TableCell>Last Join</TableCell>
                                <TableCell>Privileges</TableCell>
                                <TableCell>Actions</TableCell>
                            </TableRow>
                        </TableHead>
                        <TableBody>
                            {loading ? (
                                <TableRow>
                                    <TableCell colSpan={9} align="center">Loading...</TableCell>
                                </TableRow>
                            ) : (
                                tablePlayers.map((player) => (
                                    <TableRow key={player.id}>
                                        <TableCell>
                                            <Box display="flex" alignItems="center" gap={1}>
                                                <Avatar sx={{ width: 24, height: 24 }}>
                                                    {player.name[0]}
                                                </Avatar>
                                                {player.name}
                                            </Box>
                                        </TableCell>
                                        <TableCell>
                                            {player.isAdmin ? (
                                                <Chip label="Admin" color="error" size="small" />
                                            ) : (
                                                <Chip label="Player" size="small" />
                                            )}
                                        </TableCell>
                                        <TableCell>{player.ipAddress}</TableCell>
                                        <TableCell>{player.languageCode}</TableCell>
                                        <TableCell>{player.ping}</TableCell>
                                        <TableCell>{player.firstJoinDate}</TableCell>
                                        <TableCell>{player.lastJoinDate}</TableCell>
                                        <TableCell>{player.privileges?.length ?? 0}</TableCell>
                                        <TableCell>
                                            <Box display="flex" gap={1}>
                                                <Button size="small" variant="outlined" onClick={() => handleKick(player.id)}>
                                                    Kick
                                                </Button>
                                                <Button size="small" variant="outlined" color="error">
                                                    <BlockIcon fontSize="small" />
                                                </Button>
                                            </Box>
                                        </TableCell>
                                    </TableRow>
                                ))
                            )}
                        </TableBody>
                    </Table>
                </CardContent>
            </Card>
        </Box>
    );
};

export default PlayersPage;