import React, { useState, useEffect } from 'react';
import { Card, Calendar, Badge, Row, Col, Space, Typography, Spin, Empty } from 'antd';
import { CalendarOutlined } from '@ant-design/icons';
import { staffAPI } from '../services/api';
import CurrentShift from '../components/Staff/CurrentShift';
import dayjs from 'dayjs';

const { Title } = Typography;

const StaffWorkspace = () => {
  const [loading, setLoading] = useState(true);
  const [currentShift, setCurrentShift] = useState(null);
  const [calendarData, setCalendarData] = useState(null);
  const [selectedDate, setSelectedDate] = useState(dayjs());

  useEffect(() => {
    fetchData();
  }, []);

  useEffect(() => {
    if (selectedDate) {
      fetchCalendarData(selectedDate.month() + 1, selectedDate.year());
    }
  }, [selectedDate]);

  const fetchData = async () => {
    try {
      setLoading(true);
      const [shiftData, calendarData] = await Promise.all([
        staffAPI.getCurrentShift().catch(() => null),
        fetchCalendarData(dayjs().month() + 1, dayjs().year())
      ]);
      setCurrentShift(shiftData);
    } catch (error) {
      console.error('Error fetching data:', error);
    } finally {
      setLoading(false);
    }
  };

  const fetchCalendarData = async (month, year) => {
    try {
      const data = await staffAPI.getStaffCalendar(month, year);
      setCalendarData(data);
      return data;
    } catch (error) {
      console.error('Error fetching calendar:', error);
      return null;
    }
  };

  const getListData = (value) => {
    if (!calendarData || !calendarData.events) return [];
    
    const dateStr = value.format('YYYY-MM-DD');
    return calendarData.events.filter(event => {
      const eventDate = dayjs(event.date).format('YYYY-MM-DD');
      return eventDate === dateStr;
    });
  };

  const dateCellRender = (value) => {
    const listData = getListData(value);
    return (
      <ul style={{ listStyle: 'none', padding: 0, margin: 0 }}>
        {listData.map((item, index) => (
          <li key={index} style={{ marginBottom: 4 }}>
            <Badge
              status="success"
              text={
                <div style={{ fontSize: '12px' }}>
                  <div style={{ fontWeight: 'bold' }}>{item.eventName}</div>
                  <div style={{ color: '#666', fontSize: '11px' }}>{item.organizationName}</div>
                </div>
              }
            />
          </li>
        ))}
      </ul>
    );
  };

  const onPanelChange = (value) => {
    setSelectedDate(value);
    fetchCalendarData(value.month() + 1, value.year());
  };

  if (loading) {
    return (
      <div style={{ padding: '50px', textAlign: 'center' }}>
        <Spin size="large" />
      </div>
    );
  }

  return (
    <div style={{ padding: '24px', maxWidth: '1400px', margin: '0 auto' }}>
      <Space direction="vertical" size="large" style={{ width: '100%' }}>
        <Title level={2}>
          <CalendarOutlined /> Work Space
        </Title>

        <Row gutter={[24, 24]}>
          <Col xs={24} lg={16}>
            <Card title="Monthly Calendar">
              <Calendar
                cellRender={dateCellRender}
                onPanelChange={onPanelChange}
                value={selectedDate}
                mode="month"
              />
            </Card>
          </Col>

          <Col xs={24} lg={8}>
            <Space direction="vertical" size="large" style={{ width: '100%' }}>
              {currentShift ? (
                <CurrentShift shift={currentShift} />
              ) : (
                <Card>
                  <Empty
                    description="No active shift at the moment"
                    image={Empty.PRESENTED_IMAGE_SIMPLE}
                  />
                </Card>
              )}
            </Space>
          </Col>
        </Row>
      </Space>
    </div>
  );
};

export default StaffWorkspace;
