import React, { useEffect, useState } from 'react';
import { useNavigate } from 'react-router-dom';
import {
  Card,
  Table,
  Tag,
  Button,
  Space,
  Modal,
  message,
  Empty,
  Typography
} from 'antd';
import { CheckOutlined, CloseOutlined, CalendarOutlined, TeamOutlined } from '@ant-design/icons';
import { staffAPI } from '../services/api';
import { signalRService } from '../services/signalr';
import { useMessage } from '../hooks/useMessage';
import dayjs from 'dayjs';

const { Title, Text } = Typography;

const Invitations = () => {
  const navigate = useNavigate();
  const { success, error } = useMessage();
  const [loading, setLoading] = useState(false);
  const [invitations, setInvitations] = useState([]);
  const [modalVisible, setModalVisible] = useState(false);
  const [pendingAction, setPendingAction] = useState(null);

  useEffect(() => {
    fetchInvitations();
    
    // Setup SignalR for real-time updates
    const token = localStorage.getItem('token');
    if (token) {
      signalRService.connect(token);
    }

    // Register SignalR event handlers
    const handleStaffInvited = () => {
      fetchInvitations();
      message.info('You have a new invitation');
    };

    const handleStaffInvitationResponded = () => {
      fetchInvitations();
    };

    signalRService.on('StaffInvited', handleStaffInvited);
    signalRService.on('StaffInvitationResponded', handleStaffInvitationResponded);

    return () => {
      signalRService.off('StaffInvited', handleStaffInvited);
      signalRService.off('StaffInvitationResponded', handleStaffInvitationResponded);
    };
  }, []);

  const fetchInvitations = async () => {
    setLoading(true);
    try {
      const data = await staffAPI.getMyInvitations();
      setInvitations(data || []);
    } catch (err) {
      error('Failed to load invitations');
    } finally {
      setLoading(false);
    }
  };

  const handleRespondToInvitation = (invitationId, accept, eventName) => {
    console.log('handleRespondToInvitation called:', { invitationId, accept, eventName });
    setPendingAction({ invitationId, accept, eventName });
    setModalVisible(true);
  };

  const confirmRespondToInvitation = async () => {
    if (!pendingAction) return;
    
    const { invitationId, accept, eventName } = pendingAction;
    setModalVisible(false);
    
    try {
      setLoading(true);
      console.log('Calling API with:', { invitationId, accept });
      const result = await staffAPI.respondToInvitation(invitationId, accept);
      console.log('API response:', result);
      success(accept ? 'Invitation accepted successfully' : 'Invitation rejected');
      await fetchInvitations();
    } catch (err) {
      console.error('Error responding to invitation:', err);
      console.error('Error details:', {
        status: err.response?.status,
        statusText: err.response?.statusText,
        data: err.response?.data,
        message: err.message,
        url: err.config?.url,
        method: err.config?.method
      });
      const errorMessage = err.response?.data?.message || err.message || 'Failed to respond to invitation';
      error(errorMessage);
    } finally {
      setLoading(false);
      setPendingAction(null);
    }
  };

  const cancelRespondToInvitation = () => {
    console.log('Modal cancelled');
    setModalVisible(false);
    setPendingAction(null);
  };

  const getStatusColor = (status) => {
    const colorMap = {
      Pending: 'orange',
      Accepted: 'green',
      Rejected: 'red',
      Expired: 'gray'
    };
    return colorMap[status] || 'default';
  };

  const columns = [
    {
      title: 'Event',
      key: 'event',
      render: (_, record) => (
        <div>
          <div className="font-semibold">{record.eventName}</div>
          <div className="text-gray-500 text-sm">{record.organizationName}</div>
        </div>
      )
    },
    {
      title: 'Status',
      dataIndex: 'status',
      key: 'status',
      render: (status) => (
        <Tag color={getStatusColor(status)}>{status}</Tag>
      )
    },
    {
      title: 'Expires At',
      key: 'expiresAt',
      render: (_, record) => {
        const isExpired = dayjs(record.inviteExpiredAt).isBefore(dayjs());
        return (
          <div>
            <div className={isExpired ? 'text-red-500' : ''}>
              {dayjs(record.inviteExpiredAt).format('DD/MM/YYYY HH:mm')}
            </div>
            {isExpired && record.status === 'Pending' && (
              <Text type="danger" className="text-xs">Expired</Text>
            )}
          </div>
        );
      }
    },
    {
      title: 'Received At',
      key: 'receivedAt',
      render: (_, record) => (
        <div className="text-gray-500 text-sm">
          {dayjs(record.createdAt).format('DD/MM/YYYY HH:mm')}
        </div>
      )
    },
    {
      title: 'Actions',
      key: 'actions',
      render: (_, record) => {
        const isExpired = dayjs(record.inviteExpiredAt).isBefore(dayjs());
        const canRespond = record.status === 'Pending' && !isExpired;

        if (!canRespond) {
          return (
            <span className="text-gray-400">
              {record.status === 'Accepted' && 'Accepted'}
              {record.status === 'Rejected' && 'Rejected'}
              {isExpired && 'Expired'}
            </span>
          );
        }

        return (
          <Space>
            <Button
              type="primary"
              icon={<CheckOutlined />}
              loading={loading}
              onClick={(e) => {
                e.stopPropagation();
                console.log('Accept button clicked:', { invitationId: record.id, eventName: record.eventName });
                handleRespondToInvitation(record.id, true, record.eventName);
              }}
            >
              Accept
            </Button>
            <Button
              danger
              icon={<CloseOutlined />}
              loading={loading}
              onClick={(e) => {
                e.stopPropagation();
                console.log('Reject button clicked:', { invitationId: record.id, eventName: record.eventName });
                handleRespondToInvitation(record.id, false, record.eventName);
              }}
            >
              Reject
            </Button>
          </Space>
        );
      }
    }
  ];

  const pendingInvitations = invitations.filter(inv => inv.status === 'Pending');
  const otherInvitations = invitations.filter(inv => inv.status !== 'Pending');

  return (
    <div className="p-6">
      <div className="mb-6">
        <Title level={2}>
          <TeamOutlined className="mr-2" />
          My Invitations
        </Title>
        <Text type="secondary">
          View and manage your staff invitations for events
        </Text>
      </div>

      {invitations.length === 0 ? (
        <Card>
          <Empty
            description="No invitations found"
            image={Empty.PRESENTED_IMAGE_SIMPLE}
          />
        </Card>
      ) : (
        <>
          {pendingInvitations.length > 0 && (
            <Card
              title={
                <span>
                  <Tag color="orange" className="mr-2">
                    {pendingInvitations.length}
                  </Tag>
                  Pending Invitations
                </span>
              }
              className="mb-4"
            >
              <Table
                columns={columns}
                dataSource={pendingInvitations}
                rowKey="id"
                loading={loading}
                pagination={false}
              />
            </Card>
          )}

          {otherInvitations.length > 0 && (
            <Card
              title="All Invitations"
              className="mb-4"
            >
              <Table
                columns={columns}
                dataSource={otherInvitations}
                rowKey="id"
                loading={loading}
                pagination={{
                  pageSize: 10,
                  showSizeChanger: true,
                  showTotal: (total) => `Total ${total} invitations`
                }}
              />
            </Card>
          )}
        </>
      )}

      <Modal
        title={pendingAction ? (pendingAction.accept ? 'Accept Invitation' : 'Reject Invitation') : ''}
        open={modalVisible}
        onOk={confirmRespondToInvitation}
        onCancel={cancelRespondToInvitation}
        okText={pendingAction ? (pendingAction.accept ? 'Accept' : 'Reject') : 'OK'}
        cancelText="Cancel"
        confirmLoading={loading}
      >
        {pendingAction && (
          <p>
            Are you sure you want to {pendingAction.accept ? 'accept' : 'reject'} the invitation to work as staff for "{pendingAction.eventName}"?
          </p>
        )}
      </Modal>
    </div>
  );
};

export default Invitations;

