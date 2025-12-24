import { useEffect, useRef } from 'react';
import { useAppDispatch } from '../store/store';
import { setServerConnected, setServerDisconnected, reconnectServer } from '../store/slices/uiSlice';
import ServerService from '../services/ServerService';

const CHECK_INTERVAL = 5000; // Check every 5 seconds

export function useServerStatusMonitor() {
    const dispatch = useAppDispatch();
    const intervalRef = useRef<number | undefined>(undefined);

    const checkServerStatus = async () => {
        try {
            await ServerService.getStatus();
            // If we get a response, server is connected
            dispatch(setServerConnected(true));
        } catch (error: any) {
            // If we get an error, server is disconnected
            const errorMessage = error?.response?.statusText || error?.message || 'Connection failed';
            dispatch(setServerDisconnected(errorMessage));
        }
    };

    useEffect(() => {
        // Check server status immediately on mount
        checkServerStatus();

        // Set up interval to check periodically
        intervalRef.current = setInterval(() => {
            checkServerStatus();
        }, CHECK_INTERVAL);

        // Cleanup on unmount
        return () => {
            if (intervalRef.current) {
                clearInterval(intervalRef.current);
            }
        };
    }, [dispatch]);

    // Return a retry function that the modal can use
    const retryConnection = () => {
        checkServerStatus();
    };

    return { retryConnection };
}
