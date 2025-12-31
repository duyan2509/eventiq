import React, { useState, useEffect } from 'react';
import { Typography, Card, Table, Tag, Space, DatePicker, Empty } from 'antd';
import { IdcardOutlined } from '@ant-design/icons';
import { revenueAPI } from '../services/api';
import dayjs from 'dayjs';

const { Title } = Typography;
const { RangePicker } = DatePicker;

const MyTickets = () => {
  const [tickets, setTickets] = useState([]);
  const [loading, setLoading] = useState(true);

  useEffect(() => {
    fetchTickets();
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
  ];

  return (
    <div style={{ padding: '24px' }}>
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
    </div>
  );
};

export default MyTickets;

