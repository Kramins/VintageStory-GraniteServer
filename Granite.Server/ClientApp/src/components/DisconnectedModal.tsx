import { Dialog, DialogTitle, DialogContent, DialogActions, Button, Alert, Box } from '@mui/material';
import CloudOffIcon from '@mui/icons-material/CloudOff';

interface DisconnectedModalProps {
    open: boolean;
    onRetry: () => void;
    reason?: string;
}

export default function DisconnectedModal({ open, onRetry, reason }: DisconnectedModalProps) {
    return (
        <Dialog
            open={open}
            maxWidth="sm"
            fullWidth
            disableEscapeKeyDown
            PaperProps={{
                sx: {
                    borderRadius: 2,
                    boxShadow: 3,
                }
            }}
        >
            <DialogTitle sx={{ display: 'flex', alignItems: 'center', gap: 1, color: 'error.main' }}>
                <CloudOffIcon />
                Server Disconnected
            </DialogTitle>
            <DialogContent>
                <Box sx={{ mt: 2, display: 'flex', flexDirection: 'column', gap: 2 }}>
                    <Alert severity="error">
                        Unable to reach the server. Please check your connection and try again.
                    </Alert>
                    {reason && (
                        <Alert severity="info">
                            <strong>Error Details:</strong> {reason}
                        </Alert>
                    )}
                    <p>
                        The server may be temporarily offline or unreachable. Click the "Retry" button below to attempt to reconnect.
                    </p>
                </Box>
            </DialogContent>
            <DialogActions sx={{ p: 2 }}>
                <Button
                    onClick={onRetry}
                    variant="contained"
                    color="primary"
                >
                    Retry Connection
                </Button>
            </DialogActions>
        </Dialog>
    );
}
