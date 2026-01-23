import { createSlice } from '@reduxjs/toolkit';
import type { PayloadAction } from '@reduxjs/toolkit';
import { ServerConfigurationService } from '../../services/ServerConfigurationService';
import type { ServerConfigDTO } from '../../types/ServerConfigDTO';

interface ServerConfigState {
    config: ServerConfigDTO | null;
    loading: boolean;
    error: string | null;
    isSaving: boolean;
}

const initialState: ServerConfigState = {
    config: null,
    loading: false,
    error: null,
    isSaving: false,
};

const serverConfigSlice = createSlice({
    name: 'serverConfig',
    initialState,
    reducers: {
        fetchConfigStart(state) {
            state.loading = true;
            state.error = null;
        },
        fetchConfigSuccess(state, action: PayloadAction<ServerConfigDTO>) {
            state.loading = false;
            state.config = action.payload;
        },
        fetchConfigFailure(state, action: PayloadAction<string>) {
            state.loading = false;
            state.error = action.payload;
        },
        updateConfigStart(state) {
            state.isSaving = true;
            state.error = null;
        },
        updateConfigSuccess(state, action: PayloadAction<ServerConfigDTO>) {
            state.isSaving = false;
            state.config = { ...state.config, ...action.payload };
        },
        updateConfigFailure(state, action: PayloadAction<string>) {
            state.isSaving = false;
            state.error = action.payload;
        },
        clearError(state) {
            state.error = null;
        },
    },
});

export const {
    fetchConfigStart,
    fetchConfigSuccess,
    fetchConfigFailure,
    updateConfigStart,
    updateConfigSuccess,
    updateConfigFailure,
    clearError,
} = serverConfigSlice.actions;

export const fetchServerConfig = (serverId: string) => async (dispatch: any) => {
    dispatch(fetchConfigStart());
    try {
        const config = await ServerConfigurationService.getConfig(serverId);
        dispatch(fetchConfigSuccess(config));
    } catch (error: any) {
        dispatch(fetchConfigFailure(error.message || 'Failed to fetch server config'));
    }
};

export const syncServerConfig = (serverId: string) => async (dispatch: any) => {
    dispatch(fetchConfigStart());
    try {
        await ServerConfigurationService.syncConfig(serverId);
        // After requesting sync, fetch the config again (it will be updated via event)
        // You might want to wait for the event instead
    } catch (error: any) {
        dispatch(fetchConfigFailure(error.message || 'Failed to sync server config'));
    }
};

export const updateServerConfig = (serverId: string, config: ServerConfigDTO) => async (dispatch: any) => {
    dispatch(updateConfigStart());
    try {
        await ServerConfigurationService.updateConfig(serverId, config);
        dispatch(updateConfigSuccess(config));
    } catch (error: any) {
        dispatch(updateConfigFailure(error.message || 'Failed to update server config'));
    }
};

export default serverConfigSlice.reducer;
