import React, { useState, useEffect } from 'react';
import { useParams, useNavigate } from 'react-router-dom';
import {
  Card,
  Typography,
  Spin,
  Empty,
  Collapse,
  Button,
  Tag,
  Descriptions,
} from 'antd';
import {
  CalendarOutlined,
  ShopOutlined,
  EnvironmentOutlined,
  FileTextOutlined,
} from '@ant-design/icons';
import dayjs from 'dayjs';
import { customerAPI } from '../services/api';
import DraftContentRenderer from '../components/Event/DraftContentRenderer';
import { useAuth } from '../contexts/AuthContext';
import LoginModal from '../components/Auth/LoginModal';

const { Title, Text } = Typography;
const { Panel } = Collapse;

const CustomerEventDetail = () => {
  const { eventId } = useParams();
  const navigate = useNavigate();
  const { isAuthenticated } = useAuth();
  const [loading, setLoading] = useState(true);
  const [event, setEvent] = useState(null);
  const [loginModalVisible, setLoginModalVisible] = useState(false);
  const [pendingEventItemId, setPendingEventItemId] = useState(null);

  useEffect(() => {
    if (eventId) {
      fetchEventDetail();
    }
  }, [eventId]);

  const fetchEventDetail = async () => {
    try {
      setLoading(true);
      const data = await customerAPI.getPublishedEventDetail(eventId);
      setEvent(data);
    } catch (error) {
      console.error('Error fetching event detail:', error);
    } finally {
      setLoading(false);
    }
  };

  const formatPrice = (price) => {
    if (!price) return 'Price TBD';
    return new Intl.NumberFormat('vi-VN', {
      style: 'currency',
      currency: 'VND',
    }).format(price);
  };

  const handleBuyTicket = (eventItemId) => {
    if (!isAuthenticated) {
      setPendingEventItemId(eventItemId);
      setLoginModalVisible(true);
      return;
    }
    navigate(`/events/${eventId}/items/${eventItemId}/seats`);
  };

  const handleLoginSuccess = () => {
    setLoginModalVisible(false);
    if (pendingEventItemId) {
      navigate(`/events/${eventId}/items/${pendingEventItemId}/seats`);
      setPendingEventItemId(null);
    }
  };

  if (loading) {
    return (
      <div style={{ textAlign: 'center', padding: '50px' }}>
        <Spin size="large" />
      </div>
    );
  }

  if (!event) {
    return (
      <div style={{ padding: '50px', textAlign: 'center' }}>
        <Empty description="Event not found" />
      </div>
    );
  }

  return (
    <div style={{ padding: '24px', maxWidth: '1200px', margin: '0 auto' }}>
      <Card>
        <div style={{ width: '100%', marginBottom: 24, marginLeft: '-24px', marginRight: '-24px', marginTop: '-24px' }}>
          <img
            src={event.banner}
            alt={event.name}
            style={{ width: '100%', height: '400px', objectFit: 'cover', display: 'block', maxWidth: '1104px', margin: '0 auto' }}
          />
        </div>

        <Title level={2} style={{ marginBottom: 16 }}>
          {event.name}
        </Title>

        {event.description && (
          <div style={{ marginBottom: 24 }}>
            <DraftContentRenderer content={event.description} />
          </div>
        )}

        <Descriptions bordered column={{ xs: 1, sm: 2, md: 3 }}>
          <Descriptions.Item
            label={
              <span>
                <CalendarOutlined /> Date & Time
              </span>
            }
          >
            {dayjs(event.start).format('DD/MM/YYYY HH:mm')}
          </Descriptions.Item>
          <Descriptions.Item
            label={
              <span>
                <ShopOutlined /> Organization
              </span>
            }
          >
            {event.organizationName}
          </Descriptions.Item>
          <Descriptions.Item
            label={
              <span>
                <EnvironmentOutlined /> Venue
              </span>
            }
          >
            {event.eventAddress
              ? `${event.eventAddress.detail}, ${event.eventAddress.communeName}, ${event.eventAddress.provinceName}`
              : 'TBD'}
          </Descriptions.Item>
        </Descriptions>

        {event.ticketClasses && event.ticketClasses.length > 0 && (
          <div style={{ marginTop: 24 }}>
            <Title level={4} style={{ marginBottom: 12 }}>
              Ticket Classes
            </Title>
            <div style={{ display: 'flex', flexWrap: 'wrap', gap: 8 }}>
              {event.ticketClasses.map((ticketClass) => (
                <Tag
                  key={ticketClass.id}
                  color={ticketClass.color || 'blue'}
                  style={{ fontSize: 14, padding: '4px 12px' }}
                >
                  {ticketClass.name} - {formatPrice(ticketClass.price)}
                </Tag>
              ))}
            </div>
          </div>
        )}

        <div style={{ marginTop: 32 }}>
          <Title level={3} style={{ marginBottom: 16 }}>
            <FileTextOutlined /> Sessions
          </Title>

          {event.eventItems && event.eventItems.length > 0 ? (
            <Collapse>
              {event.eventItems.map((item) => (
                <Panel
                  key={item.id}
                  header={
                    <Text strong>{item.name}</Text>
                  }
                >
                  {item.description && (
                    <div style={{ marginBottom: 16 }}>
                      <DraftContentRenderer content={item.description} />
                    </div>
                  )}
                  <Descriptions column={2} size="small">
                    <Descriptions.Item label="Start Time">
                      {dayjs(item.start).format('DD/MM/YYYY HH:mm')}
                    </Descriptions.Item>
                    <Descriptions.Item label="End Time">
                      {dayjs(item.end).format('DD/MM/YYYY HH:mm')}
                    </Descriptions.Item>
                  </Descriptions>
                  <div style={{ marginTop: 16, textAlign: 'right' }}>
                    <Button
                      type="primary"
                      size="large"
                      onClick={() => handleBuyTicket(item.id)}
                      disabled={dayjs().isAfter(dayjs(item.start))}
                    >
                      Buy Ticket
                    </Button>
                  </div>
                </Panel>
              ))}
            </Collapse>
          ) : (
            <Empty description="No sessions available" />
          )}
        </div>
      </Card>

      <LoginModal
        visible={loginModalVisible}
        onCancel={() => {
          setLoginModalVisible(false);
          setPendingEventItemId(null);
        }}
        onSuccess={handleLoginSuccess}
      />
    </div>
  );
};

export default CustomerEventDetail;
