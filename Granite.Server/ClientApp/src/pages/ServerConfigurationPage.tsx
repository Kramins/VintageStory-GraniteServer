import React, { useEffect, useState } from 'react';
import {
    Box,
    Card,
    CardContent,
    CardHeader,
    TextField,
    Button,
    Switch,
    FormControlLabel,
    Typography,
    CircularProgress,
    Alert,
    Stack,
    Select,
    MenuItem,
    FormControl,
    InputLabel,
    Divider,
    Paper,
} from '@mui/material';
import {
    Sync as SyncIcon,
    Save as SaveIcon,
    Refresh as RefreshIcon,
    Settings as SettingsIcon,
    Security as SecurityIcon,
    Gamepad as GamepadIcon,
} from '@mui/icons-material';
import { useAppDispatch, useAppSelector } from '../store/store';
import { fetchServerConfig, updateServerConfig, syncServerConfig } from '../store/slices/serverConfigSlice';
import type { ServerConfigDTO } from '../types/ServerConfigDTO';

const ServerConfigurationPage: React.FC = () => {
    const dispatch = useAppDispatch();
    const { config, loading, error, isSaving } = useAppSelector(state => state.serverConfig);
    const selectedServerId = useAppSelector(state => state.servers.selectedServerId);
    const [formData, setFormData] = useState<ServerConfigDTO>({});

    useEffect(() => {
        if (selectedServerId) {
            dispatch(fetchServerConfig(selectedServerId));
        }
    }, [dispatch, selectedServerId]);

    useEffect(() => {
        if (config) {
            setFormData(config);
        }
    }, [config]);

    const handleInputChange = (field: keyof ServerConfigDTO, value: any) => {
        setFormData(prev => ({
            ...prev,
            [field]: value,
        }));
    };

    const handleSubmit = () => {
        if (selectedServerId) {
            dispatch(updateServerConfig(selectedServerId, formData));
        }
    };

    const handleSync = () => {
        if (selectedServerId) {
            dispatch(syncServerConfig(selectedServerId));
        }
    };

    const hasChanges = JSON.stringify(formData) !== JSON.stringify(config);

    if (!selectedServerId) {
        return (
            <Box display="flex" justifyContent="center" alignItems="center" minHeight="400px">
                <Alert severity="info">Please select a server to view its configuration.</Alert>
            </Box>
        );
    }

    if (loading) {
        return (
            <Box display="flex" justifyContent="center" alignItems="center" minHeight="400px">
                <CircularProgress />
            </Box>
        );
    }

    return (
        <Box sx={{ maxWidth: 900, mx: 'auto', py: 3 }}>
            <Card elevation={2}>
                <CardHeader
                    title={
                        <Box sx={{ display: 'flex', alignItems: 'center', gap: 1 }}>
                            <SettingsIcon />
                            <Typography variant="h5" component="span">
                                Server Configuration
                            </Typography>
                        </Box>
                    }
                    subheader="Manage server settings and game rules"
                    sx={{ pb: 1 }}
                />
                <Divider />
                <CardContent sx={{ p: 3 }}>
                    <Stack spacing={4}>
                        {error && (
                            <Alert severity="error" variant="filled">
                                {error}
                            </Alert>
                        )}

                        {/* Basic Settings Section */}
                        <Paper elevation={0} sx={{ p: 3, bgcolor: 'background.default', borderRadius: 2 }}>
                            <Typography variant="h6" gutterBottom sx={{ display: 'flex', alignItems: 'center', gap: 1, mb: 3 }}>
                                <SettingsIcon color="primary" />
                                Basic Settings
                            </Typography>
                            <Stack spacing={3}>
                                <TextField
                                    fullWidth
                                    label="Server Name"
                                    value={formData.serverName || ''}
                                    onChange={e => handleInputChange('serverName', e.target.value)}
                                    disabled={isSaving}
                                    variant="outlined"
                                />

                                <TextField
                                    fullWidth
                                    multiline
                                    rows={3}
                                    label="Welcome Message"
                                    value={formData.welcomeMessage || ''}
                                    onChange={e => handleInputChange('welcomeMessage', e.target.value)}
                                    disabled={isSaving}
                                    placeholder="Message displayed when players join the server"
                                    variant="outlined"
                                />

                                <Box sx={{ display: 'flex', gap: 2, flexDirection: { xs: 'column', sm: 'row' } }}>
                                    <TextField
                                        fullWidth
                                        type="number"
                                        label="Port"
                                        value={formData.port || ''}
                                        onChange={e => handleInputChange('port', e.target.value ? parseInt(e.target.value) : undefined)}
                                        disabled
                                        helperText="Port cannot be changed after server start"
                                        variant="outlined"
                                    />

                                    <TextField
                                        fullWidth
                                        type="number"
                                        label="Max Players"
                                        value={formData.maxClients || ''}
                                        onChange={e => handleInputChange('maxClients', e.target.value ? parseInt(e.target.value) : undefined)}
                                        disabled={isSaving}
                                        inputProps={{ min: 1, max: 100 }}
                                        variant="outlined"
                                    />
                                </Box>

                                <Box sx={{ display: 'flex', gap: 2, flexDirection: { xs: 'column', sm: 'row' } }}>
                                    <TextField
                                        fullWidth
                                        type="number"
                                        label="Max Chunk Radius"
                                        value={formData.maxChunkRadius || ''}
                                        onChange={e => handleInputChange('maxChunkRadius', e.target.value ? parseInt(e.target.value) : undefined)}
                                        disabled={isSaving}
                                        helperText="Horizontal distance in chunks from each player"
                                        inputProps={{ min: 1, max: 32 }}
                                        variant="outlined"
                                    />

                                    <FormControl fullWidth disabled={isSaving} variant="outlined">
                                        <InputLabel>Whitelist Mode</InputLabel>
                                        <Select
                                            value={formData.whitelistMode || ''}
                                            label="Whitelist Mode"
                                            onChange={e => handleInputChange('whitelistMode', e.target.value)}
                                        >
                                            <MenuItem value="">Default</MenuItem>
                                            <MenuItem value="Off">Off</MenuItem>
                                            <MenuItem value="On">On</MenuItem>
                                        </Select>
                                    </FormControl>
                                </Box>
                            </Stack>
                        </Paper>

                        {/* Security Section */}
                        <Paper elevation={0} sx={{ p: 3, bgcolor: 'background.default', borderRadius: 2 }}>
                            <Typography variant="h6" gutterBottom sx={{ display: 'flex', alignItems: 'center', gap: 1, mb: 3 }}>
                                <SecurityIcon color="primary" />
                                Security
                            </Typography>
                            <TextField
                                fullWidth
                                type="password"
                                label="Server Password"
                                value={formData.password || ''}
                                onChange={e => handleInputChange('password', e.target.value)}
                                disabled={isSaving}
                                placeholder="Leave empty for public server"
                                variant="outlined"
                                helperText="Set a password to restrict access to your server"
                            />
                        </Paper>

                        {/* Game Rules Section */}
                        <Paper elevation={0} sx={{ p: 3, bgcolor: 'background.default', borderRadius: 2 }}>
                            <Typography variant="h6" gutterBottom sx={{ display: 'flex', alignItems: 'center', gap: 1, mb: 2 }}>
                                <GamepadIcon color="primary" />
                                Game Rules
                            </Typography>
                            <Stack spacing={1}>
                                <FormControlLabel
                                    control={
                                        <Switch
                                            checked={formData.allowPvP ?? false}
                                            onChange={e => handleInputChange('allowPvP', e.target.checked)}
                                            disabled={isSaving}
                                            color="primary"
                                        />
                                    }
                                    label={
                                        <Box>
                                            <Typography variant="body1">Allow PvP</Typography>
                                            <Typography variant="caption" color="text.secondary">
                                                Enable player vs player combat
                                            </Typography>
                                        </Box>
                                    }
                                />
                                <Divider sx={{ my: 1 }} />
                                <FormControlLabel
                                    control={
                                        <Switch
                                            checked={formData.allowFireSpread ?? false}
                                            onChange={e => handleInputChange('allowFireSpread', e.target.checked)}
                                            disabled={isSaving}
                                            color="primary"
                                        />
                                    }
                                    label={
                                        <Box>
                                            <Typography variant="body1">Allow Fire Spread</Typography>
                                            <Typography variant="caption" color="text.secondary">
                                                Fire can spread to nearby blocks
                                            </Typography>
                                        </Box>
                                    }
                                />
                                <Divider sx={{ my: 1 }} />
                                <FormControlLabel
                                    control={
                                        <Switch
                                            checked={formData.allowFallingBlocks ?? false}
                                            onChange={e => handleInputChange('allowFallingBlocks', e.target.checked)}
                                            disabled={isSaving}
                                            color="primary"
                                        />
                                    }
                                    label={
                                        <Box>
                                            <Typography variant="body1">Allow Falling Blocks</Typography>
                                            <Typography variant="caption" color="text.secondary">
                                                Blocks affected by gravity can fall
                                            </Typography>
                                        </Box>
                                    }
                                />
                            </Stack>
                        </Paper>

                        {/* Action Buttons */}
                        <Box
                            sx={{
                                display: 'flex',
                                gap: 2,
                                justifyContent: 'space-between',
                                pt: 2,
                                borderTop: '1px solid',
                                borderColor: 'divider',
                            }}
                        >
                            <Button
                                variant="outlined"
                                startIcon={<SyncIcon />}
                                onClick={handleSync}
                                disabled={isSaving || loading}
                                size="large"
                            >
                                Sync from Server
                            </Button>
                            <Box sx={{ display: 'flex', gap: 2 }}>
                                <Button
                                    variant="outlined"
                                    startIcon={<RefreshIcon />}
                                    onClick={() => setFormData(config || {})}
                                    disabled={!hasChanges || isSaving}
                                    size="large"
                                >
                                    Reset
                                </Button>
                                <Button
                                    variant="contained"
                                    startIcon={isSaving ? <CircularProgress size={20} color="inherit" /> : <SaveIcon />}
                                    onClick={handleSubmit}
                                    disabled={!hasChanges || isSaving}
                                    size="large"
                                >
                                    {isSaving ? 'Saving...' : 'Save Changes'}
                                </Button>
                            </Box>
                        </Box>

                        {hasChanges && (
                            <Alert severity="warning" variant="outlined">
                                You have unsaved changes. Click "Save Changes" to apply them to the server.
                            </Alert>
                        )}
                    </Stack>
                </CardContent>
            </Card>
        </Box>
    );
};

export default ServerConfigurationPage;
