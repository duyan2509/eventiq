import React, { useState, useEffect } from 'react';
import { useNavigate } from 'react-router-dom';
import {
  Card,
  Typography,
  Descriptions,
  Button,
  Space,
  Alert,
  Empty,
} from 'antd';
import { DollarOutlined, ShoppingCartOutlined, ArrowLeftOutlined } from '@ant-design/icons';

const { Title, Text } = Typography;

const PaymentSkeleton = () => {
  const navigate = useNavigate();
  const [orderInfo, setOrderInfo] = useState(null);

  useEffect(() => {
    // Get order info from localStorage
    const selectedSeats = localStorage.getItem('selectedSeats');
    const eventId = localStorage.getItem('eventId');
    const eventItemId = localStorage.getItem('eventItemId');

    if (selectedSeats && eventId && eventItemId) {
      try {
        const seats = JSON.parse(selectedSeats);
        const totalPrice = seats.reduce((sum, seat) => sum + (seat.price || 0), 0);
        setOrderInfo({
          seats,
          eventId,
          eventItemId,
          totalPrice,
          seatCount: seats.length,
        });
      } catch (error) {
        console.error('Error parsing order info:', error);
      }
    }
  }, []);

  const formatPrice = (price) => {
    return new Intl.NumberFormat('vi-VN', {
      style: 'currency',
      currency: 'VND',
    }).format(price);
  };

  const handleBack = () => {
    navigate(-1);
  };

  if (!orderInfo) {
    return (
      <div style={{ padding: '50px', textAlign: 'center' }}>
        <Empty description="No order information available" />
        <Button type="primary" onClick={handleBack} style={{ marginTop: 16 }}>
          Back
        </Button>
      </div>
    );
  }

  return (
    <div style={{ padding: '24px', maxWidth: '800px', margin: '0 auto' }}>
      <Card>
        <Space direction="vertical" size="large" style={{ width: '100%' }}>
          <div>
            <Button
              icon={<ArrowLeftOutlined />}
              onClick={handleBack}
              style={{ marginBottom: 16 }}
            >
              Back
            </Button>
            <Title level={2}>
              <ShoppingCartOutlined /> Checkout
            </Title>
          </div>

          <Alert
            message="Notice"
            description="Payment feature is under development. Please check back later."
            type="info"
            showIcon
            style={{ marginBottom: 24 }}
          />

          <Card title="Order Summary" type="inner">
            <Descriptions bordered column={1}>
              <Descriptions.Item label="Number of Seats">
                {orderInfo.seatCount} seat(s)
              </Descriptions.Item>
              <Descriptions.Item label="Selected Seats">
                {orderInfo.seats.map((seat, index) => (
                  <Tag key={index} style={{ marginBottom: 4 }}>
                    {seat.label || seat.seatKey}
                    {seat.price && ` - ${formatPrice(seat.price)}`}
                  </Tag>
                ))}
              </Descriptions.Item>
              <Descriptions.Item
                label={
                  <span>
                    <DollarOutlined /> Total Amount
                  </span>
                }
              >
                <Text strong style={{ color: '#f5222d', fontSize: 20 }}>
                  {formatPrice(orderInfo.totalPrice)}
                </Text>
              </Descriptions.Item>
            </Descriptions>
          </Card>

          <Card title="Payment Information (Mock)" type="inner">
            <Descriptions bordered column={1}>
              <Descriptions.Item label="Payment Method">
                Not integrated yet
              </Descriptions.Item>
              <Descriptions.Item label="Status">
                <Text type="warning">Under development</Text>
              </Descriptions.Item>
            </Descriptions>
          </Card>

          <div style={{ textAlign: 'center', marginTop: 24 }}>
            <Button
              type="primary"
              size="large"
              disabled
              style={{ marginRight: 8 }}
            >
              Confirm Payment
            </Button>
            <Button size="large" onClick={handleBack}>
              Cancel
            </Button>
          </div>
        </Space>
      </Card>
    </div>
  );
};

export default PaymentSkeleton;
