import { createSlice } from '@reduxjs/toolkit';
import type { PayloadAction } from '@reduxjs/toolkit';

export interface UserInfo {
    username: string;
    role: string;
}

interface AuthState {
    user: UserInfo | null;
    isAuthenticated: boolean;
    token: string | null;
}

const decodeJwtToken = (token: string): UserInfo | null => {
    try {
        // JWT tokens have 3 parts separated by dots
        const parts = token.split('.');
        if (parts.length !== 3) return null;

        // Decode the payload (middle part)
        const payload = JSON.parse(atob(parts[1]));

        return {
            username: payload.unique_name || payload.nameid || payload.sub || 'Unknown',
            role: payload.role || 'User',
        };
    } catch (error) {
        console.error('Failed to decode JWT token:', error);
        return null;
    }
};

const initialState: AuthState = {
    user: null,
    isAuthenticated: false,
    token: null,
};

const authSlice = createSlice({
    name: 'auth',
    initialState,
    reducers: {
        setAuth(state, action: PayloadAction<string>) {
            const token = action.payload;
            state.token = token;
            state.user = decodeJwtToken(token);
            state.isAuthenticated = true;
        },
        clearAuth(state) {
            state.token = null;
            state.user = null;
            state.isAuthenticated = false;
        },
    },
});

export const { setAuth, clearAuth } = authSlice.actions;
export default authSlice.reducer;
