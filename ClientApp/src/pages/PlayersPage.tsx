import React from 'react';
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

const mockPlayers = [
    { id: 1, name: 'Steve', status: 'online', role: 'admin', playtime: '120h', lastSeen: 'Now' },
    { id: 2, name: 'Alex', status: 'online', role: 'player', playtime: '85h', lastSeen: 'Now' },
    { id: 3, name: 'Herobrine', status: 'offline', role: 'player', playtime: '45h', lastSeen: '2h ago' },
    { id: 4, name: 'Notch', status: 'online', role: 'moderator', playtime: '200h', lastSeen: 'Now' },
    { id: 5, name: 'Enderman', status: 'offline', role: 'player', playtime: '12h', lastSeen: '1d ago' },
];

const PlayersPage: React.FC = () => {
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
                                    {mockPlayers.filter(p => p.status === 'online').length}
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
                                    {mockPlayers.length}
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
                                    {mockPlayers.filter(p => p.role === 'admin' || p.role === 'moderator').length}
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
                                <TableCell>Status</TableCell>
                                <TableCell>Role</TableCell>
                                <TableCell>Playtime</TableCell>
                                <TableCell>Last Seen</TableCell>
                                <TableCell>Actions</TableCell>
                            </TableRow>
                        </TableHead>
                        <TableBody>
                            {mockPlayers.map((player) => (
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
                                        <Chip
                                            label={player.status}
                                            color={player.status === 'online' ? 'success' : 'default'}
                                            size="small"
                                        />
                                    </TableCell>
                                    <TableCell>
                                        <Chip
                                            label={player.role}
                                            color={
                                                player.role === 'admin' ? 'error' :
                                                    player.role === 'moderator' ? 'warning' : 'default'
                                            }
                                            size="small"
                                        />
                                    </TableCell>
                                    <TableCell>{player.playtime}</TableCell>
                                    <TableCell>{player.lastSeen}</TableCell>
                                    <TableCell>
                                        <Box display="flex" gap={1}>
                                            <Button size="small" variant="outlined">
                                                Kick
                                            </Button>
                                            <Button size="small" variant="outlined" color="error">
                                                <BlockIcon fontSize="small" />
                                            </Button>
                                        </Box>
                                    </TableCell>
                                </TableRow>
                            ))}
                        </TableBody>
                    </Table>
                </CardContent>
            </Card>
        </Box>
    );
};

export default PlayersPage;