import React, { useState, useEffect } from 'react';
import { Layout, Input, Button, Space, Dropdown, Avatar, Badge } from 'antd';
import { useMessage } from '../../hooks/useMessage';
import { SearchOutlined, UserOutlined, LogoutOutlined, TeamOutlined, BellOutlined } from '@ant-design/icons';
import { useNavigate } from 'react-router-dom';
import { useAuth } from '../../contexts/AuthContext';
import { staffAPI } from '../../services/api';
import { signalRService } from '../../services/signalr';
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
  const [pendingInvitationsCount, setPendingInvitationsCount] = useState(0);

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

  useEffect(() => {
    if (user) {
      fetchPendingInvitations();
      
      // Setup SignalR for real-time updates
      const token = localStorage.getItem('token');
      if (token) {
        signalRService.connect(token);
      }

      // Register SignalR event handlers
      const handleStaffInvited = () => {
        fetchPendingInvitations();
      };

      const handleStaffInvitationResponded = () => {
        fetchPendingInvitations();
      };

      signalRService.on('StaffInvited', handleStaffInvited);
      signalRService.on('StaffInvitationResponded', handleStaffInvitationResponded);

      // Refresh every 30 seconds as fallback
      const interval = setInterval(fetchPendingInvitations, 30000);
      
      return () => {
        clearInterval(interval);
        signalRService.off('StaffInvited', handleStaffInvited);
        signalRService.off('StaffInvitationResponded', handleStaffInvitationResponded);
      };
    }
  }, [user]);

  const fetchPendingInvitations = async () => {
    try {
      const invitations = await staffAPI.getMyInvitations();
      const pending = invitations?.filter(inv => inv.status === 'Pending') || [];
      setPendingInvitationsCount(pending.length);
    } catch (err) {
      // Silently fail - don't show error for background refresh
    }
  };

  const handleMenuClick = ({ key }) => {
    switch (key) {
      case 'organizations':
        navigate('/org');
        break;
      case 'invitations':
        navigate('/invitations');
        break;
      case 'tickets':
        navigate('/my-tickets');
        break;
      case 'logout':
        handleLogout();
        break;
      default:
        break;
    }
  };

  const userMenuItems = [
    {
      key: 'profile',
      icon: <UserOutlined />,
      label: 'Profile',
    },
    {
      key: 'tickets',
      icon: <UserOutlined />,
      label: 'My Tickets',
    },
    {
      key: 'organizations',
      icon: <TeamOutlined />,
      label: 'My Organizations',
    },
    {
      key: 'invitations',
      icon: pendingInvitationsCount > 0 ? (
        <Badge count={pendingInvitationsCount} size="small">
          <BellOutlined />
        </Badge>
      ) : (
        <BellOutlined />
      ),
      label: `My Invitations${pendingInvitationsCount > 0 ? ` (${pendingInvitationsCount})` : ''}`,
    },
    {
      type: 'divider',
    },
    {
      key: 'logout',
      icon: <LogoutOutlined />,
      label: 'Logout',
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
              menu={{ items: userMenuItems, onClick: handleMenuClick }}
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
