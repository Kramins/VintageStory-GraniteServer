import { createSlice } from '@reduxjs/toolkit';
import type { PayloadAction } from '@reduxjs/toolkit';

interface UIState {
    isServerConnected: boolean;
    disconnectionReason?: string;
}

const initialState: UIState = {
    isServerConnected: true,
};

const uiSlice = createSlice({
    name: 'ui',
    initialState,
    reducers: {
        setServerConnected(state, action: PayloadAction<boolean>) {
            state.isServerConnected = action.payload;
        },
        setDisconnectionReason(state, action: PayloadAction<string | undefined>) {
            state.disconnectionReason = action.payload;
        },
        setServerDisconnected(state, action: PayloadAction<string | undefined>) {
            state.isServerConnected = false;
            state.disconnectionReason = action.payload;
        },
        reconnectServer(state) {
            state.isServerConnected = true;
            state.disconnectionReason = undefined;
        },
    },
});

export const { setServerConnected, setDisconnectionReason, setServerDisconnected, reconnectServer } = uiSlice.actions;
export default uiSlice.reducer;
