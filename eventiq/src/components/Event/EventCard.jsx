import React from 'react';
import { Card, Typography, Tag } from 'antd';
import { CalendarOutlined, DollarOutlined, ShopOutlined, EnvironmentOutlined } from '@ant-design/icons';
import dayjs from 'dayjs';

const { Title, Text } = Typography;

const EventCard = ({ event, onClick }) => {
  const formatPrice = (price) => {
    if (!price) return 'Price TBD';
    return new Intl.NumberFormat('vi-VN', {
      style: 'currency',
      currency: 'VND',
    }).format(price);
  };

  const isUpcoming = dayjs(event.start).isAfter(dayjs());

  return (
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
      onClick={() => onClick(event.id)}
    >
      <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'start', marginBottom: 8 }}>
        <Title level={4} style={{ margin: 0, flex: 1 }}>
          {event.name}
        </Title>
        <Tag color={isUpcoming ? 'green' : 'default'} style={{ marginLeft: 8 }}>
          {isUpcoming ? 'Upcoming' : 'Past'}
        </Tag>
      </div>
      <div style={{ marginBottom: 8 }}>
        <CalendarOutlined />{' '}
        <Text>{dayjs(event.start).format('DD/MM/YYYY HH:mm')}</Text>
      </div>
      <div style={{ marginBottom: 8 }}>
        <ShopOutlined /> <Text>{event.organizationName}</Text>
      </div>
      {event.provinceName && (
        <div style={{ marginBottom: 8 }}>
          <EnvironmentOutlined /> <Text>{event.provinceName}</Text>
        </div>
      )}
      <div>
        <DollarOutlined />{' '}
        <Tag color="green" style={{ fontSize: 14 }}>
          {formatPrice(event.lowestPrice)}
        </Tag>
      </div>
    </Card>
  );
};

export default EventCard;

