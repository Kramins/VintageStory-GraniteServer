import { useEffect } from 'react';
import { AuthService } from '../services/AuthService';
import store from '../store/store';
import { setAuth } from '../store/slices/authSlice';

/**
 * Component to initialize auth state from localStorage on app startup
 */
export default function AuthInitializer() {
    useEffect(() => {
        const token = AuthService.getToken();
        if (token) {
            // Restore auth state from stored token
            store.dispatch(setAuth(token));
        }
    }, []);

    return null;
}
