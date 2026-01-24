import { createBrowserRouter } from "react-router-dom";
import DashboardLayout from "./layouts/Dashboard";
import AuthenticationLayout from "./layouts/AuthenticationLayout";
import ProtectedRoute from "./components/ProtectedRoute";
import OverviewPage from "./pages/OverviewPage";
import PlayersPage from "./pages/PlayersPage";
import PlayerDetailsPage from "./pages/PlayerDetailsPage";
import ServerConfigurationPage from "./pages/ServerConfigurationPage";
import LoginPage from "./pages/LoginPage";

const MainRoutes = {
    path: '/',
    element: <ProtectedRoute><DashboardLayout /></ProtectedRoute>,
    children: [
        {
            path: '/:serverId',
            element: <OverviewPage />
        },
        {
            path: '/:serverId/players',
            element: <PlayersPage />
        },
        {
            path: '/:serverId/players/:playerId',
            element: <PlayerDetailsPage />
        },
        {
            path: '/:serverId/config',
            element: <ServerConfigurationPage />
        }
    ]
};

const AuthRoutes = {
    path: '/login',
    element: <AuthenticationLayout />,
    children: [
        {
            path: '/login',
            element: <LoginPage />
        }
    ]
};

const router = createBrowserRouter([MainRoutes, AuthRoutes], { basename: import.meta.env.VITE_APP_BASE_NAME });

export default router;