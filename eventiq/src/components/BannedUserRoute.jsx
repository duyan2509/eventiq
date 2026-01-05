import React, { useEffect } from 'react';
import { Navigate, useLocation } from 'react-router-dom';
import { useAuth } from '../contexts/AuthContext';

const BannedUserRoute = ({ children }) => {
  const { user, loading } = useAuth();
  const location = useLocation();

  if (loading) {
    return null;
  }

  if (!user) {
    return children;
  }

  const isBanned = user.isBanned || user.IsBanned || false;

  if (isBanned && location.pathname !== '/my-ticket-b') {
    return <Navigate to="/my-ticket-b" replace />;
  }

  if (isBanned && location.pathname === '/my-ticket-b') {
    return children;
  }

  if (!isBanned && location.pathname === '/my-ticket-b') {
    return <Navigate to="/my-tickets" replace />;
  }

  return children;
};

export default BannedUserRoute;

