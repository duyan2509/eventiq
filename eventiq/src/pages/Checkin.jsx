import React, { useState, useEffect } from 'react';
import { useParams, useNavigate } from 'react-router-dom';
import { Card, Table, Select, Button, Space, Typography, Spin, message, Row, Col } from 'antd';
import { LoginOutlined, ReloadOutlined } from '@ant-design/icons';
import { checkinAPI, eventAPI } from '../services/api';
import { useMessage } from '../hooks/useMessage';
import dayjs from 'dayjs';
import CheckInForm from '../components/Staff/CheckInForm';
import CheckInResultPanel from '../components/Staff/CheckInResultPanel';

const { Title } = Typography;

const Checkin = () => {
  const { eventId, eventItemId } = useParams();
  const navigate = useNavigate();
  const { success, error } = useMessage();
  const [loading, setLoading] = useState(false);
  const [eventItems, setEventItems] = useState([]);
  const [selectedEventItemId, setSelectedEventItemId] = useState(eventItemId || null);
  const [checkins, setCheckins] = useState([]);
  const [checkinLoading, setCheckinLoading] = useState(false);
  const [checkInResult, setCheckInResult] = useState(null);
  const [pagination, setPagination] = useState({
    current: 1,
    pageSize: 10,
    total: 0,
  });

  useEffect(() => {
    if (eventId) {
      fetchEventItems();
    }
  }, [eventId]);

  useEffect(() => {
    if (eventId && selectedEventItemId) {
      fetchCheckins(pagination.current, pagination.pageSize);
    }
  }, [eventId, selectedEventItemId]);

  const fetchEventItems = async () => {
    try {
      setLoading(true);
      const items = await eventAPI.getEventItems(eventId);
      setEventItems(items || []);
      if (!selectedEventItemId && items && items.length > 0) {
        setSelectedEventItemId(items[0].id);
      }
    } catch (err) {
      error('Failed to load event items');
    } finally {
      setLoading(false);
    }
  };

  const fetchCheckins = async (page = 1, pageSize = 10) => {
    try {
      setCheckinLoading(true);
      const data = await checkinAPI.getCheckins(eventId, selectedEventItemId, page, pageSize);
      setCheckins(data.data || []);
      setPagination({
        current: data.page || page,
        pageSize: data.size || pageSize,
        total: data.total || 0,
      });
    } catch (err) {
      error('Failed to load checkins');
    } finally {
      setCheckinLoading(false);
    }
  };

  const handleTableChange = (newPagination) => {
    fetchCheckins(newPagination.current, newPagination.pageSize);
  };

  const handleCheckInSuccess = (result) => {
    setCheckInResult({ success: true, message: result.message, ticketId: result.ticketId });
    success('Check-in successful');
    fetchCheckins(pagination.current, pagination.pageSize);
    setTimeout(() => setCheckInResult(null), 3000);
  };

  const handleCheckInError = (errorMessage) => {
    setCheckInResult({ success: false, message: errorMessage });
    error(errorMessage);
    setTimeout(() => setCheckInResult(null), 3000);
  };

  const columns = [
    {
      title: 'Customer',
      key: 'customer',
      dataIndex: 'customerNameOrEmail',
      render: (text) => <Typography.Text>{text || 'N/A'}</Typography.Text>,
    },
    {
      title: 'Staff',
      key: 'staff',
      dataIndex: 'staffNameOrEmail',
      render: (text) => <Typography.Text>{text || 'N/A'}</Typography.Text>,
    },
    {
      title: 'Event Item',
      dataIndex: 'eventItemName',
      key: 'eventItemName',
    },
    {
      title: 'Check In Time',
      dataIndex: 'checkinTime',
      key: 'checkinTime',
      render: (time) => dayjs(time).format('DD/MM/YYYY HH:mm:ss'),
    },
  ];

  return (
    <div style={{ padding: '24px', maxWidth: '1400px', margin: '0 auto' }}>
      <Space direction="vertical" size="large" style={{ width: '100%' }}>
        <Title level={2}>
          <LoginOutlined /> Check In
        </Title>

        <Row gutter={16}>
          <Col xs={24} md={12}>
            <CheckInForm onSuccess={handleCheckInSuccess} onError={handleCheckInError} />
            {checkInResult && (
              <div style={{ marginTop: 16 }}>
                <CheckInResultPanel
                  success={checkInResult.success}
                  message={checkInResult.message}
                  ticketId={checkInResult.ticketId}
                />
              </div>
            )}
          </Col>
          <Col xs={24} md={12}>
            <Card>
              <Space direction="vertical" size="middle" style={{ width: '100%' }}>
                <Space>
                  <Typography.Text strong>Event Item:</Typography.Text>
                  <Select
                    style={{ width: 200 }}
                    value={selectedEventItemId}
                    onChange={setSelectedEventItemId}
                    loading={loading}
                    placeholder="Select event item"
                  >
                    {eventItems.map((item) => (
                      <Select.Option key={item.id} value={item.id}>
                        {item.name}
                      </Select.Option>
                    ))}
                  </Select>
                  <Button
                    icon={<ReloadOutlined />}
                    onClick={() => fetchCheckins(pagination.current, pagination.pageSize)}
                    loading={checkinLoading}
                  >
                    Refresh
                  </Button>
                </Space>
              </Space>
            </Card>
          </Col>
        </Row>

        <Card title="Check In Records">
          <Table
            columns={columns}
            dataSource={checkins}
            rowKey="id"
            loading={checkinLoading}
            pagination={{
              current: pagination.current,
              pageSize: pagination.pageSize,
              total: pagination.total,
              showSizeChanger: true,
              showQuickJumper: true,
              pageSizeOptions: ['10', '20', '50', '100'],
              showTotal: (total, range) => `${range[0]}-${range[1]} of ${total} checkins`,
            }}
            onChange={handleTableChange}
          />
        </Card>
      </Space>
    </div>
  );
};

export default Checkin;
