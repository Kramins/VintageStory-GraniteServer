import { Navigate, useLocation } from "react-router-dom";
import { useEffect, useState } from "react";
import { AuthService } from "../services/AuthService";
import { CircularProgress, Box } from "@mui/material";
import { useAppSelector } from "../store/store";

interface ProtectedRouteProps {
  children: React.ReactNode;
}

export default function ProtectedRoute({ children }: ProtectedRouteProps) {
  const location = useLocation();
  const [authRequired, setAuthRequired] = useState<boolean | null>(null);
  const [loading, setLoading] = useState(true);
  const isAuthenticated = useAppSelector(state => state.auth.isAuthenticated);

  useEffect(() => {
    // Check if authentication is required
    AuthService.getAuthSettings()
      .then((settings) => {
        const isAuthRequired = settings.authenticationType !== "None" && settings.authenticationType !== "";
        console.log('[ProtectedRoute] Auth settings loaded:', { authenticationType: settings.authenticationType, isAuthRequired });
        setAuthRequired(isAuthRequired);
      })
      .catch((err) => {
        console.error('[ProtectedRoute] Failed to get auth settings, assuming auth required:', err);
        // If we can't get settings, assume auth is required
        setAuthRequired(true);
      })
      .finally(() => {
        setLoading(false);
      });
  }, []);

  console.log('[ProtectedRoute] State:', { loading, authRequired, isAuthenticated, pathname: location.pathname });

  if (loading) {
    return (
      <Box sx={{ display: "flex", justifyContent: "center", alignItems: "center", minHeight: "100vh" }}>
        <CircularProgress />
      </Box>
    );
  }

  // If auth is not required, allow access
  if (!authRequired) {
    console.log('[ProtectedRoute] Auth not required, allowing access');
    return <>{children}</>;
  }

  // If auth is required but user is not authenticated, redirect to login
  if (!isAuthenticated) {
    console.log('[ProtectedRoute] Auth required but not authenticated, redirecting to /login');
    return <Navigate to="/login" state={{ from: location }} replace />;
  }

  console.log('[ProtectedRoute] Authenticated, allowing access');
  return <>{children}</>;
}
