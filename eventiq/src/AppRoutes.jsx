import React from 'react';
import { BrowserRouter as Router, Routes, Route } from 'react-router-dom';
import MainLayout from './components/Layout/MainLayout';
import Home from './pages/Home';
import AdminPage from './pages/AdminPage';
import OrgList from './pages/OrgList';
import OrganizationDetail from './pages/OrganizationDetail';
import CreateEvent from './pages/CreateEvent';
import UpdateEvent from './pages/UpdateEvent';
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
              <OrgList />
            </ProtectedRoute>
          }
        />
        <Route
          path="/org/:orgId"
          element={
            <ProtectedRoute roles="Org">
              <OrganizationDetail />
            </ProtectedRoute>
          }
        />
        <Route
          path="/create-event"
          element={
            <ProtectedRoute roles="Org">
              <CreateEvent />
            </ProtectedRoute>
          }
        />
        <Route
          path="/org/:orgId/event/:eventId"
          element={
            <ProtectedRoute roles="Org">
              <UpdateEvent />
            </ProtectedRoute>
          }
        />
      </Routes>
    </MainLayout>
  </Router>
);

export default AppRoutes;
