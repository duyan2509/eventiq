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
  Image,
} from 'antd';
import {
  CalendarOutlined,
  DollarOutlined,
  ShopOutlined,
  EnvironmentOutlined,
  FileTextOutlined,
} from '@ant-design/icons';
import dayjs from 'dayjs';
import { customerAPI } from '../services/api';

const { Title, Paragraph, Text } = Typography;
const { Panel } = Collapse;

const CustomerEventDetail = () => {
  const { eventId } = useParams();
  const navigate = useNavigate();
  const [loading, setLoading] = useState(true);
  const [event, setEvent] = useState(null);

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
    navigate(`/events/${eventId}/items/${eventItemId}/seats`);
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
        <Image
          src={event.banner}
          alt={event.name}
          style={{ width: '100%', height: '400px', objectFit: 'cover', marginBottom: 24 }}
        />

        <Title level={2} style={{ marginBottom: 16 }}>
          {event.name}
        </Title>

        {event.description && (
          <Paragraph style={{ fontSize: 16, marginBottom: 24 }}>
            {event.description}
          </Paragraph>
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
                    <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center' }}>
                      <div>
                        <Text strong>{item.name}</Text>
                        {item.lowestPrice && (
                          <Tag color="green" style={{ marginLeft: 8 }}>
                            <DollarOutlined /> {formatPrice(item.lowestPrice)}
                          </Tag>
                        )}
                      </div>
                    </div>
                  }
                >
                  {item.description && (
                    <Paragraph style={{ marginBottom: 16 }}>
                      {item.description}
                    </Paragraph>
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
    </div>
  );
};

export default CustomerEventDetail;
