import React from 'react';
import { Card, Descriptions, Tag, Space, Typography, Button } from 'antd';
import { ClockCircleOutlined, CalendarOutlined, TeamOutlined, LoginOutlined } from '@ant-design/icons';
import { useNavigate } from 'react-router-dom';
import dayjs from 'dayjs';
import utc from 'dayjs/plugin/utc';
import timezone from 'dayjs/plugin/timezone';

dayjs.extend(utc);
dayjs.extend(timezone);

const { Title, Text } = Typography;

const CurrentShift = ({ shift }) => {
  const navigate = useNavigate();

  if (!shift) {
    return null;
  }

  const formatTime = (date) => {
    return dayjs.utc(date).format('HH:mm');
  };

  const formatDate = (date) => {
    return dayjs.utc(date).format('dddd, MMMM D, YYYY');
  };

  const handleCheckinClick = () => {
    navigate(`/events/${shift.eventId}/${shift.eventItemId}`);
  };

  return (
    <Card
      title={
        <Space>
          <ClockCircleOutlined />
          <span>Current Working Shift</span>
        </Space>
      }
      style={{ marginBottom: 24 }}
      extra={
        <Button
          type="primary"
          icon={<LoginOutlined />}
          onClick={handleCheckinClick}
        >
          Check In
        </Button>
      }
    >
      <Descriptions bordered column={1}>
        <Descriptions.Item label="Event">
          <Space direction="vertical" size="small">
            <Text strong>{shift.eventName}</Text>
            <Text type="secondary">
              <TeamOutlined /> {shift.organizationName}
            </Text>
          </Space>
        </Descriptions.Item>
        <Descriptions.Item label="Date">
          <CalendarOutlined /> {formatDate(shift.startTime)}
        </Descriptions.Item>
        <Descriptions.Item label="Time">
          <Space>
            <ClockCircleOutlined />
            <Text>{formatTime(shift.startTime)} - {formatTime(shift.endTime)}</Text>
          </Space>
        </Descriptions.Item>
        <Descriptions.Item label="Assigned Tasks">
          {shift.assignedTasks && shift.assignedTasks.length > 0 ? (
            <Space wrap>
              {shift.assignedTasks.map((task, index) => (
                <Tag key={index} color="blue">
                  {task.taskName}: {task.optionName}
                </Tag>
              ))}
            </Space>
          ) : (
            <Text type="secondary">No tasks assigned</Text>
          )}
        </Descriptions.Item>
      </Descriptions>
    </Card>
  );
};

export default CurrentShift;
