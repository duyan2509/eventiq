import React, { useState, useEffect } from 'react';
import { Row, Col, Typography, Spin, Empty, Pagination } from 'antd';
import { useNavigate, useSearchParams } from 'react-router-dom';
import { customerAPI } from '../services/api';
import EventCard from '../components/Event/EventCard';
import EventFilters from '../components/Event/EventFilters';

const { Title } = Typography;

const CustomerEventList = () => {
  const [loading, setLoading] = useState(true);
  const [events, setEvents] = useState([]);
  const [pagination, setPagination] = useState({
    current: 1,
    pageSize: 12,
    total: 0,
  });
  const [search, setSearch] = useState('');
  const [timeSort, setTimeSort] = useState('asc');
  const [province, setProvince] = useState('all');
  const [eventType, setEventType] = useState('all');
  const navigate = useNavigate();
  const [searchParams, setSearchParams] = useSearchParams();

  useEffect(() => {
    const searchParam = searchParams.get('search') || '';
    const pageParam = parseInt(searchParams.get('page') || '1');
    setSearch(searchParam);
    setPagination(prev => ({ ...prev, current: pageParam }));
  }, [searchParams]);

  useEffect(() => {
    fetchEvents(search, pagination.current, pagination.pageSize, timeSort, province, eventType);
  }, [search, pagination.current, pagination.pageSize, timeSort, province, eventType]);

  const fetchEvents = async (searchValue, page, size, sort, provinceValue, eventTypeValue) => {
    try {
      setLoading(true);
      const normalizedProvince = provinceValue === 'all' ? '' : provinceValue;
      const normalizedEventType = eventTypeValue === 'all' ? '' : eventTypeValue;
      const data = await customerAPI.getEvents(searchValue, page, size, sort || '', normalizedProvince, normalizedEventType);
      setEvents(data.data || []);
      setPagination(prev => ({
        ...prev,
        total: data.total || 0,
      }));
    } catch (error) {
      console.error('Error fetching events:', error);
    } finally {
      setLoading(false);
    }
  };

  const handleEventClick = (eventId) => {
    navigate(`/events/${eventId}`);
  };

  const handleSearchChange = (value) => {
    setSearch(value);
    setPagination(prev => ({ ...prev, current: 1 }));
    setSearchParams({ search: value, page: '1' });
  };

  const handleTimeSortChange = (value) => {
    setTimeSort(value);
    setPagination(prev => ({ ...prev, current: 1 }));
  };

  const handleProvinceChange = (value) => {
    setProvince(value || 'all');
    setPagination(prev => ({ ...prev, current: 1 }));
  };

  const handleEventTypeChange = (value) => {
    setEventType(value || 'all');
    setPagination(prev => ({ ...prev, current: 1 }));
  };

  const handlePageChange = (page, pageSize) => {
    setPagination(prev => ({ ...prev, current: page, pageSize }));
    setSearchParams({ search, page: page.toString() });
  };

  if (loading) {
    return (
      <div style={{ textAlign: 'center', padding: '50px' }}>
        <Spin size="large" />
      </div>
    );
  }

  return (
    <div style={{ padding: '24px', maxWidth: '1200px', margin: '0 auto' }}>
      <Title level={2} style={{ marginBottom: 24 }}>
        Events
      </Title>

      <EventFilters
        timeSort={timeSort}
        province={province}
        eventType={eventType}
        onTimeSortChange={handleTimeSortChange}
        onProvinceChange={handleProvinceChange}
        onEventTypeChange={handleEventTypeChange}
      />

      {events.length === 0 ? (
        <Empty description="No events found" />
      ) : (
        <>
          <Row gutter={[16, 16]}>
            {events.map((event) => (
              <Col xs={24} sm={12} md={8} lg={6} key={event.id}>
                <EventCard event={event} onClick={handleEventClick} />
              </Col>
            ))}
          </Row>
          <div style={{ marginTop: 24, textAlign: 'center' }}>
            <Pagination
              current={pagination.current}
              pageSize={pagination.pageSize}
              total={pagination.total}
              onChange={handlePageChange}
              showSizeChanger
              showQuickJumper
              pageSizeOptions={['12', '24', '48']}
              showTotal={(total, range) => `${range[0]}-${range[1]} of ${total} events`}
            />
          </div>
        </>
      )}
    </div>
  );
};

export default CustomerEventList;
