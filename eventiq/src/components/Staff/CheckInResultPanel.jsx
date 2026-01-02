import React from 'react';
import { Card, Result, Typography } from 'antd';
import { CheckCircleOutlined, CloseCircleOutlined } from '@ant-design/icons';

const { Text } = Typography;

const CheckInResultPanel = ({ success, message, ticketId }) => {
  if (success) {
    return (
      <Card>
        <Result
          status="success"
          icon={<CheckCircleOutlined />}
          title="Check-in Successful"
          subTitle={message || 'Ticket has been successfully checked in'}
          extra={
            ticketId && (
              <Text type="secondary">Ticket ID: {ticketId}</Text>
            )
          }
        />
      </Card>
    );
  }

  return (
    <Card>
      <Result
        status="error"
        icon={<CloseCircleOutlined />}
        title="Check-in Failed"
        subTitle={message || 'An error occurred during check-in'}
      />
    </Card>
  );
};

export default CheckInResultPanel;
