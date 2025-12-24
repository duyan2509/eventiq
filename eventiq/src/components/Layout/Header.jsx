import React, { useState } from 'react';
import { Layout, Input, Button, Space, Dropdown, Avatar } from 'antd';
import { useMessage } from '../../hooks/useMessage';
import { SearchOutlined, UserOutlined, LogoutOutlined, SettingOutlined, TeamOutlined } from '@ant-design/icons';
import { useNavigate } from 'react-router-dom';
import { useAuth } from '../../contexts/AuthContext';
import LoginModal from '../Auth/LoginModal';
import RegisterModal from '../Auth/RegisterModal';

const { Header: AntHeader } = Layout;
const { Search } = Input;

const Header = () => {
  const navigate = useNavigate();
  const { user, logout } = useAuth();
  const { success, error, warning, contextHolder } = useMessage();
  const [loginVisible, setLoginVisible] = useState(false);
  const [registerVisible, setRegisterVisible] = useState(false);
  const [searchValue, setSearchValue] = useState('');

  const handleSearch = (value) => {
    if (value.trim()) {
      warning(`Searching: ${value}`);
      // TODO: Implement search functionality
    }
  };

  const handleLogout = () => {
    logout();
    success('Logged out successfully');
    navigate('/');
  };

  const userMenuItems = [
    {
      key: 'profile',
      icon: <UserOutlined />,
      label: 'Profile',
    },
    {
      key: 'organizations',
      icon: <TeamOutlined />,
      label: 'My Organizations',
      onClick: () => navigate('/org'),
    },
    {
      key: 'settings',
      icon: <SettingOutlined />,
      label: 'Settings',
    },
    {
      type: 'divider',
    },
    {
      key: 'logout',
      icon: <LogoutOutlined />,
      label: 'Logout',
      onClick: handleLogout,
    },
  ];

  return (
    <>
      <AntHeader className="bg-white px-6 shadow-md flex items-center justify-between sticky top-0 z-50">
        {/* Logo */}
        <div className="text-2xl font-bold text-blue-500">
          EventIQ
        </div>

        {/* Search Bar */}
        <div className="flex-1 max-w-2xl mx-10 mt-4">
          <Search
            placeholder="Search events, venues..."
            allowClear
            enterButton={<SearchOutlined />}
            size="large"
            value={searchValue}
            onChange={(e) => setSearchValue(e.target.value)}
            onSearch={handleSearch}
            className="w-full"
          />
        </div>

        {/* Auth Section */}
        <Space size="middle">
          {user ? (
            <Dropdown
              menu={{ items: userMenuItems }}
              placement="bottomRight"
              arrow
            >
              <Space className="cursor-pointer">
                <Avatar 
                  size="large" 
                  icon={<UserOutlined />} 
                  className="bg-blue-500"
                />
                <span className="font-medium">
                  {user.firstName} {user.lastName}
                </span>
              </Space>
            </Dropdown>
          ) : (
            <Space>
              <Button 
                type="default" 
                onClick={() => setLoginVisible(true)}
              >
                Login
              </Button>
              <Button 
                type="primary" 
                onClick={() => setRegisterVisible(true)}
              >
                Register
              </Button>
            </Space>
          )}
        </Space>
      </AntHeader>

      {/* Modals */}
      {contextHolder}
      <LoginModal 
        visible={loginVisible} 
        onCancel={() => setLoginVisible(false)}
        onSuccess={() => {
          setLoginVisible(false);
          success('Login successful');
        }}
      />
      
      <RegisterModal 
        visible={registerVisible} 
        onCancel={() => setRegisterVisible(false)}
        onSuccess={() => {
          setRegisterVisible(false);
          success('Registration successful');
        }}
      />
    </>
  );
};

export default Header;
