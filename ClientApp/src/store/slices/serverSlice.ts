import { createSlice } from '@reduxjs/toolkit';
import type { PayloadAction } from '@reduxjs/toolkit';
import ServerService from '../../services/ServerService';
import type { ServerStatusDTO } from '../../types/ServerStatusDTO';

interface ServerState {
    status: ServerStatusDTO | null; // Updated type of status to ServerStatusDTO
    loading: boolean;
    error: string | null;
}

const initialState: ServerState = {
    status: null,
    loading: false,
    error: null,
};

const serverSlice = createSlice({
    name: 'server',
    initialState,
    reducers: {
        fetchServerStatusStart(state) {
            state.loading = true;
            state.error = null;
        },
        fetchServerStatusSuccess(state, action: PayloadAction<ServerStatusDTO>) {
            state.loading = false;
            state.status = action.payload;
        },
        fetchServerStatusFailure(state, action: PayloadAction<string>) {
            state.loading = false;
            state.error = action.payload;
        },
    },
});

export const { fetchServerStatusStart, fetchServerStatusSuccess, fetchServerStatusFailure } = serverSlice.actions;

export const fetchServerStatus = () => async (dispatch: any) => {
    dispatch(fetchServerStatusStart());
    try {
        const status = await ServerService.getStatus();
        dispatch(fetchServerStatusSuccess(status));
    } catch (error: any) {
        dispatch(fetchServerStatusFailure(error.message));
    }
};

export default serverSlice.reducer;