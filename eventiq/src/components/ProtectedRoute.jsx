import React from 'react';
import { Navigate } from 'react-router-dom';
import { useAuth } from '../contexts/AuthContext';

const decodeRolesFromToken = (jwtToken) => {
    try {
        const payload = JSON.parse(atob(jwtToken.split('.')[1]));
        const roles =
            payload?.['http://schemas.microsoft.com/ws/2008/06/identity/claims/role'] ||
            payload?.role ||
            payload?.roles ||
            [];
        if (Array.isArray(roles)) return roles;
        if (roles) return [roles];
    } catch (_) {}
    return [];
};

const ProtectedRoute = ({ roles, children, redirect = '/' }) => {
    const { loading } = useAuth();
    const storedUser = JSON.parse(localStorage.getItem('user'));
    const token = localStorage.getItem('token');

    if (loading) return null;
    if (!storedUser && !token) return <Navigate to="/" replace />;

    // If no roles specified, just check authentication
    if (!roles) {
        return children;
    }

    let userRoles = decodeRolesFromToken(token);
    if (!userRoles || (Array.isArray(userRoles) && userRoles.length === 0)) {
        userRoles = storedUser?.roles;
    }
    if (!userRoles) return <Navigate to={redirect} replace />;
    if (!Array.isArray(userRoles)) userRoles = [userRoles];

    const requiredRoles = Array.isArray(roles) ? roles : [roles];
    const hasRole = requiredRoles.some((role) => userRoles.includes(role));

    if (!hasRole) return <Navigate to={redirect} replace />;
    return children;
};

export default ProtectedRoute;
