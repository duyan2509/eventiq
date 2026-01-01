import React, { useState, useEffect } from 'react';
import { Statistic } from 'antd';
import { ClockCircleOutlined } from '@ant-design/icons';

const CountdownTimer = ({ initialSeconds, onExpire }) => {
  const [seconds, setSeconds] = useState(initialSeconds);

  useEffect(() => {
    setSeconds(initialSeconds);
  }, [initialSeconds]);

  useEffect(() => {
    if (seconds <= 0) {
      if (onExpire) {
        onExpire();
      }
      return;
    }

    const timer = setInterval(() => {
      setSeconds((prev) => {
        if (prev <= 1) {
          clearInterval(timer);
          if (onExpire) {
            onExpire();
          }
          return 0;
        }
        return prev - 1;
      });
    }, 1000);

    return () => clearInterval(timer);
  }, [seconds, onExpire]);

  const formatTime = (totalSeconds) => {
    const mins = Math.floor(totalSeconds / 60);
    const secs = totalSeconds % 60;
    return `${mins.toString().padStart(2, '0')}:${secs.toString().padStart(2, '0')}`;
  };

  const getStatus = () => {
    if (seconds <= 0) return 'expired';
    if (seconds <= 60) return 'warning';
    return 'normal';
  };

  const status = getStatus();
  const valueStyle = {
    color: status === 'expired' ? '#ff4d4f' : status === 'warning' ? '#faad14' : '#52c41a',
  };

  return (
    <Statistic
      title="Seat Lock Time Remaining"
      value={formatTime(seconds)}
      prefix={<ClockCircleOutlined />}
      valueStyle={valueStyle}
    />
  );
};

export default CountdownTimer;
