import { useEffect } from 'react';
import { AuthService } from '../services/AuthService';
import store, { useAppSelector } from '../store/store';
import { setAuth } from '../store/slices/authSlice';
import { EventBus } from '../services/EventBus';

/**
 * Component to initialize auth state from localStorage on app startup
 */
export default function AuthInitializer() {
    const token = useAppSelector(state => state.auth.token);

    useEffect(() => {
        const token = AuthService.getToken();
        if (token) {
            // Restore auth state from stored token
            store.dispatch(setAuth(token));
        }
    }, []);

    useEffect(() => {
        if (token) {
            EventBus.start(token);
            return () => EventBus.stop();
        }

        EventBus.stop();
        return undefined;
    }, [token]);

    return null;
}
