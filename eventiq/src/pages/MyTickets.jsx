import React, { useState, useEffect } from 'react';
import { Typography, Card, Table, Tag, Space, DatePicker, Empty, Modal } from 'antd';
import { IdcardOutlined } from '@ant-design/icons';
import { revenueAPI, checkinAPI } from '../services/api';
import dayjs from 'dayjs';
import CheckInRequestButton from '../components/Ticket/CheckInRequestButton';
import PasswordVerificationForm from '../components/Ticket/PasswordVerificationForm';
import OtpDisplayPanel from '../components/Ticket/OtpDisplayPanel';
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
  const { error, success, contextHolder } = useMessage();
  useEffect(() => {
    fetchTickets();
    
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

    signalRService.on('TicketCheckedIn', handleTicketCheckedIn);

    return () => {
      signalRService.off('TicketCheckedIn', handleTicketCheckedIn);
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
    } catch (error) {
      console.error('Check-in error:', error);
      error(error.response?.data?.message || 'Failed to request check-in');
    } finally {
      setPasswordLoading(false);
    }
  };

  const handlePasswordCancel = () => {
    setShowPasswordForm(false);
    setSelectedTicketId(null);
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
        <CheckInRequestButton
          onClick={() => handleRequestCheckIn(record.ticketId)}
          disabled={(record.ticketStatus || record.TicketStatus) === 'USED' || record.status !== 'UPCOMING'}
        />
      ),
    },
  ];

  return (
    <div style={{ padding: '24px' }}>
      {contextHolder}
      <Title level={2}>
        <IdcardOutlined /> My Tickets
      </Title>
      <Card>
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
    </div>
  );
};

export default MyTickets;

