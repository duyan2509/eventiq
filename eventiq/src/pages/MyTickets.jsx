import React, { useState, useEffect } from 'react';
import { Typography, Card, Table, Tag, Space, DatePicker, Empty, Modal, Tabs, Button, Badge } from 'antd';
import { IdcardOutlined } from '@ant-design/icons';
import { revenueAPI, checkinAPI, transferAPI } from '../services/api';
import dayjs from 'dayjs';
import CheckInRequestButton from '../components/Ticket/CheckInRequestButton';
import PasswordVerificationForm from '../components/Ticket/PasswordVerificationForm';
import OtpDisplayPanel from '../components/Ticket/OtpDisplayPanel';
import TransferTicketForm from '../components/Ticket/TransferTicketForm';
import { useMessage} from '../hooks/useMessage';
import { signalRService } from '../services/signalr';
const { Title } = Typography;
const { RangePicker } = DatePicker;

const MyTickets = () => {
  const [tickets, setTickets] = useState([]);
  const [loading, setLoading] = useState(true);
  const [selectedTicketId, setSelectedTicketId] = useState(null);
  const [showPasswordForm, setShowPasswordForm] = useState(false);
  const [passwordLoading, setPasswordLoading] = useState(false);
  const [checkInData, setCheckInData] = useState(null);
  const [otpExpiresIn, setOtpExpiresIn] = useState(90);
  const intervalRef = React.useRef(null);
  const [selectedTicketForTransfer, setSelectedTicketForTransfer] = useState(null);
  const [showTransferForm, setShowTransferForm] = useState(false);
  const [transferLoading, setTransferLoading] = useState(false);
  const [incomingTransfers, setIncomingTransfers] = useState([]);
  const [transfersLoading, setTransfersLoading] = useState(false);
  const [transfersPagination, setTransfersPagination] = useState({
    current: 1,
    pageSize: 10,
    total: 0,
  });
  const [previousPendingCount, setPreviousPendingCount] = useState(0);
  const { error, success, contextHolder } = useMessage();
  useEffect(() => {
    fetchTickets();
    fetchIncomingTransfers(1, 10);
    
    const token = localStorage.getItem('token');
    if (token) {
      signalRService.connect(token);
    }

    const handleTicketCheckedIn = (data) => {
      if (intervalRef.current) {
        clearInterval(intervalRef.current);
        intervalRef.current = null;
      }
      setCheckInData(null);
      success(data.message || 'Ticket checked in successfully!');
      fetchTickets();
    };

    const handleTransferRequestReceived = () => {
      fetchIncomingTransfers(transfersPagination.current, transfersPagination.pageSize);
      success('You have received a new transfer request');
    };

    signalRService.on('TicketCheckedIn', handleTicketCheckedIn);
    signalRService.on('TransferRequestReceived', handleTransferRequestReceived);

    return () => {
      signalRService.off('TicketCheckedIn', handleTicketCheckedIn);
      signalRService.off('TransferRequestReceived', handleTransferRequestReceived);
    };
  }, []);

  const fetchTickets = async () => {
    try {
      setLoading(true);
      const data = await revenueAPI.getUserTickets();
      setTickets(data || []);
    } catch (error) {
      console.error('Error fetching tickets:', error);
    } finally {
      setLoading(false);
    }
  };

  const fetchIncomingTransfers = async (page = 1, pageSize = 10) => {
    try {
      setTransfersLoading(true);
      const data = await transferAPI.getIncomingTransfers(page, pageSize);
      const transfers = data.data || [];
      setIncomingTransfers(transfers);
      
      const pendingCount = transfers.filter(
        transfer => transfer.status === 'PENDING' && dayjs(transfer.expiresAt).isAfter(dayjs())
      ).length;
      
      if (previousPendingCount > 0 && pendingCount > previousPendingCount) {
        success(`You have ${pendingCount - previousPendingCount} new transfer request(s)`);
      }
      
      setPreviousPendingCount(pendingCount);
      
      setTransfersPagination({
        current: data.page || page,
        pageSize: data.size || pageSize,
        total: data.total || 0,
      });
    } catch (err) {
      console.error('Error fetching incoming transfers:', err);
    } finally {
      setTransfersLoading(false);
    }
  };

  const formatPrice = (price) => {
    return new Intl.NumberFormat('vi-VN', {
      style: 'currency',
      currency: 'VND',
    }).format(price);
  };

  const handleRequestCheckIn = (ticketId) => {
    setSelectedTicketId(ticketId);
    setShowPasswordForm(true);
  };

  const handlePasswordConfirm = async (password) => {
    try {
      setPasswordLoading(true);
      const result = await checkinAPI.requestCheckIn(selectedTicketId, password);
      console.log('Check-in response:', result);
      if (result && (result.ticketCode || result.TicketCode) && (result.otp || result.Otp)) {
        if (intervalRef.current) {
          clearInterval(intervalRef.current);
        }
        setCheckInData(result);
        setShowPasswordForm(false);
        setOtpExpiresIn(90);
        
        const interval = setInterval(() => {
          setOtpExpiresIn((prev) => {
            if (prev <= 1) {
              clearInterval(interval);
              intervalRef.current = null;
              return 0;
            }
            return prev - 1;
          });
        }, 1000);
        intervalRef.current = interval;
        
        setTimeout(() => {
          setCheckInData(null);
          if (intervalRef.current) {
            clearInterval(intervalRef.current);
          }
          intervalRef.current = null;
        }, 90000);
        
        success('Check-in request created successfully');
      } else {
        console.error('Invalid response:', result);
        error('Invalid response format');
      }
    } catch (err) {
      console.error('Check-in error:', err);
      error(err.response?.data?.message || 'Failed to request check-in');
    } finally {
      setPasswordLoading(false);
    }
  };

  const handlePasswordCancel = () => {
    setShowPasswordForm(false);
    setSelectedTicketId(null);
  };

  const handleTransferClick = (ticketId) => {
    setSelectedTicketForTransfer(ticketId);
    setShowTransferForm(true);
  };

  const handleTransferConfirm = async (toUserEmail, password) => {
    try {
      setTransferLoading(true);
      await transferAPI.transferTicket(selectedTicketForTransfer, toUserEmail, password);
      success('Transfer request created successfully');
      setShowTransferForm(false);
      setSelectedTicketForTransfer(null);
      fetchTickets();
      fetchIncomingTransfers(transfersPagination.current, transfersPagination.pageSize);
    } catch (err) {
      error(err.response?.data?.message || 'Failed to create transfer request');
    } finally {
      setTransferLoading(false);
    }
  };

  const handleTransferCancel = () => {
    setShowTransferForm(false);
    setSelectedTicketForTransfer(null);
  };

  const handleAcceptTransfer = async (transferId) => {
    try {
      await transferAPI.acceptTransfer(transferId);
      success('Transfer accepted successfully');
      fetchIncomingTransfers(transfersPagination.current, transfersPagination.pageSize);
      fetchTickets();
    } catch (err) {
      error(err.response?.data?.message || 'Failed to accept transfer');
    }
  };

  const handleRejectTransfer = async (transferId) => {
    try {
      await transferAPI.rejectTransfer(transferId);
      success('Transfer rejected successfully');
      fetchIncomingTransfers(transfersPagination.current, transfersPagination.pageSize);
    } catch (err) {
      error(err.response?.data?.message || 'Failed to reject transfer');
    }
  };

  const handleTransfersTableChange = (newPagination) => {
    fetchIncomingTransfers(newPagination.current, newPagination.pageSize);
  };


  useEffect(() => {
    return () => {
      if (intervalRef.current) {
        clearInterval(intervalRef.current);
      }
    };
  }, []);

  const columns = [
    {
      title: 'Event',
      dataIndex: 'eventName',
      key: 'eventName',
    },
    {
      title: 'Event Item',
      dataIndex: 'eventItemName',
      key: 'eventItemName',
    },
    {
      title: 'Ticket Class',
      dataIndex: 'ticketClassName',
      key: 'ticketClassName',
    },
    {
      title: 'Ticket Code',
      dataIndex: 'ticketCode',
      key: 'ticketCode',
      render: (_, record) => record.ticketCode || record.TicketCode || '',
    },
    {
      title: 'Price',
      dataIndex: 'price',
      key: 'price',
      render: (price) => formatPrice(price),
    },
    {
      title: 'Purchase Date',
      dataIndex: 'purchaseDate',
      key: 'purchaseDate',
      render: (date) => dayjs(date).format('DD/MM/YYYY HH:mm'),
    },
    {
      title: 'Event Date',
      key: 'eventDate',
      render: (_, record) => (
        <span>
          {dayjs(record.eventStartDate).format('DD/MM/YYYY')} - {dayjs(record.eventEndDate).format('DD/MM/YYYY')}
        </span>
      ),
    },
    {
      title: 'Status',
      dataIndex: 'status',
      key: 'status',
      render: (status) => (
        <Tag color={status === 'UPCOMING' ? 'green' : 'default'}>
          {status === 'UPCOMING' ? 'Upcoming' : 'Expired'}
        </Tag>
      ),
    },
    {
      title: 'Actions',
      key: 'actions',
      render: (_, record) => (
        <Space>
          <CheckInRequestButton
            onClick={() => handleRequestCheckIn(record.ticketId)}
            disabled={(record.ticketStatus || record.TicketStatus) === 'USED' || record.status !== 'UPCOMING'}
          />
          <Button
            onClick={() => handleTransferClick(record.ticketId)}
            disabled={(record.ticketStatus || record.TicketStatus) === 'USED' || record.status !== 'UPCOMING'}
          >
            Transfer
          </Button>
        </Space>
      ),
    },
  ];

  const transferColumns = [
    {
      title: 'Sender',
      dataIndex: 'senderName',
      key: 'senderName',
    },
    {
      title: 'Event Name',
      dataIndex: 'eventName',
      key: 'eventName',
    },
    {
      title: 'Event Date',
      dataIndex: 'eventDate',
      key: 'eventDate',
      render: (date) => dayjs(date).format('DD/MM/YYYY HH:mm'),
    },
    {
      title: 'Status',
      dataIndex: 'status',
      key: 'status',
      render: (status) => (
        <Tag color={
          status === 'PENDING' ? 'orange' :
          status === 'ACCEPTED' ? 'green' :
          status === 'REJECTED' ? 'red' : 'default'
        }>
          {status}
        </Tag>
      ),
    },
    {
      title: 'Expires At',
      dataIndex: 'expiresAt',
      key: 'expiresAt',
      render: (date) => dayjs(date).format('DD/MM/YYYY HH:mm'),
    },
    {
      title: 'Actions',
      key: 'actions',
      render: (_, record) => {
        if (record.status === 'PENDING' && dayjs(record.expiresAt).isAfter(dayjs())) {
          return (
            <Space>
              <Button type="primary" onClick={() => handleAcceptTransfer(record.id)}>
                Accept
              </Button>
              <Button danger onClick={() => handleRejectTransfer(record.id)}>
                Reject
              </Button>
            </Space>
          );
        }
        return null;
      },
    },
  ];

  return (
    <div style={{ padding: '24px' }}>
      {contextHolder}
      <Title level={2}>
         My Tickets
      </Title>
      <Card>
        <Tabs
          defaultActiveKey="tickets"
          items={[
            {
              key: 'tickets',
              label: 'My Tickets',
              children: (
                <Table
                  columns={columns}
                  dataSource={tickets}
                  rowKey="ticketId"
                  loading={loading}
                  pagination={{ pageSize: 10 }}
                  locale={{
                    emptyText: <Empty description="No tickets found" />,
                  }}
                />
              ),
            },
            {
              key: 'transfers',
              label: (() => {
                const pendingCount = incomingTransfers.filter(
                  transfer => transfer.status === 'PENDING' && dayjs(transfer.expiresAt).isAfter(dayjs())
                ).length;
                return (
                  <span>
                    Incoming Transfers
                    {pendingCount > 0 && (
                      <Badge count={pendingCount} size="small" style={{ marginLeft: 8 }} />
                    )}
                  </span>
                );
              })(),
              children: (
                <Table
                  columns={transferColumns}
                  dataSource={incomingTransfers}
                  rowKey="id"
                  loading={transfersLoading}
                  pagination={{
                    current: transfersPagination.current,
                    pageSize: transfersPagination.pageSize,
                    total: transfersPagination.total,
                    showSizeChanger: true,
                    showQuickJumper: true,
                    pageSizeOptions: ['10', '20', '50', '100'],
                    showTotal: (total, range) => `${range[0]}-${range[1]} of ${total} transfers`,
                  }}
                  onChange={handleTransfersTableChange}
                  locale={{
                    emptyText: <Empty description="No incoming transfers" />,
                  }}
                />
              ),
            },
          ]}
        />
      </Card>
      <Modal
        title="Check-in Information"
        open={!!checkInData}
        footer={null}
        closable={false}
        maskClosable={false}
        width={500}
      >
        {checkInData && (
          <OtpDisplayPanel
            ticketCode={checkInData.ticketCode || checkInData.TicketCode || ''}
            otp={checkInData.otp || checkInData.Otp || ''}
            expiresIn={otpExpiresIn}
          />
        )}
      </Modal>
      <PasswordVerificationForm
        visible={showPasswordForm}
        onCancel={handlePasswordCancel}
        onConfirm={handlePasswordConfirm}
        loading={passwordLoading}
      />
      <TransferTicketForm
        visible={showTransferForm}
        onCancel={handleTransferCancel}
        onConfirm={handleTransferConfirm}
        loading={transferLoading}
      />
    </div>
  );
};

export default MyTickets;

