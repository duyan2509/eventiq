import React, { useState, useEffect } from 'react';
import { Layout, Input, Button, Space, Dropdown, Avatar, Badge } from 'antd';
import { useMessage } from '../../hooks/useMessage';
import { SearchOutlined, UserOutlined, LogoutOutlined, TeamOutlined, BellOutlined, CalendarOutlined } from '@ant-design/icons';
import { useNavigate } from 'react-router-dom';
import { useAuth } from '../../contexts/AuthContext';
import { staffAPI, transferAPI } from '../../services/api';
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
  const [pendingTransfersCount, setPendingTransfersCount] = useState(0);

  const handleSearch = (value) => {
    if (value.trim()) {
      navigate(`/events?search=${encodeURIComponent(value)}&page=1`);
    } else {
      navigate('/events');
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
      fetchPendingTransfers();
      
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

      const handleTransferRequestReceived = () => {
        fetchPendingTransfers();
        success('You have received a new transfer request');
      };

      signalRService.on('StaffInvited', handleStaffInvited);
      signalRService.on('StaffInvitationResponded', handleStaffInvitationResponded);
      signalRService.on('TransferRequestReceived', handleTransferRequestReceived);

      const interval = setInterval(() => {
        fetchPendingInvitations();
        fetchPendingTransfers();
      }, 30000);
      
      return () => {
        clearInterval(interval);
        signalRService.off('StaffInvited', handleStaffInvited);
        signalRService.off('StaffInvitationResponded', handleStaffInvitationResponded);
        signalRService.off('TransferRequestReceived', handleTransferRequestReceived);
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

  const fetchPendingTransfers = async () => {
    try {
      const data = await transferAPI.getIncomingTransfers(1, 10);
      const transfers = data.data || [];
      const pending = transfers.filter(
        transfer => transfer.status === 'PENDING' && new Date(transfer.expiresAt) > new Date()
      );
      setPendingTransfersCount(pending.length);
    } catch (err) {
    }
  };

  const handleMenuClick = ({ key }) => {
    const isBanned = user?.isBanned || user?.IsBanned || false;
    switch (key) {
      case 'organizations':
        if (isBanned) {
          warning('Your account has been banned. You cannot access this page.');
          navigate('/my-ticket-b');
        } else {
          navigate('/org');
        }
        break;
      case 'invitations':
        if (isBanned) {
          warning('Your account has been banned. You cannot access this page.');
          navigate('/my-ticket-b');
        } else {
          navigate('/invitations');
        }
        break;
      case 'tickets':
        navigate(isBanned ? '/my-ticket-b' : '/my-tickets');
        break;
      case 'workspace':
        if (isBanned) {
          warning('Your account has been banned. You cannot access this page.');
          navigate('/my-ticket-b');
        } else {
          navigate('/workspace');
        }
        break;
      case 'logout':
        handleLogout();
        break;
      default:
        break;
    }
  };

  const isBanned = user?.isBanned || user?.IsBanned || false;
  const userMenuItems = [
    {
      key: 'profile',
      icon: <UserOutlined />,
      label: 'Profile',
      disabled: isBanned,
    },
    {
      key: 'tickets',
      icon: pendingTransfersCount > 0 && !isBanned ? (
        <Badge count={pendingTransfersCount} size="small">
          <UserOutlined />
        </Badge>
      ) : (
        <UserOutlined />
      ),
      label: isBanned 
        ? 'My Tickets (Banned)' 
        : `My Tickets${pendingTransfersCount > 0 ? ` (${pendingTransfersCount})` : ''}`,
    },
    {
      key: 'workspace',
      icon: <CalendarOutlined />,
      label: 'Work Space',
      disabled: isBanned,
    },
    {
      key: 'organizations',
      icon: <TeamOutlined />,
      label: 'My Organizations',
      disabled: isBanned,
    },
    {
      key: 'invitations',
      icon: pendingInvitationsCount > 0 && !isBanned ? (
        <Badge count={pendingInvitationsCount} size="small">
          <BellOutlined />
        </Badge>
      ) : (
        <BellOutlined />
      ),
      label: `My Invitations${pendingInvitationsCount > 0 && !isBanned ? ` (${pendingInvitationsCount})` : ''}`,
      disabled: isBanned,
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
