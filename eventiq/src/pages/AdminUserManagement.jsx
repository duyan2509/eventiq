import React, { useState, useEffect } from 'react';
import { Typography, Card, Table, Space, Input, Button, Tag, Modal, message } from 'antd';
import { UserOutlined, StopOutlined, CheckCircleOutlined, SearchOutlined } from '@ant-design/icons';
import { adminAPI } from '../services/api';
import { useMessage } from '../hooks/useMessage';

const { Title } = Typography;
const { TextArea } = Input;

const AdminUserManagement = () => {
  const [users, setUsers] = useState([]);
  const [loading, setLoading] = useState(false);
  const [pagination, setPagination] = useState({
    current: 1,
    pageSize: 10,
    total: 0,
  });
  const [emailSearch, setEmailSearch] = useState('');
  const [banModalVisible, setBanModalVisible] = useState(false);
  const [unbanModalVisible, setUnbanModalVisible] = useState(false);
  const [selectedUser, setSelectedUser] = useState(null);
  const [banReason, setBanReason] = useState('');
  const { success, error, contextHolder } = useMessage();

  useEffect(() => {
    fetchUsers();
  }, [pagination.current, pagination.pageSize, emailSearch]);

  const fetchUsers = async () => {
    try {
      setLoading(true);
      const data = await adminAPI.getUsers(pagination.current, pagination.pageSize, emailSearch || null);
      setUsers(data.data || []);
      setPagination(prev => ({
        ...prev,
        total: data.total || 0,
      }));
    } catch (err) {
      error(err.response?.data?.message || 'Failed to fetch users');
    } finally {
      setLoading(false);
    }
  };

  const handleTableChange = (newPagination) => {
    setPagination(prev => ({
      ...prev,
      current: newPagination.current,
      pageSize: newPagination.pageSize,
    }));
  };

  const handleSearch = (value) => {
    setEmailSearch(value);
    setPagination(prev => ({ ...prev, current: 1 }));
  };

  const handleBanClick = (user) => {
    setSelectedUser(user);
    setBanReason('');
    setBanModalVisible(true);
  };

  const handleUnbanClick = (user) => {
    setSelectedUser(user);
    setUnbanModalVisible(true);
  };

  const handleBanConfirm = async () => {
    try {
      await adminAPI.banUser(selectedUser.id, banReason || null);
      success('User banned successfully');
      setBanModalVisible(false);
      setSelectedUser(null);
      setBanReason('');
      fetchUsers();
    } catch (err) {
      error(err.response?.data?.message || 'Failed to ban user');
    }
  };

  const handleUnbanConfirm = async () => {
    try {
      await adminAPI.unbanUser(selectedUser.id);
      success('User unbanned successfully');
      setUnbanModalVisible(false);
      setSelectedUser(null);
      fetchUsers();
    } catch (err) {
      error(err.response?.data?.message || 'Failed to unban user');
    }
  };

  const columns = [
    {
      title: 'Email',
      dataIndex: 'email',
      key: 'email',
    },
    {
      title: 'Roles',
      dataIndex: 'roles',
      key: 'roles',
      render: (roles) => (
        <Space>
          {roles?.map((role, index) => (
            <Tag key={index} color={role === 'Admin' ? 'red' : role === 'Org' ? 'blue' : 'green'}>
              {role}
            </Tag>
          ))}
        </Space>
      ),
    },
    {
      title: 'Status',
      dataIndex: 'isBanned',
      key: 'isBanned',
      render: (isBanned) => (
        <Tag color={isBanned ? 'red' : 'green'}>
          {isBanned ? 'Banned' : 'Active'}
        </Tag>
      ),
    },
    {
      title: 'Actions',
      key: 'actions',
      render: (_, record) => (
        record.roles.includes('Admin') ? null : (
          <Space>
            {record.isBanned ? (
              <Button
                type="primary"
                icon={<CheckCircleOutlined />}
                onClick={() => handleUnbanClick(record)}
              >
                Unban
              </Button>
            ) : (
              <Button
                danger
                icon={<StopOutlined />}
                onClick={() => handleBanClick(record)}
              >
                Ban
              </Button>
            )}
          </Space>
        )
      ),
    },
  ];

  return (
    <div style={{ padding: '24px' }}>
      {contextHolder}
      <Title level={2}>
        <UserOutlined /> User Management
      </Title>
      <Card>
        <Space style={{ marginBottom: 16, width: '100%' }} direction="vertical" size="middle">
          <Input
            placeholder="Search by email"
            prefix={<SearchOutlined />}
            value={emailSearch}
            onChange={(e) => handleSearch(e.target.value)}
            allowClear
            style={{ width: 300 }}
          />
        </Space>
        <Table
          columns={columns}
          dataSource={users}
          rowKey="id"
          loading={loading}
          pagination={{
            current: pagination.current,
            pageSize: pagination.pageSize,
            total: pagination.total,
            showSizeChanger: true,
            showQuickJumper: true,
            pageSizeOptions: ['10', '20', '50', '100'],
            showTotal: (total, range) => `${range[0]}-${range[1]} of ${total} users`,
          }}
          onChange={handleTableChange}
        />
      </Card>

      <Modal
        title="Ban User"
        open={banModalVisible}
        onOk={handleBanConfirm}
        onCancel={() => {
          setBanModalVisible(false);
          setSelectedUser(null);
          setBanReason('');
        }}
        okText="Ban"
        okButtonProps={{ danger: true }}
      >
        <p>Are you sure you want to ban user <strong>{selectedUser?.email}</strong>?</p>
        <TextArea
          placeholder="Ban reason (optional)"
          value={banReason}
          onChange={(e) => setBanReason(e.target.value)}
          rows={4}
          style={{ marginTop: 16 }}
        />
      </Modal>

      <Modal
        title="Unban User"
        open={unbanModalVisible}
        onOk={handleUnbanConfirm}
        onCancel={() => {
          setUnbanModalVisible(false);
          setSelectedUser(null);
        }}
        okText="Unban"
      >
        <p>Are you sure you want to unban user <strong>{selectedUser?.email}</strong>?</p>
      </Modal>
    </div>
  );
};

export default AdminUserManagement;
