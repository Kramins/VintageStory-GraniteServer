import React, { useState } from 'react';
import {
    Dialog,
    DialogActions,
    DialogContent,
    DialogTitle,
    Button,
    TextField
} from '@mui/material';

interface AnnounceModalProps {
    open: boolean;
    onClose: () => void;
    onSubmit: (message: string) => void;
}

const AnnounceModal: React.FC<AnnounceModalProps> = ({ open, onClose, onSubmit }) => {
    const [message, setMessage] = useState('');

    const handleSubmit = () => {
        onSubmit(message);
        setMessage('');
        onClose();
    };

    return (
        <Dialog open={open} onClose={onClose}>
            <DialogTitle>Announce Message</DialogTitle>
            <DialogContent>
                <TextField
                    autoFocus
                    margin="dense"
                    label="Message"
                    type="text"
                    fullWidth
                    value={message}
                    onChange={(e) => setMessage(e.target.value)}
                />
            </DialogContent>
            <DialogActions>
                <Button onClick={onClose} color="secondary">
                    Cancel
                </Button>
                <Button onClick={handleSubmit} color="primary">
                    Announce
                </Button>
            </DialogActions>
        </Dialog>
    );
};

export default AnnounceModal;