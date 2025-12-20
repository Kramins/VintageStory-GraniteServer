import { configureStore } from '@reduxjs/toolkit';
import type { ThunkDispatch } from '@reduxjs/toolkit';
import type { AnyAction } from 'redux';
import { useDispatch, useSelector } from 'react-redux';

import playersReducer from './slices/playersSlice'
import playerDetailsReducer from './slices/playerDetailsSlice'
import serverReducer from './slices/serverSlice'
import worldReducer from './slices/collectiblesSlice'

const store = configureStore({
    reducer: {
        players: playersReducer,
        playerDetails: playerDetailsReducer,
        server: serverReducer,
        world: worldReducer,
    },
});


export type RootState = ReturnType<typeof store.getState>;
export type AppDispatch = ThunkDispatch<RootState, void, AnyAction>;

// Export typed hooks for use throughout the app
export const useAppDispatch = () => useDispatch<AppDispatch>();
export const useAppSelector = useSelector.withTypes<RootState>();

export default store;