import React from 'react';
import {
    Box,
    Card,
    CardContent,
    Typography,
    TextField,
    Switch,
    FormControlLabel,
    Button,
    Divider,
    Alert,
} from '@mui/material';
import {
    Settings as SettingsIcon,
    Security as SecurityIcon,
    Notifications as NotificationsIcon,
} from '@mui/icons-material';

const SettingsPage: React.FC = () => {
    return (
        <Box>
            <Typography variant="h4" component="h1" gutterBottom>
                Server Settings
            </Typography>

            <Alert severity="warning" sx={{ mb: 3 }}>
                Changes to server settings will require a restart to take effect.
            </Alert>

            <Box display="flex" flexDirection="column" gap={2}>
                <Card>
                    <CardContent>
                        <Box display="flex" alignItems="center" gap={1} mb={2}>
                            <SettingsIcon />
                            <Typography variant="h6">
                                General Settings
                            </Typography>
                        </Box>
                        <Box display="flex" flexDirection="column" gap={2}>
                            <TextField
                                label="Server Name"
                                defaultValue="Granite Vintage Story Server"
                                variant="outlined"
                                fullWidth
                            />
                            <TextField
                                label="Max Players"
                                type="number"
                                defaultValue="20"
                                variant="outlined"
                                style={{ maxWidth: 200 }}
                            />
                            <TextField
                                label="Server Description"
                                defaultValue="A friendly Vintage Story server"
                                variant="outlined"
                                multiline
                                rows={3}
                                fullWidth
                            />
                            <FormControlLabel
                                control={<Switch defaultChecked />}
                                label="Allow PvP"
                            />
                            <FormControlLabel
                                control={<Switch defaultChecked />}
                                label="Enable Whitelist"
                            />
                        </Box>
                    </CardContent>
                </Card>

                <Card>
                    <CardContent>
                        <Box display="flex" alignItems="center" gap={1} mb={2}>
                            <SecurityIcon />
                            <Typography variant="h6">
                                Security Settings
                            </Typography>
                        </Box>
                        <Box display="flex" flexDirection="column" gap={2}>
                            <TextField
                                label="RCON Password"
                                type="password"
                                defaultValue="********"
                                variant="outlined"
                                fullWidth
                            />
                            <TextField
                                label="RCON Port"
                                type="number"
                                defaultValue="25575"
                                variant="outlined"
                                style={{ maxWidth: 200 }}
                            />
                            <FormControlLabel
                                control={<Switch defaultChecked />}
                                label="Enable RCON"
                            />
                            <FormControlLabel
                                control={<Switch />}
                                label="Online Mode (Premium Only)"
                            />
                            <FormControlLabel
                                control={<Switch defaultChecked />}
                                label="Anti-Cheat Protection"
                            />
                        </Box>
                    </CardContent>
                </Card>

                <Card>
                    <CardContent>
                        <Box display="flex" alignItems="center" gap={1} mb={2}>
                            <NotificationsIcon />
                            <Typography variant="h6">
                                Notification Settings
                            </Typography>
                        </Box>
                        <Box display="flex" flexDirection="column" gap={2}>
                            <FormControlLabel
                                control={<Switch defaultChecked />}
                                label="Player Join/Leave Messages"
                            />
                            <FormControlLabel
                                control={<Switch defaultChecked />}
                                label="Death Messages"
                            />
                            <FormControlLabel
                                control={<Switch />}
                                label="Achievement Announcements"
                            />
                            <FormControlLabel
                                control={<Switch defaultChecked />}
                                label="Admin Notifications"
                            />
                        </Box>
                    </CardContent>
                </Card>

                <Divider />

                <Box display="flex" gap={2} justifyContent="flex-end">
                    <Button variant="outlined">
                        Reset to Defaults
                    </Button>
                    <Button variant="contained" color="primary">
                        Save Settings
                    </Button>
                </Box>
            </Box>
        </Box>
    );
};

export default SettingsPage;