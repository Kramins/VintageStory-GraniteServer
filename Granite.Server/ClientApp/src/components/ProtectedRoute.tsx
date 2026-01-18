import { Navigate, useLocation } from "react-router-dom";
import { useEffect, useState } from "react";
import { AuthService } from "../services/AuthService";
import { CircularProgress, Box } from "@mui/material";

interface ProtectedRouteProps {
  children: React.ReactNode;
}

export default function ProtectedRoute({ children }: ProtectedRouteProps) {
  const location = useLocation();
  const [authRequired, setAuthRequired] = useState<boolean | null>(null);
  const [loading, setLoading] = useState(true);

  useEffect(() => {
    // Check if authentication is required
    AuthService.getAuthSettings()
      .then((settings) => {
        const isAuthRequired = settings.authenticationType !== "None" && settings.authenticationType !== "";
        setAuthRequired(isAuthRequired);
      })
      .catch(() => {
        // If we can't get settings, assume auth is required
        setAuthRequired(true);
      })
      .finally(() => {
        setLoading(false);
      });
  }, []);

  if (loading) {
    return (
      <Box sx={{ display: "flex", justifyContent: "center", alignItems: "center", minHeight: "100vh" }}>
        <CircularProgress />
      </Box>
    );
  }

  // If auth is not required, allow access
  if (!authRequired) {
    return <>{children}</>;
  }

  // If auth is required but user is not authenticated, redirect to login
  if (!AuthService.isAuthenticated()) {
    return <Navigate to="/login" state={{ from: location }} replace />;
  }

  return <>{children}</>;
}
