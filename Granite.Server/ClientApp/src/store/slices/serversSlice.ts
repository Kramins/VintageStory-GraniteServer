import { createSlice } from '@reduxjs/toolkit';
import type { PayloadAction } from '@reduxjs/toolkit';
import type { ServerDTO } from '../../types/ServerDTO';

const SELECTED_SERVER_KEY = 'selected_server_id';

interface ServersState {
    servers: ServerDTO[];
    selectedServerId: string | null;
    loading: boolean;
    error: string | null;
}

const initialState: ServersState = {
    servers: [],
    selectedServerId: localStorage.getItem(SELECTED_SERVER_KEY),
    loading: false,
    error: null,
};

const serversSlice = createSlice({
    name: 'servers',
    initialState,
    reducers: {
        setServers: (state, action: PayloadAction<ServerDTO[]>) => {
            state.servers = action.payload;
            state.loading = false;
            state.error = null;
            
            // Auto-select first server if none selected and servers available
            if (!state.selectedServerId && action.payload.length > 0) {
                state.selectedServerId = action.payload[0].id;
                localStorage.setItem(SELECTED_SERVER_KEY, action.payload[0].id);
            }
            
            // Clear selection if selected server no longer exists
            if (state.selectedServerId && !action.payload.find(s => s.id === state.selectedServerId)) {
                state.selectedServerId = action.payload.length > 0 ? action.payload[0].id : null;
                if (state.selectedServerId) {
                    localStorage.setItem(SELECTED_SERVER_KEY, state.selectedServerId);
                } else {
                    localStorage.removeItem(SELECTED_SERVER_KEY);
                }
            }
        },
        selectServer: (state, action: PayloadAction<string>) => {
            state.selectedServerId = action.payload;
            localStorage.setItem(SELECTED_SERVER_KEY, action.payload);
        },
        setLoading: (state, action: PayloadAction<boolean>) => {
            state.loading = action.payload;
        },
        setError: (state, action: PayloadAction<string | null>) => {
            state.error = action.payload;
            state.loading = false;
        },
        clearServers: (state) => {
            state.servers = [];
            state.selectedServerId = null;
            state.loading = false;
            state.error = null;
            localStorage.removeItem(SELECTED_SERVER_KEY);
        },
    },
});

export const { setServers, selectServer, setLoading, setError, clearServers } = serversSlice.actions;
export default serversSlice.reducer;
