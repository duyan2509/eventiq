import React from 'react';
import { ConfigProvider, Spin } from 'antd';
import enUS from 'antd/locale/en_US';
import { AuthProvider, useAuth } from './contexts/AuthContext';
import AppRoutes from './AppRoutes';
import './App.css';

const AppContent = () => {
  const { loading } = useAuth();

  if (loading) {
    return (
      <div className="flex justify-center items-center h-screen">
        <Spin size="large" />
      </div>
    );
  }

  return <AppRoutes />;
};

function App() {
  return (
    <ConfigProvider locale={enUS}>
      <AuthProvider>
        <AppContent />
      </AuthProvider>
    </ConfigProvider>
  );
}

export default App;