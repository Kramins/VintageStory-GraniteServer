import React from 'react';
import {
    Box,
    Card,
    CardContent,
    Typography,
    TextField,
    Button,
    List,
    ListItem,
    ListItemText,
    ListItemSecondaryAction,
    IconButton,
    Chip,
} from '@mui/material';
import {
    Gavel as GavelIcon,
    Delete as DeleteIcon,
    Add as AddIcon,
    Warning as WarningIcon,
} from '@mui/icons-material';

const mockBans = [
    { id: 1, player: 'BadPlayer123', reason: 'Griefing', date: '2024-12-10', duration: 'Permanent' },
    { id: 2, player: 'Cheater456', reason: 'Hacking', date: '2024-12-09', duration: '7 days' },
    { id: 3, player: 'Spammer789', reason: 'Spam', date: '2024-12-08', duration: '1 day' },
];

const mockWarnings = [
    { id: 1, player: 'NewPlayer', reason: 'Minor rule violation', date: '2024-12-12' },
    { id: 2, player: 'RegularPlayer', reason: 'Language warning', date: '2024-12-11' },
];

const ModerationPage: React.FC = () => {
    return (
        <Box>
            <Typography variant="h4" component="h1" gutterBottom>
                Moderation Tools
            </Typography>

            <Box display="flex" flexWrap="wrap" gap={2} mb={3}>
                <Card sx={{ minWidth: 200 }}>
                    <CardContent>
                        <Box display="flex" alignItems="center" justifyContent="space-between">
                            <Box>
                                <Typography variant="h4" component="div" color="error.main">
                                    {mockBans.length}
                                </Typography>
                                <Typography variant="subtitle1" color="text.secondary">
                                    Active Bans
                                </Typography>
                            </Box>
                            <GavelIcon sx={{ fontSize: 40, color: 'error.main' }} />
                        </Box>
                    </CardContent>
                </Card>

                <Card sx={{ minWidth: 200 }}>
                    <CardContent>
                        <Box display="flex" alignItems="center" justifyContent="space-between">
                            <Box>
                                <Typography variant="h4" component="div" color="warning.main">
                                    {mockWarnings.length}
                                </Typography>
                                <Typography variant="subtitle1" color="text.secondary">
                                    Recent Warnings
                                </Typography>
                            </Box>
                            <WarningIcon sx={{ fontSize: 40, color: 'warning.main' }} />
                        </Box>
                    </CardContent>
                </Card>
            </Box>

            <Box display="flex" flexWrap="wrap" gap={2} mb={3}>
                <Box flex={1} minWidth={300}>
                    <Card>
                        <CardContent>
                            <Typography variant="h6" gutterBottom>
                                Quick Actions
                            </Typography>
                            <Box display="flex" flexDirection="column" gap={2}>
                                <TextField
                                    label="Player Name"
                                    variant="outlined"
                                    size="small"
                                    fullWidth
                                />
                                <TextField
                                    label="Reason"
                                    variant="outlined"
                                    size="small"
                                    fullWidth
                                />
                                <Box display="flex" gap={1} flexWrap="wrap">
                                    <Button variant="contained" color="warning" size="small">
                                        Warn
                                    </Button>
                                    <Button variant="contained" color="error" size="small">
                                        Kick
                                    </Button>
                                    <Button variant="contained" color="error" size="small">
                                        Ban
                                    </Button>
                                    <Button variant="outlined" size="small">
                                        Mute
                                    </Button>
                                </Box>
                            </Box>
                        </CardContent>
                    </Card>
                </Box>

                <Box flex="0 0 300px">
                    <Card>
                        <CardContent>
                            <Typography variant="h6" gutterBottom>
                                Server Rules
                            </Typography>
                            <Box display="flex" flexDirection="column" gap={1}>
                                <Chip label="No Griefing" variant="outlined" size="small" />
                                <Chip label="No Cheating" variant="outlined" size="small" />
                                <Chip label="Respect Players" variant="outlined" size="small" />
                                <Chip label="No Spam" variant="outlined" size="small" />
                                <Chip label="English Only" variant="outlined" size="small" />
                            </Box>
                        </CardContent>
                    </Card>
                </Box>
            </Box>

            <Box display="flex" flexWrap="wrap" gap={2}>
                <Box flex={1} minWidth={400}>
                    <Card>
                        <CardContent>
                            <Box display="flex" justifyContent="space-between" alignItems="center" mb={2}>
                                <Typography variant="h6">
                                    Active Bans
                                </Typography>
                                <Button startIcon={<AddIcon />} size="small">
                                    Add Ban
                                </Button>
                            </Box>
                            <List>
                                {mockBans.map((ban) => (
                                    <ListItem key={ban.id} divider>
                                        <ListItemText
                                            primary={ban.player}
                                            secondary={`${ban.reason} • ${ban.date} • ${ban.duration}`}
                                        />
                                        <ListItemSecondaryAction>
                                            <IconButton edge="end" color="error">
                                                <DeleteIcon />
                                            </IconButton>
                                        </ListItemSecondaryAction>
                                    </ListItem>
                                ))}
                            </List>
                        </CardContent>
                    </Card>
                </Box>

                <Box flex={1} minWidth={400}>
                    <Card>
                        <CardContent>
                            <Box display="flex" justifyContent="space-between" alignItems="center" mb={2}>
                                <Typography variant="h6">
                                    Recent Warnings
                                </Typography>
                                <Button startIcon={<AddIcon />} size="small">
                                    Add Warning
                                </Button>
                            </Box>
                            <List>
                                {mockWarnings.map((warning) => (
                                    <ListItem key={warning.id} divider>
                                        <ListItemText
                                            primary={warning.player}
                                            secondary={`${warning.reason} • ${warning.date}`}
                                        />
                                        <ListItemSecondaryAction>
                                            <IconButton edge="end" color="error">
                                                <DeleteIcon />
                                            </IconButton>
                                        </ListItemSecondaryAction>
                                    </ListItem>
                                ))}
                            </List>
                        </CardContent>
                    </Card>
                </Box>
            </Box>
        </Box>
    );
};

export default ModerationPage;