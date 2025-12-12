import React from 'react';
import {
    Box,
    Card,
    CardContent,
    Typography,
    Button,
    Chip,
} from '@mui/material';
import {
    Public as WorldIcon,
    Save as SaveIcon,
    Restore as BackupIcon,
    Timer as TimerIcon,
} from '@mui/icons-material';

const WorldPage: React.FC = () => {
    return (
        <Box>
            <Typography variant="h4" component="h1" gutterBottom>
                World Management
            </Typography>

            <Box display="flex" flexWrap="wrap" gap={2} mb={3}>
                <Card sx={{ minWidth: 200 }}>
                    <CardContent>
                        <Box display="flex" alignItems="center" justifyContent="space-between">
                            <Box>
                                <Typography variant="h4" component="div" color="info.main">
                                    42
                                </Typography>
                                <Typography variant="subtitle1" color="text.secondary">
                                    World Age (days)
                                </Typography>
                            </Box>
                            <TimerIcon sx={{ fontSize: 40, color: 'info.main' }} />
                        </Box>
                    </CardContent>
                </Card>

                <Card sx={{ minWidth: 200 }}>
                    <CardContent>
                        <Box display="flex" alignItems="center" justifyContent="space-between">
                            <Box>
                                <Typography variant="h4" component="div" color="primary.main">
                                    1.2GB
                                </Typography>
                                <Typography variant="subtitle1" color="text.secondary">
                                    World Size
                                </Typography>
                            </Box>
                            <WorldIcon sx={{ fontSize: 40, color: 'primary.main' }} />
                        </Box>
                    </CardContent>
                </Card>

                <Card sx={{ minWidth: 200 }}>
                    <CardContent>
                        <Box display="flex" alignItems="center" justifyContent="space-between">
                            <Box>
                                <Typography variant="h4" component="div" color="success.main">
                                    15
                                </Typography>
                                <Typography variant="subtitle1" color="text.secondary">
                                    Backups
                                </Typography>
                            </Box>
                            <BackupIcon sx={{ fontSize: 40, color: 'success.main' }} />
                        </Box>
                    </CardContent>
                </Card>
            </Box>

            <Box display="flex" flexWrap="wrap" gap={2}>
                <Box flex={1} minWidth={400}>
                    <Card>
                        <CardContent>
                            <Typography variant="h6" gutterBottom>
                                World Information
                            </Typography>
                            <Box display="flex" flexDirection="column" gap={2}>
                                <Box>
                                    <Typography variant="body2" color="text.secondary">
                                        World Name:
                                    </Typography>
                                    <Typography variant="body1">
                                        Granite Valley
                                    </Typography>
                                </Box>
                                <Box>
                                    <Typography variant="body2" color="text.secondary">
                                        Seed:
                                    </Typography>
                                    <Typography variant="body1" fontFamily="monospace">
                                        12345678901234
                                    </Typography>
                                </Box>
                                <Box>
                                    <Typography variant="body2" color="text.secondary">
                                        Game Mode:
                                    </Typography>
                                    <Typography variant="body1">
                                        Survival
                                    </Typography>
                                </Box>
                                <Box>
                                    <Typography variant="body2" color="text.secondary">
                                        Difficulty:
                                    </Typography>
                                    <Chip label="Normal" color="warning" size="small" />
                                </Box>
                                <Box>
                                    <Typography variant="body2" color="text.secondary">
                                        Last Backup:
                                    </Typography>
                                    <Typography variant="body1">
                                        2 hours ago
                                    </Typography>
                                </Box>
                            </Box>
                        </CardContent>
                    </Card>
                </Box>

                <Box flex="0 0 300px">
                    <Card>
                        <CardContent>
                            <Typography variant="h6" gutterBottom>
                                World Actions
                            </Typography>
                            <Box display="flex" flexDirection="column" gap={2}>
                                <Button
                                    variant="contained"
                                    color="primary"
                                    startIcon={<SaveIcon />}
                                    fullWidth
                                >
                                    Save World
                                </Button>
                                <Button
                                    variant="outlined"
                                    color="info"
                                    startIcon={<BackupIcon />}
                                    fullWidth
                                >
                                    Create Backup
                                </Button>
                                <Button
                                    variant="outlined"
                                    color="secondary"
                                    fullWidth
                                >
                                    Reset Spawn
                                </Button>
                                <Button
                                    variant="outlined"
                                    color="warning"
                                    fullWidth
                                >
                                    Generate New Chunks
                                </Button>
                                <Button
                                    variant="outlined"
                                    color="error"
                                    fullWidth
                                >
                                    Reset World
                                </Button>
                            </Box>
                        </CardContent>
                    </Card>
                </Box>
            </Box>
        </Box>
    );
};

export default WorldPage;