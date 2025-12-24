import React from 'react';
import { Layout } from 'antd';
import { Outlet } from 'react-router-dom';
import Header from './Header';

const { Content, Footer } = Layout;

const MainLayout = () => {
  return (
    <Layout className="min-h-screen">
      <Header />
      <Content className="p-6 bg-gray-50">
        <Outlet />
      </Content>
      <Footer className="text-center bg-white">
        EventIQ ©2024 - Event Management Platform
      </Footer>
    </Layout>
  );
};

export default MainLayout;
