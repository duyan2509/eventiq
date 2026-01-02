import React, { useState } from 'react';
import { Form, Input, Button, Card } from 'antd';
import { CheckCircleOutlined } from '@ant-design/icons';
import { staffAPI } from '../../services/api';

const CheckInForm = ({ onSuccess, onError }) => {
  const [form] = Form.useForm();
  const [loading, setLoading] = useState(false);

  const handleSubmit = async (values) => {
    try {
      setLoading(true);
      const result = await staffAPI.checkIn(values.ticketCode, values.otp);
      form.resetFields();
      if (onSuccess) {
        onSuccess(result);
      }
    } catch (error) {
      if (onError) {
        onError(error.response?.data?.message || 'Check-in failed');
      }
    } finally {
      setLoading(false);
    }
  };

  return (
    <Card title="Check-in Ticket">
      <Form form={form} layout="vertical" onFinish={handleSubmit}>
        <Form.Item
          name="ticketCode"
          label="Ticket Code"
          rules={[{ required: true, message: 'Please enter ticket code' }]}
        >
          <Input placeholder="Enter ticket code" />
        </Form.Item>
        <Form.Item
          name="otp"
          label="OTP"
          rules={[{ required: true, message: 'Please enter OTP' }]}
        >
          <Input placeholder="Enter 6-digit OTP" maxLength={6} />
        </Form.Item>
        <Form.Item>
          <Button type="primary" htmlType="submit" icon={<CheckCircleOutlined />} loading={loading} block>
            Check In
          </Button>
        </Form.Item>
      </Form>
    </Card>
  );
};

export default CheckInForm;
