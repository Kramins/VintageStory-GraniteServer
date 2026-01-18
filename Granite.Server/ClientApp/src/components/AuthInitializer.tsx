import { useEffect } from 'react';
import { AuthService } from '../services/AuthService';
import ServerService from '../services/ServerService';
import store, { useAppSelector } from '../store/store';
import { setAuth } from '../store/slices/authSlice';
import { setServers, setLoading, setError } from '../store/slices/serversSlice';

/**
 * Component to initialize auth state from localStorage on app startup
 * Note: SignalR connection is managed by LoginPage after successful authentication
 */
export default function AuthInitializer() {
    const isAuthenticated = useAppSelector(state => state.auth.isAuthenticated);

    useEffect(() => {
        const token = AuthService.getToken();
        if (token) {
            // Restore auth state from stored token
            // ProtectedRoute will validate if this token is still valid
            store.dispatch(setAuth(token));
        }
    }, []);

    // Fetch servers when user is authenticated
    useEffect(() => {
        if (isAuthenticated) {
            const fetchServers = async () => {
                try {
                    store.dispatch(setLoading(true));
                    const servers = await ServerService.fetchServers();
                    store.dispatch(setServers(servers));
                } catch (err) {
                    console.error('Failed to fetch servers:', err);
                    store.dispatch(setError('Failed to load game servers'));
                }
            };

            fetchServers();
        }
    }, [isAuthenticated]);

    return null;
}
