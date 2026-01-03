import React from 'react';
import { Typography, Space, Statistic } from 'antd';
import { KeyOutlined, QrcodeOutlined } from '@ant-design/icons';

const { Text } = Typography;

const OtpDisplayPanel = ({ ticketCode, otp, expiresIn }) => {
  return (
    <Space direction="vertical" size="large" style={{ width: '100%' }}>
      <Space direction="vertical" size="middle" style={{ width: '100%' }}>
        <div>
          <Text strong>Ticket Code:</Text>
          <div style={{ marginTop: 8 }}>
            <Statistic
              value={ticketCode}
              prefix={<QrcodeOutlined />}
              valueStyle={{ fontSize: 24, fontFamily: 'monospace' }}
            />
          </div>
        </div>
        <div>
          <Text strong>OTP:</Text>
          <div style={{ marginTop: 8 }}>
            <Statistic
              precision={0}
              value={otp}
              prefix={<KeyOutlined />}
              valueStyle={{ fontSize: 32, fontFamily: 'monospace', color: '#1890ff' }}
            />
          </div>
        </div>
        <div>
          <Text type="secondary">OTP expires in: {expiresIn} seconds</Text>
        </div>
        <div>
          <Text type="warning">Please show this screen to the staff for check-in</Text>
        </div>
      </Space>
    </Space>
  );
};

export default OtpDisplayPanel;
