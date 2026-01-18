import { useState, useEffect } from "react";
import { useNavigate, useLocation } from "react-router-dom";
import {
  Box,
  Card,
  CardContent,
  TextField,
  Button,
  Typography,
  Alert,
  CircularProgress,
} from "@mui/material";
import { AuthService } from "../services/AuthService";
import ServerService from "../services/ServerService";
import { useAppDispatch } from "../store/store";
import { setServers, setLoading as setServersLoading, setError as setServersError } from "../store/slices/serversSlice";
import { EventBus } from "../services/EventBus";

export default function LoginPage() {
  const [username, setUsername] = useState("");
  const [password, setPassword] = useState("");
  const [error, setError] = useState("");
  const [loading, setLoading] = useState(false);
  const [authType, setAuthType] = useState<string | null>(null);
  const [checkingAuth, setCheckingAuth] = useState(true);
  const navigate = useNavigate();
  const location = useLocation();
  const dispatch = useAppDispatch();

  useEffect(() => {
    // Get auth settings from the server
    AuthService.getAuthSettings()
      .then((settings) => {
        setAuthType(settings.authenticationType);
        
        // If auth is disabled (None), redirect to main app
        if (settings.authenticationType === "None" || settings.authenticationType === "") {
          const from = (location.state as any)?.from?.pathname || "/";
          navigate(from, { replace: true });
        }
      })
      .catch((err) => {
        setError("Failed to load authentication settings");
        console.error(err);
      })
      .finally(() => {
        setCheckingAuth(false);
      });
  }, [navigate, location]);

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    setError("");
    setLoading(true);

    try {
      await AuthService.login({ username, password });
      const token = AuthService.getToken();
      
      // Fetch available game servers after successful login
      try {
        dispatch(setServersLoading(true));
        const servers = await ServerService.fetchServers();
        dispatch(setServers(servers));
      } catch (serverErr) {
        console.error("Failed to fetch servers:", serverErr);
        dispatch(setServersError("Failed to load game servers"));
        // Don't block navigation on server fetch failure
      }
      
      // Start SignalR connection after successful authentication
      if (token) {
        EventBus.start(token);
      }
      
      const from = (location.state as any)?.from?.pathname || "/";
      navigate(from, { replace: true });
    } catch (err: any) {
      setError(err.response?.data?.message || "Invalid username or password");
    } finally {
      setLoading(false);
    }
  };

  if (checkingAuth) {
    return (
      <Box sx={{ display: "flex", justifyContent: "center", alignItems: "center", minHeight: "50vh" }}>
        <CircularProgress />
      </Box>
    );
  }

  return (
    <Box sx={{ width: "100%" }}>
      <Card>
        <CardContent sx={{ p: 4 }}>
          <Typography variant="h4" component="h1" gutterBottom align="center">
            Granite Server
          </Typography>
          <Typography variant="body2" color="text.secondary" align="center" sx={{ mb: 3 }}>
            Sign in to continue
          </Typography>

          {error && (
            <Alert severity="error" sx={{ mb: 2 }}>
              {error}
            </Alert>
          )}

          <form onSubmit={handleSubmit}>
            <TextField
              fullWidth
              label="Username"
              value={username}
              onChange={(e) => setUsername(e.target.value)}
              margin="normal"
              required
              autoFocus
              disabled={loading}
            />
            <TextField
              fullWidth
              label="Password"
              type="password"
              value={password}
              onChange={(e) => setPassword(e.target.value)}
              margin="normal"
              required
              disabled={loading}
            />
            <Button
              type="submit"
              fullWidth
              variant="contained"
              size="large"
              sx={{ mt: 3 }}
              disabled={loading}
            >
              {loading ? <CircularProgress size={24} /> : "Sign In"}
            </Button>
          </form>

          {authType && (
            <Typography variant="caption" color="text.secondary" align="center" sx={{ mt: 2, display: "block" }}>
              Authentication: {authType}
            </Typography>
          )}
        </CardContent>
      </Card>
    </Box>
  );
}
