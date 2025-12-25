import React, { useState } from 'react';
import { Layout, Menu, Button, Space, Avatar, Dropdown } from 'antd';
import { useNavigate, useLocation, Outlet } from 'react-router-dom';
import {
    DashboardOutlined,
    CalendarOutlined,
    DollarOutlined,
    UserOutlined,
    LogoutOutlined,
    BellOutlined,
} from '@ant-design/icons';
import { useAuth } from '../../contexts/AuthContext';
import { useMessage } from '../../hooks/useMessage';

const { Sider, Content, Header } = Layout;

const AdminLayout = () => {
    const navigate = useNavigate();
    const location = useLocation();
    const { user, logout } = useAuth();
    const { success, contextHolder } = useMessage();
    const [collapsed, setCollapsed] = useState(false);

    const handleLogout = () => {
        logout();
        success('Đăng xuất thành công');
        navigate('/');
    };

    const handleProfile = () => {
        navigate('/admin/profile');
    };

    const handleUserMenuClick = ({ key }) => {
        switch (key) {
            case 'profile':
                handleProfile();
                break;
            case 'invitations':
                navigate('/invitations');
                break;
            case 'logout':
                handleLogout();
                break;
            default:
                break;
        }
    };

    const menuItems = [
        {
            key: '/admin',
            icon: <DashboardOutlined />,
            label: 'Dashboard',
        },
        {
            key: '/admin/events',
            icon: <CalendarOutlined />,
            label: 'Quản lý Event',
        },
        {
            key: '/admin/revenue',
            icon: <DollarOutlined />,
            label: 'Quản lý Doanh thu / Checkout',
        },
        {
            key: '/admin/users',
            icon: <UserOutlined />,
            label: 'Quản lý Người dùng',
        },
    ];

    const handleMenuClick = ({ key }) => {
        navigate(key);
    };

    // Xác định selectedKey dựa trên pathname hiện tại
    // Ưu tiên match chính xác, sau đó match prefix dài nhất
    const getSelectedKey = () => {
        const path = location.pathname;
        
        // Kiểm tra exact match trước
        const exactMatch = menuItems.find(item => item.key === path);
        if (exactMatch) return exactMatch.key;
        
        // Nếu không có exact match, tìm item có key là prefix của path
        // Sắp xếp theo độ dài giảm dần để match prefix dài nhất trước
        const sortedItems = [...menuItems].sort((a, b) => b.key.length - a.key.length);
        const prefixMatch = sortedItems.find(item => path.startsWith(item.key + '/') || path === item.key);
        if (prefixMatch) return prefixMatch.key;
        
        // Default về /admin
        return '/admin';
    };

    const selectedKey = getSelectedKey();

    return (
        <Layout style={{ minHeight: '100vh' }}>
            <Sider
                collapsible
                collapsed={collapsed}
                onCollapse={setCollapsed}
                width={250}
                style={{
                    overflow: 'auto',
                    height: '100vh',
                    position: 'fixed',
                    left: 0,
                    top: 0,
                    bottom: 0,
                }}
            >
                <div style={{ 
                    height: 64, 
                    margin: 16, 
                    background: 'rgba(255, 255, 255, 0.2)',
                    borderRadius: 6,
                    display: 'flex',
                    alignItems: 'center',
                    justifyContent: 'center',
                    color: 'white',
                    fontWeight: 'bold',
                    fontSize: collapsed ? 14 : 18
                }}>
                    {collapsed ? 'EQ' : 'EventIQ'}
                </div>
                <Menu
                    theme="dark"
                    mode="inline"
                    selectedKeys={[selectedKey]}
                    items={menuItems}
                    onClick={handleMenuClick}
                />
            </Sider>
            <Layout style={{ marginLeft: collapsed ? 80 : 250, transition: 'all 0.2s' }}>
                <Header style={{ 
                    background: '#fff', 
                    padding: '0 24px', 
                    display: 'flex', 
                    justifyContent: 'flex-end', 
                    alignItems: 'center',
                    boxShadow: '0 2px 8px rgba(0,0,0,0.1)'
                }}>
                    <Space size="middle">
                        <Dropdown
                            menu={{
                                items: [
                                    {
                                        key: 'profile',
                                        icon: <UserOutlined />,
                                        label: 'Profile',
                                    },
                                    {
                                        key: 'invitations',
                                        icon: <BellOutlined />,
                                        label: 'My Invitations',
                                    },
                                    {
                                        type: 'divider',
                                    },
                                    {
                                        key: 'logout',
                                        icon: <LogoutOutlined />,
                                        label: 'Logout',
                                    },
                                ],
                                onClick: handleUserMenuClick,
                            }}
                            placement="bottomRight"
                            arrow
                        >
                            <Space className="cursor-pointer" style={{ padding: '8px 12px', borderRadius: 4 }}>
                                <Avatar 
                                    size="default" 
                                    icon={<UserOutlined />} 
                                    style={{ backgroundColor: '#1890ff' }}
                                />
                                <span style={{ fontWeight: 500 }}>
                                    {user?.firstName} {user?.lastName}
                                </span>
                            </Space>
                        </Dropdown>
                    </Space>
                </Header>
                <Content style={{ margin: '24px 16px', padding: 24, background: '#fff', minHeight: 280 }}>
                    {contextHolder}
                    <Outlet />
                </Content>
            </Layout>
        </Layout>
    );
};

export default AdminLayout;

