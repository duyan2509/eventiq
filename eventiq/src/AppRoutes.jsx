import React from 'react';
import { BrowserRouter as Router, Routes, Route } from 'react-router-dom';
import MainLayout from './components/Layout/MainLayout';
import Home from './pages/Home';
import AdminPage from './pages/AdminPage';
import OrgPage from './pages/OrgPage';
import ProtectedRoute from './components/ProtectedRoute';

const AppRoutes = () => (
  <Router>
    <MainLayout>
      <Routes>
        <Route path="/" element={<Home />} />
        <Route
          path="/admin"
          element={
            <ProtectedRoute roles="Admin">
              <AdminPage />
            </ProtectedRoute>
          }
        />
        <Route
          path="/org"
          element={
            <ProtectedRoute roles="Org">
              <OrgPage />
            </ProtectedRoute>
          }
        />
      </Routes>
    </MainLayout>
  </Router>
);

export default AppRoutes;
