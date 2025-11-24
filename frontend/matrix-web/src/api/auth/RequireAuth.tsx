import { Navigate, useLocation } from "react-router-dom";
import { useAuth } from "./AuthContext";

interface Props {
  children: React.ReactElement;
}

export const RequireAuth = ({ children }: Props) => {
  const { user, isLoading } = useAuth();
  const location = useLocation();

  if (isLoading) {
    return <div>Loading...</div>;
  }

  if (!user) {
    return <Navigate to="/login" state={{ from: location }} replace />;
  }

  return children;
};
