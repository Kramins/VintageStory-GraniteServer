import React, { useEffect, useState } from 'react';
import ServerService from '../services/ServerService';
import WorldService from '../services/WorldService';
import { useToast } from '../components/ToastProvider';
import type { ServerStatusDTO } from '../types/ServerStatusDTO';
import {
    Box,
    Card,
    CardContent,
    Typography,
    Paper,
    Chip,
} from '@mui/material';
import {
    People as PlayersIcon,
    Computer as ServerIcon,
    AccessTime as UptimeIcon,
    Memory as MemoryIcon,
} from '@mui/icons-material';
import AnnounceModal from '../components/AnnounceModal';

const StatCard: React.FC<{
    title: string;
    value: string;
    icon: React.ReactNode;
    color?: string;
}> = ({ title, value, icon, color = 'primary.main' }) => (
    <Card sx={{ minWidth: 200 }}>
        <CardContent>
            <Box display="flex" alignItems="center" justifyContent="space-between">
                <Box>
                    <Typography variant="h4" component="div" sx={{ color }}>
                        {value}
                    </Typography>
                    <Typography variant="subtitle1" color="text.secondary">
                        {title}
                    </Typography>
                </Box>
                <Box sx={{ color, fontSize: 40 }}>
                    {icon}
                </Box>
            </Box>
        </CardContent>
    </Card>
);

const OverviewPage: React.FC = () => {
    const toast = useToast();
    const [status, setStatus] = useState<ServerStatusDTO | null>(null);
    const [loading, setLoading] = useState(false); // Changed to false - no longer loading status on mount
    const [error, setError] = useState<string | null>(null);
    const [saving, setSaving] = useState(false);
    const [announceOpen, setAnnounceOpen] = useState(false);

    // TODO: Update to use multi-server architecture with selected serverId
    // The old /api/server/status endpoint no longer exists
    // useEffect(() => {
    //     ServerService.getStatus()
    //         .then(data => {
    //             setStatus(data);
    //             setLoading(false);
    //         })
    //         .catch(() => {
    //             setError('Failed to load server status');
    //             setLoading(false);
    //         });
    // }, []);

    const handleSaveWorld = async () => {
        if (saving) return;
        setSaving(true);
        try {
            await WorldService.saveNow();
            toast.show('World saved successfully.', 'success');
        } catch (e: any) {
            if (e?.response?.status === 401) {
                toast.show('Please log in to save the world.', 'info');
            } else {
                toast.show('Failed to save world. Please try again.', 'error');
            }
        } finally {
            setSaving(false);
        }
    };

    const handleAnnounce = async (message: string) => {
        try {
            await ServerService.announce(message);
            toast.show('Message announced successfully.', 'success');
        } catch (e: any) {
            toast.show('Failed to announce message. Please try again.', 'error');
        }
    };

    // Temporarily disable these checks until multi-server architecture is fully implemented
    // TODO: Once multi-server support is added with serverId, re-enable status fetching
    if (!status) {
        return (
            <Box>
                <Typography variant="h4" component="h1" gutterBottom>
                    Server Overview
                </Typography>
                <Paper sx={{ p: 3, mt: 2 }}>
                    <Typography variant="body1" color="text.secondary">
                        Server overview is being updated to support multiple game servers.
                        Please select a server from the navigation to view its status.
                    </Typography>
                </Paper>
            </Box>
        );
    }

    // Helper formatting
    const formatUptime = (seconds: number) => {
        const d = Math.floor(seconds / 86400);
        const h = Math.floor((seconds % 86400) / 3600);
        const m = Math.floor((seconds % 3600) / 60);
        return `${d > 0 ? d + 'd ' : ''}${h}h ${m}m`;
    };
    const formatMemory = (bytes: number) => {
        return (bytes / (1024 * 1024 * 1024)).toFixed(2) + ' GB';
    };

    return (
        <Box>
            <Typography variant="h4" component="h1" gutterBottom>
                Server Overview
            </Typography>

            <Box display="flex" flexWrap="wrap" gap={2} mb={3}>
                <StatCard
                    title="Online Players"
                    value={status.currentPlayers.toString()}
                    icon={<PlayersIcon />}
                    color="success.main"
                />
                <StatCard
                    title="Server Status"
                    value={status.isOnline ? 'Online' : 'Offline'}
                    icon={<ServerIcon />}
                    color={status.isOnline ? 'success.main' : 'error.main'}
                />
                <StatCard
                    title="Uptime"
                    value={formatUptime(status.upTime)}
                    icon={<UptimeIcon />}
                    color="info.main"
                />
                <StatCard
                    title="Memory Usage"
                    value={formatMemory(status.memoryUsageBytes)}
                    icon={<MemoryIcon />}
                    color="warning.main"
                />
            </Box>

            <Box display="flex" flexWrap="wrap" gap={2}>
                <Box flex={1} minWidth={400}>
                    <Card>
                        <CardContent>
                            <Typography variant="h6" gutterBottom>
                                Server Information
                            </Typography>
                            <Paper sx={{ p: 2, bgcolor: 'background.default' }}>
                                <Box display="grid" gridTemplateColumns="1fr 1fr" gap={2}>
                                    <Box>
                                        <Typography variant="body2" color="text.secondary">
                                            Server Name:
                                        </Typography>
                                        <Typography variant="body1">
                                            {status.serverName}
                                        </Typography>
                                    </Box>
                                    <Box>
                                        <Typography variant="body2" color="text.secondary">
                                            Game Version:
                                        </Typography>
                                        <Typography variant="body1">
                                            {status.gameVersion}
                                        </Typography>
                                    </Box>
                                    <Box>
                                        <Typography variant="body2" color="text.secondary">
                                            World Name:
                                        </Typography>
                                        <Typography variant="body1">
                                            {status.worldName}
                                        </Typography>
                                    </Box>
                                    <Box>
                                        <Typography variant="body2" color="text.secondary">
                                            World Seed:
                                        </Typography>
                                        <Typography variant="body1">
                                            {status.worldSeed}
                                        </Typography>
                                    </Box>
                                    <Box>
                                        <Typography variant="body2" color="text.secondary">
                                            World Age:
                                        </Typography>
                                        <Typography variant="body1">
                                            {status.worldAgeDays} days
                                        </Typography>
                                    </Box>
                                    <Box>
                                        <Typography variant="body2" color="text.secondary">
                                            Max Players:
                                        </Typography>
                                        <Typography variant="body1">
                                            {status.maxPlayers}
                                        </Typography>
                                    </Box>
                                </Box>
                            </Paper>
                        </CardContent>
                    </Card>
                </Box>
                {/* Quick Actions */}
                <Box flex="0 0 300px">
                    <Card>
                        <CardContent>
                            <Typography variant="h6" gutterBottom>
                                Quick Actions
                            </Typography>
                            <Box display="flex" flexDirection="column" gap={1}>
                                <Chip
                                    label="Save World"
                                    color="info"
                                    variant="outlined"
                                    clickable
                                    onClick={handleSaveWorld}
                                    disabled={saving}
                                />
                                <Chip
                                    label="Announce Message"
                                    color="primary"
                                    variant="outlined"
                                    clickable
                                    onClick={() => setAnnounceOpen(true)}
                                />
                            </Box>
                        </CardContent>
                    </Card>
                </Box>
            </Box>
            <AnnounceModal
                open={announceOpen}
                onClose={() => setAnnounceOpen(false)}
                onSubmit={handleAnnounce}
            />
        </Box>
    );
};

export default OverviewPage;