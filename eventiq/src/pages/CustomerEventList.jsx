import React, { useState, useEffect } from 'react';
import { Card, Row, Col, Tabs, Typography, Spin, Empty, Tag } from 'antd';
import { CalendarOutlined, DollarOutlined, ShopOutlined } from '@ant-design/icons';
import { useNavigate } from 'react-router-dom';
import dayjs from 'dayjs';
import { customerAPI } from '../services/api';

const { Title, Text } = Typography;
const { TabPane } = Tabs;

const CustomerEventList = () => {
  const [loading, setLoading] = useState(true);
  const [upcomingEvents, setUpcomingEvents] = useState([]);
  const [pastEvents, setPastEvents] = useState([]);
  const navigate = useNavigate();

  useEffect(() => {
    fetchEvents();
  }, []);

  const fetchEvents = async () => {
    try {
      setLoading(true);
      const data = await customerAPI.getPublishedEvents();
      setUpcomingEvents(data.upcomingEvents || []);
      setPastEvents(data.pastEvents || []);
    } catch (error) {
      console.error('Error fetching events:', error);
    } finally {
      setLoading(false);
    }
  };

  const handleEventClick = (eventId) => {
    navigate(`/events/${eventId}`);
  };

  const formatPrice = (price) => {
    if (!price) return 'Price TBD';
    return new Intl.NumberFormat('vi-VN', {
      style: 'currency',
      currency: 'VND',
    }).format(price);
  };

  const EventCard = ({ event }) => (
    <Card
      hoverable
      style={{ marginBottom: 16, height: '100%' }}
      cover={
        <img
          alt={event.name}
          src={event.banner}
          style={{ height: 200, objectFit: 'cover' }}
        />
      }
      onClick={() => handleEventClick(event.id)}
    >
      <Title level={4} style={{ marginBottom: 8 }}>
        {event.name}
      </Title>
      <div style={{ marginBottom: 8 }}>
        <CalendarOutlined />{' '}
        <Text>{dayjs(event.start).format('DD/MM/YYYY HH:mm')}</Text>
      </div>
      <div style={{ marginBottom: 8 }}>
        <ShopOutlined /> <Text>{event.organizationName}</Text>
      </div>
      <div>
        <DollarOutlined />{' '}
        <Tag color="green" style={{ fontSize: 14 }}>
          {formatPrice(event.lowestPrice)}
        </Tag>
      </div>
    </Card>
  );

  if (loading) {
    return (
      <div style={{ textAlign: 'center', padding: '50px' }}>
        <Spin size="large" />
      </div>
    );
  }

  return (
    <div style={{ padding: '24px', maxWidth: '1200px', margin: '0 auto' }}>
      <Title level={2} style={{ marginBottom: 24 }}>
        Events
      </Title>

      <Tabs defaultActiveKey="upcoming" size="large">
        <TabPane
          tab={`Upcoming (${upcomingEvents.length})`}
          key="upcoming"
        >
          {upcomingEvents.length === 0 ? (
            <Empty description="No upcoming events" />
          ) : (
            <Row gutter={[16, 16]}>
              {upcomingEvents.map((event) => (
                <Col xs={24} sm={12} md={8} lg={6} key={event.id}>
                  <EventCard event={event} />
                </Col>
              ))}
            </Row>
          )}
        </TabPane>

        <TabPane tab={`Past (${pastEvents.length})`} key="past">
          {pastEvents.length === 0 ? (
            <Empty description="No past events" />
          ) : (
            <Row gutter={[16, 16]}>
              {pastEvents.map((event) => (
                <Col xs={24} sm={12} md={8} lg={6} key={event.id}>
                  <EventCard event={event} />
                </Col>
              ))}
            </Row>
          )}
        </TabPane>
      </Tabs>
    </div>
  );
};

export default CustomerEventList;
