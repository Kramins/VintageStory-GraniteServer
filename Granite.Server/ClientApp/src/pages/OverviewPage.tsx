import React, { useEffect, useState } from 'react';
import { useParams } from 'react-router-dom';
import ServerService from '../services/ServerService';
import WorldService from '../services/WorldService';
import { useToast } from '../components/ToastProvider';
import { useEventBus } from '../hooks/useEventBus';
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
    const { serverId } = useParams<{ serverId: string }>();
    const toast = useToast();
    const [status, setStatus] = useState<ServerStatusDTO | null>(null);
    const [loading, setLoading] = useState(true);
    const [error, setError] = useState<string | null>(null);
    const [saving, setSaving] = useState(false);
    const [announceOpen, setAnnounceOpen] = useState(false);

    // Listen for ServerMetricsEvent to update status in real-time
    useEventBus((event) => {
        if (event.messageType === 'ServerMetricsEvent' && event.data && status) {
            setStatus(prev => {
                if (!prev) return prev;
                return {
                    ...prev,
                    currentPlayers: event.data.activePlayerCount,
                    memoryUsageBytes: Math.round(event.data.memoryUsageMB * 1024 * 1024),
                    upTime: event.data.upTimeSeconds || prev.upTime,
                };
            });
        }
    }, [status]);

    useEffect(() => {
        if (!serverId) {
            setError('Server ID is required');
            setLoading(false);
            return;
        }

        const fetchStatus = async () => {
            try {
                setLoading(true);
                setError(null);
                const data = await ServerService.getStatus(serverId);
                setStatus(data);
            } catch (e: any) {
                console.error('Failed to load server status:', e);
                setError('Failed to load server status');
                setStatus(null);
            } finally {
                setLoading(false);
            }
        };

        fetchStatus();
    }, [serverId]);

    const handleSaveWorld = async () => {
        if (saving || !serverId) return;
        setSaving(true);
        try {
            await WorldService.saveNow(serverId);
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
        if (!serverId) return;
        try {
            await ServerService.announce(serverId, message);
            toast.show('Message announced successfully.', 'success');
        } catch (e: any) {
            toast.show('Failed to announce message. Please try again.', 'error');
        }
    };

    // Temporarily disable these checks until multi-server architecture is fully implemented
    // TODO: Once multi-server support is added with serverId, re-enable status fetching
    if (loading) {
        return (
            <Box>
                <Typography variant="h4" component="h1" gutterBottom>
                    Server Overview
                </Typography>
                <Paper sx={{ p: 3, mt: 2 }}>
                    <Typography variant="body1" color="text.secondary">
                        Loading server status...
                    </Typography>
                </Paper>
            </Box>
        );
    }

    if (error || !status) {
        return (
            <Box>
                <Typography variant="h4" component="h1" gutterBottom>
                    Server Overview
                </Typography>
                <Paper sx={{ p: 3, mt: 2 }}>
                    <Typography variant="body1" color="error">
                        {error || 'Failed to load server status'}
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