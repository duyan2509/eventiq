import React from 'react';
import { Layout } from 'antd';
import Header from './Header';

const { Content, Footer } = Layout;

const MainLayout = ({ children }) => {
  return (
    <Layout className="min-h-screen">
      <Header />
      <Content className="p-6 bg-gray-50">
        {children}
      </Content>
      <Footer className="text-center bg-white">
        EventIQ ©2024 - Event Management Platform
      </Footer>
    </Layout>
  );
};

export default MainLayout;
