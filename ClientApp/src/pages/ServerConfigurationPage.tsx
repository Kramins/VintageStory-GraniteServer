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
} from '@mui/material';
import { useAppDispatch, useAppSelector } from '../store/store';
import { fetchServerConfig, updateServerConfig } from '../store/slices/serverConfigSlice';
import type { ServerConfigDTO } from '../types/ServerConfigDTO';

const ServerConfigurationPage: React.FC = () => {
    const dispatch = useAppDispatch();
    const { config, loading, error, isSaving } = useAppSelector(state => state.serverConfig);
    const [formData, setFormData] = useState<ServerConfigDTO>({});

    useEffect(() => {
        dispatch(fetchServerConfig());
    }, [dispatch]);

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
        dispatch(updateServerConfig(formData));
    };

    const hasChanges = JSON.stringify(formData) !== JSON.stringify(config);

    if (loading) {
        return (
            <Box display="flex" justifyContent="center" alignItems="center" minHeight="400px">
                <CircularProgress />
            </Box>
        );
    }

    return (
        <Box sx={{ maxWidth: 800, mx: 'auto', py: 2 }}>
            <Card>
                <CardHeader
                    title="Server Configuration"
                    subheader="Manage server settings and game rules"
                />
                <CardContent>
                    <Stack spacing={3}>
                        {error && <Alert severity="error">{error}</Alert>}

                        {/* Server Name */}
                        <TextField
                            fullWidth
                            label="Server Name"
                            value={formData.serverName || ''}
                            onChange={e => handleInputChange('serverName', e.target.value)}
                            disabled={isSaving}
                        />

                        {/* Welcome Message */}
                        <TextField
                            fullWidth
                            label="Welcome Message"
                            value={formData.welcomeMessage || ''}
                            onChange={e => handleInputChange('welcomeMessage', e.target.value)}
                            rows={3}
                            disabled={isSaving}
                            placeholder="Message displayed when players join"
                        />

                        {/* Port */}
                        <TextField
                            fullWidth
                            type="number"
                            label="Port"
                            value={formData.port || ''}
                            onChange={e => handleInputChange('port', e.target.value ? parseInt(e.target.value) : undefined)}
                            disabled
                            helperText="Port cannot be changed after server start"
                        />

                        {/* Max Clients */}
                        <TextField
                            fullWidth
                            type="number"
                            label="Max Players"
                            value={formData.maxClients || ''}
                            onChange={e => handleInputChange('maxClients', e.target.value ? parseInt(e.target.value) : undefined)}
                            disabled={isSaving}
                            inputProps={{ min: 1 }}
                        />

                        {/* Password */}
                        <TextField
                            fullWidth
                            type="password"
                            label="Server Password"
                            value={formData.password || ''}
                            onChange={e => handleInputChange('password', e.target.value)}
                            disabled={isSaving}
                            placeholder="Leave empty for no password"
                        />

                        {/* Max Chunk Radius */}
                        <TextField
                            fullWidth
                            type="number"
                            label="Max Chunk Radius"
                            value={formData.maxChunkRadius || ''}
                            onChange={e => handleInputChange('maxChunkRadius', e.target.value ? parseInt(e.target.value) : undefined)}
                            disabled={isSaving}
                            helperText="Horizontal distance in chunks from each player to load"
                            inputProps={{ min: 1 }}
                        />

                        {/* Whitelist Mode */}
                        <FormControl fullWidth disabled={isSaving}>
                            <InputLabel>Whitelist Mode</InputLabel>
                            <Select
                                value={formData.whitelistMode || ''}
                                label="Whitelist Mode"
                                onChange={e => handleInputChange('whitelistMode', e.target.value)}
                            >
                                <MenuItem value="">Select an option</MenuItem>
                                <MenuItem value="Default">Default</MenuItem>
                                <MenuItem value="Off">Off</MenuItem>
                                <MenuItem value="On">On</MenuItem>
                            </Select>
                        </FormControl>

                        <Box sx={{ borderTop: '1px solid', borderColor: 'divider', pt: 3 }}>
                            <Typography variant="h6" gutterBottom>
                                Game Rules
                            </Typography>

                            {/* PvP */}
                            <FormControlLabel
                                control={
                                    <Switch
                                        checked={formData.allowPvP ?? false}
                                        onChange={e => handleInputChange('allowPvP', e.target.checked)}
                                        disabled={isSaving}
                                    />
                                }
                                label="Allow PvP"
                            />

                            {/* Fire Spread */}
                            <FormControlLabel
                                control={
                                    <Switch
                                        checked={formData.allowFireSpread ?? false}
                                        onChange={e => handleInputChange('allowFireSpread', e.target.checked)}
                                        disabled={isSaving}
                                    />
                                }
                                label="Allow Fire Spread"
                            />

                            {/* Falling Blocks */}
                            <FormControlLabel
                                control={
                                    <Switch
                                        checked={formData.allowFallingBlocks ?? false}
                                        onChange={e => handleInputChange('allowFallingBlocks', e.target.checked)}
                                        disabled={isSaving}
                                    />
                                }
                                label="Allow Falling Blocks"
                            />
                        </Box>

                        {/* Action Buttons */}
                        <Box sx={{ display: 'flex', gap: 2, justifyContent: 'flex-end', pt: 2 }}>
                            <Button
                                variant="outlined"
                                onClick={() => setFormData(config || {})}
                                disabled={!hasChanges || isSaving}
                            >
                                Reset
                            </Button>
                            <Button
                                variant="contained"
                                onClick={handleSubmit}
                                disabled={!hasChanges || isSaving}
                            >
                                {isSaving ? <CircularProgress size={24} /> : 'Save Changes'}
                            </Button>
                        </Box>
                    </Stack>
                </CardContent>
            </Card>
        </Box>
    );
};

export default ServerConfigurationPage;
