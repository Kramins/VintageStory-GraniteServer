import { createBrowserRouter } from "react-router-dom";
import DashboardLayout from "./layouts/Dashboard";
import OverviewPage from "./pages/OverviewPage";
import PlayersPage from "./pages/PlayersPage";
import WorldPage from "./pages/WorldPage";
import ModerationPage from "./pages/ModerationPage";
import SettingsPage from "./pages/SettingsPage";

const MainRoutes = {
    path: '/',
    element: <DashboardLayout />,
    children: [
        {
            path: '/',
            element: <OverviewPage />
        },
        {
            path: '/players',
            element: <PlayersPage />
        },
        {
            path: '/world',
            element: <WorldPage />
        },
        {
            path: '/moderation',
            element: <ModerationPage />
        },
        {
            path: '/settings',
            element: <SettingsPage />
        }
    ]

};


const router = createBrowserRouter([MainRoutes], { basename: import.meta.env.VITE_APP_BASE_NAME });

export default router;