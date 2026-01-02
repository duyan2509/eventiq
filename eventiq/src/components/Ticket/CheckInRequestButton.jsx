import React from 'react';
import { Button } from 'antd';
import { CheckCircleOutlined } from '@ant-design/icons';

const CheckInRequestButton = ({ onClick, disabled }) => {
  return (
    <Button
      type="primary"
      icon={<CheckCircleOutlined />}
      onClick={onClick}
      disabled={disabled}
    >
      Request Check-in
    </Button>
  );
};

export default CheckInRequestButton;
