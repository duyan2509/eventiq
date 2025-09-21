import React from 'react';
import { Navigate } from 'react-router-dom';
import { useAuth } from '../contexts/AuthContext';


const ProtectedRoute = ({ roles, children, redirect = '/' }) => {
    const { loading } = useAuth();
    const user = JSON.parse(localStorage.getItem('user')) ;
    if (loading) return null;
    if (!user) return <Navigate to="/" replace />;

    let userRoles = user['roles'];
    if (!userRoles) return <Navigate to={redirect} replace />;
    if (!Array.isArray(userRoles)) userRoles = [userRoles];

    const requiredRoles = Array.isArray(roles) ? roles : [roles];
    const hasRole = requiredRoles.some((role) => userRoles.includes(role));

    if (!hasRole) return <Navigate to={redirect} replace />;
    return children;
};

export default ProtectedRoute;
