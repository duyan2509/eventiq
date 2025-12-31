import React from 'react';
import { BrowserRouter as Router, Routes, Route } from 'react-router-dom';
import MainLayout from './components/Layout/MainLayout';
import AdminLayout from './components/Layout/AdminLayout';
import Home from './pages/Home';
import AdminDashboard from './pages/AdminDashboard';
import AdminEventManagement from './pages/AdminEventManagement';
import AdminRevenueManagement from './pages/AdminRevenueManagement';
import AdminUserManagement from './pages/AdminUserManagement';
import AdminProfile from './pages/AdminProfile';
import OrgList from './pages/OrgList';
import OrganizationDetail from './pages/OrganizationDetail';
import CreateEvent from './pages/CreateEvent';
import UpdateEvent from './pages/UpdateEvent';
import EventDetail from './pages/EventDetail';
import ProtectedRoute from './components/ProtectedRoute';
import SeatMapDesignerPage from './pages/SeatMapDesignerPage';
import StaffManagement from './pages/StaffManagement';
import Invitations from './pages/Invitations';
import CustomerEventList from './pages/CustomerEventList';
import CustomerEventDetail from './pages/CustomerEventDetail';
import CustomerSeatMap from './pages/CustomerSeatMap';
import PaymentSkeleton from './pages/PaymentSkeleton';
import OrgRevenueReport from './pages/OrgRevenueReport';
import MyTickets from './pages/MyTickets';
const AppRoutes = () => (
  <Router>
    <Routes>
      {/* Seat Map Designer Route - Outside MainLayout */}
      <Route
        path="/org/:orgId/event/:eventId/seat-map/:chartId"
        element={
          <ProtectedRoute roles="Org">
            <SeatMapDesignerPage />
          </ProtectedRoute>
        }
      />
      <Route
        path="/org/:orgId/event/:eventId/chart/:chartId"
        element={
          <ProtectedRoute roles="Org">
            <SeatMapDesignerPage />
          </ProtectedRoute>
        }
      />
      
      {/* Customer Seat Map Route - Outside MainLayout (Full Screen) */}
      <Route
        path="/events/:eventId/items/:eventItemId/seats"
        element={<CustomerSeatMap />}
      />
      
      {/* Admin routes - Inside AdminLayout */}
      <Route
        element={
          <ProtectedRoute roles="Admin">
            <AdminLayout />
          </ProtectedRoute>
        }
      >
        <Route path="/admin" element={<AdminDashboard />} />
        <Route path="/admin/events" element={<AdminEventManagement />} />
        <Route path="/admin/revenue" element={<AdminRevenueManagement />} />
        <Route path="/admin/users" element={<AdminUserManagement />} />
        <Route path="/admin/profile" element={<AdminProfile />} />
      </Route>

      {/* All other routes - Inside MainLayout */}
      <Route
        element={<MainLayout />}
      >
        <Route path="/" element={<Home />} />
        <Route path="/events" element={<CustomerEventList />} />
        <Route path="/events/:eventId" element={<CustomerEventDetail />} />
        <Route path="/payment" element={<PaymentSkeleton />} />
        <Route path="/event/:eventId" element={<EventDetail />} />
        <Route path="/my-tickets" element={<MyTickets />} />
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
        <Route
          path="/org/:orgId/event/:eventId/staff"
          element={
            <ProtectedRoute roles="Org">
              <StaffManagement />
            </ProtectedRoute>
          }
        />
        <Route
          path="/org/:orgId/event/:eventId/revenue"
          element={
            <ProtectedRoute roles="Org">
              <OrgRevenueReport />
            </ProtectedRoute>
          }
        />
        <Route
          path="/invitations"
          element={
            <ProtectedRoute>
              <Invitations />
            </ProtectedRoute>
          }
        />
      </Route>
    </Routes>
  </Router>
);

export default AppRoutes;
