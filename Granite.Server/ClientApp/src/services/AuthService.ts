import axios from 'axios';
import type { AuthSettingsDTO } from '../types/AuthSettingsDTO';
import type { BasicAuthCredentialsDTO } from '../types/BasicAuthCredentialsDTO';
import type { TokenDTO } from '../types/TokenDTO';
import store from '../store/store';
import { setAuth, clearAuth } from '../store/slices/authSlice';

const API_BASE = '/api/auth';
const TOKEN_KEY = 'auth_token';
const REFRESH_TOKEN_KEY = 'refresh_token';

let cachedAuthSettings: AuthSettingsDTO | null = null;

export const AuthService = {
    async getAuthSettings(): Promise<AuthSettingsDTO> {
        if (cachedAuthSettings) {
            return cachedAuthSettings;
        }
        const response = await axios.get(`${API_BASE}/settings`);
        cachedAuthSettings = response.data;
        return response.data;
    },

    clearAuthSettingsCache(): void {
        cachedAuthSettings = null;
    },

    async login(credentials: BasicAuthCredentialsDTO): Promise<TokenDTO> {
        const response = await axios.post(`${API_BASE}/login`, credentials);
        const token = response.data;
        
        // Store tokens in localStorage
        this.setToken(token.access_token);
        if (token.refresh_token) {
            this.setRefreshToken(token.refresh_token);
        }
        
        // Dispatch to Redux store
        store.dispatch(setAuth(token.access_token));
        
        return token;
    },

    logout(): void {
        localStorage.removeItem(TOKEN_KEY);
        localStorage.removeItem(REFRESH_TOKEN_KEY);
        store.dispatch(clearAuth());
    },

    getToken(): string | null {
        return localStorage.getItem(TOKEN_KEY);
    },

    setToken(token: string): void {
        localStorage.setItem(TOKEN_KEY, token);
    },

    getRefreshToken(): string | null {
        return localStorage.getItem(REFRESH_TOKEN_KEY);
    },

    setRefreshToken(token: string): void {
        localStorage.setItem(REFRESH_TOKEN_KEY, token);
    },

    isAuthenticated(): boolean {
        return !!this.getToken();
    }
};

// Setup axios interceptor to automatically add auth token to requests
axios.interceptors.request.use(
    (config) => {
        // Don't add token to auth endpoints (login, settings, etc.)
        const isAuthEndpoint = config.url?.includes('/api/auth/');
        
        if (!isAuthEndpoint) {
            const token = AuthService.getToken();
            if (token) {
                config.headers.Authorization = `Bearer ${token}`;
            }
        }
        return config;
    },
    (error) => {
        return Promise.reject(error);
    }
);

// Setup axios interceptor to handle 401 errors
axios.interceptors.response.use(
    (response) => response,
    async (error) => {
        if (error.response?.status === 401) {
            // Check if auth is actually required before redirecting
            try {
                const settings = await AuthService.getAuthSettings();
                const authRequired = settings.authenticationType !== "None" && settings.authenticationType !== "";
                
                if (authRequired) {
                    AuthService.logout();
                    // Only redirect to login if not already there
                    if (!window.location.pathname.startsWith('/login')) {
                        window.location.href = '/login';
                    }
                }
            } catch {
                // If we can't get settings, proceed with logout/redirect as safety measure
                AuthService.logout();
                if (!window.location.pathname.startsWith('/login')) {
                    window.location.href = '/login';
                }
            }
        }
        return Promise.reject(error);
    }
);
