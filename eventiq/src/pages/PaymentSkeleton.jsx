import React, { useState, useEffect } from 'react';
import { useNavigate, useSearchParams } from 'react-router-dom';
import { checkoutAPI, paymentAPI } from '../services/api';
import {
  Card,
  Typography,
  Descriptions,
  Button,
  Space,
  Alert,
  Empty,
  Tag,
  Row,
  Col,
} from 'antd';
import { DollarOutlined, ShoppingCartOutlined, ArrowLeftOutlined } from '@ant-design/icons';

const { Title, Text } = Typography;

const PaymentSkeleton = () => {
  const navigate = useNavigate();
  const [searchParams] = useSearchParams();
  const [orderInfo, setOrderInfo] = useState(null);
  const [checkoutId, setCheckoutId] = useState(null);
  const [loading, setLoading] = useState(false);
  const [paymentStatus, setPaymentStatus] = useState(null); // 'pending', 'success', 'failed'

  useEffect(() => {
    // Get order info from localStorage
    const selectedSeats = localStorage.getItem('selectedSeats');
    const eventId = localStorage.getItem('eventId');
    const eventItemId = localStorage.getItem('eventItemId');

    console.log('PaymentSkeleton: Loading order data from localStorage', {
      hasSelectedSeats: !!selectedSeats,
      eventId,
      eventItemId,
    });

    if (selectedSeats && eventId && eventItemId) {
      try {
        const seats = JSON.parse(selectedSeats);
        console.log('PaymentSkeleton: Parsed seats:', seats);
        
        if (!Array.isArray(seats) || seats.length === 0) {
          console.error('Invalid seats data:', seats);
          return;
        }
        
        const totalPrice = seats.reduce((sum, seat) => sum + (seat.price || 0), 0);
        setOrderInfo({
          seats,
          eventId,
          eventItemId,
          totalPrice,
          seatCount: seats.length,
        });
        console.log('PaymentSkeleton: Order info set successfully');
      } catch (error) {
        console.error('Error parsing order info:', error);
      }
    } else {
      console.warn('PaymentSkeleton: Missing required data in localStorage', {
        selectedSeats: !!selectedSeats,
        eventId: !!eventId,
        eventItemId: !!eventItemId,
      });
    }
  }, []);

  const formatPrice = (price) => {
    return new Intl.NumberFormat('vi-VN', {
      style: 'currency',
      currency: 'VND',
    }).format(price);
  };

  useEffect(() => {
    // Get checkout ID from localStorage
    const storedCheckoutId = localStorage.getItem('checkoutId');
    if (storedCheckoutId) {
      setCheckoutId(storedCheckoutId);
    }

    // Check if returning from VNPAY
    const vnpResponseCode = searchParams.get('vnp_ResponseCode');
    if (vnpResponseCode) {
      if (vnpResponseCode === '00') {
        setPaymentStatus('success');
      } else {
        setPaymentStatus('failed');
      }
    } else {
      setPaymentStatus('pending');
    }
  }, [searchParams]);

  const handleBack = async () => {
    // Cancel checkout if exists
    if (checkoutId) {
      try {
        await checkoutAPI.cancelCheckout(checkoutId);
        localStorage.removeItem('checkoutId');
      } catch (error) {
        console.error('Error canceling checkout:', error);
      }
    }
    navigate(-1);
  };

  const handleProceedToPayment = async () => {
    if (!checkoutId) {
      alert('Checkout ID not found');
      return;
    }

    setLoading(true);
    try {
      // Get return URL
      const returnUrl = `${window.location.origin}/payment`;
      
      // Create VNPAY payment URL
      const result = await paymentAPI.createPaymentUrl(checkoutId, returnUrl);
      console.log('Payment URL created:', result);
      
      // Redirect to VNPAY
      if (result.paymentUrl) {
        window.location.href = result.paymentUrl;
      } else {
        throw new Error('Payment URL not received');
      }
    } catch (error) {
      console.error('Error creating payment URL:', error);
      const errorMessage = error.response?.data?.message || error.message || 'Failed to create payment URL';
      alert(errorMessage);
      setLoading(false);
    }
  };

  const handleBackToEvents = () => {
    // Clear localStorage
    localStorage.removeItem('checkoutId');
    localStorage.removeItem('selectedSeats');
    localStorage.removeItem('eventId');
    localStorage.removeItem('eventItemId');
    navigate('/events');
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
    <div style={{ padding: '24px', maxWidth: '1200px', margin: '0 auto' }}>
      <Space direction="vertical" size="large" style={{ width: '100%' }}>
        <Space direction="horizontal" >
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
        </Space>

        {paymentStatus === 'success' && (
          <Alert
            message="Payment Successful"
            description="Your payment has been processed successfully. Your tickets will be available shortly."
            type="success"
            showIcon
            style={{ marginBottom: 24 }}
          />
        )}
        {paymentStatus === 'failed' && (
          <Alert
            message="Payment Failed"
            description="Your payment could not be processed. Please try again."
            type="error"
            showIcon
            style={{ marginBottom: 24 }}
          />
        )}
        {paymentStatus === 'pending' && (
          <Alert
            message="Ready to Pay"
            description="Click the button below to proceed to VNPAY payment gateway."
            type="info"
            showIcon
            style={{ marginBottom: 24 }}
          />
        )}

        <Row gutter={[24, 24]}>
          {/* Left Column: Order Summary */}
          <Col xs={24} md={12}>
            <Card 
              title="Order Summary" 
              type="inner"
              style={{ height: '100%' }}
            >
              <Descriptions bordered column={1}>
                <Descriptions.Item label="Number of Seats">
                  {orderInfo.seatCount} seat(s)
                </Descriptions.Item>
                <Descriptions.Item label="Selected Seats">
                  <Space wrap>
                    {orderInfo.seats.map((seat, index) => (
                      <Tag key={index} style={{ marginBottom: 4 }}>
                        {seat.label || seat.seatKey}
                        {seat.price && ` - ${formatPrice(seat.price)}`}
                      </Tag>
                    ))}
                  </Space>
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
          </Col>

          {/* Right Column: Payment Information */}
          <Col xs={24} md={12}>
            <Card 
              title="Payment Information" 
              type="inner"
              style={{ height: '100%' }}
            >
              <Descriptions bordered column={1}>
                <Descriptions.Item label="Payment Method">
                  <Space>
                    <DollarOutlined />
                    <Text strong>VNPAY</Text>
                  </Space>
                </Descriptions.Item>
                <Descriptions.Item label="Status">
                  {paymentStatus === 'success' && <Tag color="green">Paid</Tag>}
                  {paymentStatus === 'failed' && <Tag color="red">Failed</Tag>}
                  {paymentStatus === 'pending' && <Tag color="orange">Pending</Tag>}
                </Descriptions.Item>
                <Descriptions.Item label="Checkout ID">
                  <Text code>{checkoutId || 'N/A'}</Text>
                </Descriptions.Item>
              </Descriptions>
            </Card>
          </Col>
        </Row>

        <div style={{ textAlign: 'center', marginTop: 24 }}>
          {paymentStatus === 'success' && (
            <Button
              type="primary"
              size="large"
              onClick={handleBackToEvents}
            >
              Back to Events
            </Button>
          )}
          {paymentStatus === 'failed' && (
            <>
              <Button
                type="primary"
                size="large"
                style={{ marginRight: 8 }}
                onClick={handleProceedToPayment}
                loading={loading}
                disabled={!checkoutId || loading}
              >
                Retry Payment
              </Button>
              <Button size="large" onClick={handleBack} disabled={loading}>
                Cancel
              </Button>
            </>
          )}
          {paymentStatus === 'pending' && (
            <>
              <Button
                type="primary"
                size="large"
                style={{ marginRight: 8 }}
                onClick={handleProceedToPayment}
                loading={loading}
                disabled={!checkoutId || loading}
              >
                Proceed to Payment
              </Button>
              <Button size="large" onClick={handleBack} disabled={loading}>
                Cancel
              </Button>
            </>
          )}
        </div>
      </Space>
    </div>
  );
};

export default PaymentSkeleton;
