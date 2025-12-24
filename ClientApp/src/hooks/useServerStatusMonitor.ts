import { useEffect, useRef } from 'react';
import { useAppDispatch, useAppSelector } from '../store/store';
import { setServerConnected, setServerDisconnected } from '../store/slices/uiSlice';
import ServerService from '../services/ServerService';

const CHECK_INTERVAL = 5000; // Check every 5 seconds

export function useServerStatusMonitor() {
    const dispatch = useAppDispatch();
    const isAuthenticated = useAppSelector(state => state.auth.isAuthenticated);
    const intervalRef = useRef<number | undefined>(undefined);
    const pausedRef = useRef<boolean>(false);

    const checkServerStatus = async () => {
        try {
            await ServerService.getStatus();
            // If we get a response, server is connected
            dispatch(setServerConnected(true));
        } catch (error: any) {
            // Ignore 401; the auth flow will handle the login screen
            if (error?.response?.status === 401) {
                dispatch(setServerConnected(true));
                // Stop further checks until authenticated again
                pausedRef.current = true;
                if (intervalRef.current) {
                    clearInterval(intervalRef.current);
                    intervalRef.current = undefined;
                }
                return;
            }
            // If we get an error, server is disconnected
            const errorMessage = error?.response?.statusText || error?.message || 'Connection failed';
            dispatch(setServerDisconnected(errorMessage));
        }
    };

    useEffect(() => {
        // If not authenticated, do not start polling
        if (!isAuthenticated) {
            pausedRef.current = true;
            if (intervalRef.current) {
                clearInterval(intervalRef.current);
                intervalRef.current = undefined;
            }
            return;
        }

        // If we were paused (e.g., due to 401) and now authenticated, resume
        pausedRef.current = false;

        // Check server status immediately on mount/auth change
        checkServerStatus();

        // Set up interval to check periodically
        intervalRef.current = setInterval(() => {
            checkServerStatus();
        }, CHECK_INTERVAL);

        // Cleanup on unmount
        return () => {
            if (intervalRef.current) {
                clearInterval(intervalRef.current);
                intervalRef.current = undefined;
            }
        };
    }, [dispatch, isAuthenticated]);

    // Return a retry function that the modal can use
    const retryConnection = () => {
        checkServerStatus();
    };

    return { retryConnection };
}
