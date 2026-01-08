import React, { useState, useEffect } from 'react';
import { Select, Space } from 'antd';
import { addressAPI } from '../../services/api';
import TimeSortFilter from './TimeSortFilter';

const { Option } = Select;

const EventFilters = ({ timeSort, province, eventType, onTimeSortChange, onProvinceChange, onEventTypeChange }) => {
  const [provinces, setProvinces] = useState([]);
  const [loading, setLoading] = useState(false);

  useEffect(() => {
    const fetchProvinces = async () => {
      try {
        setLoading(true);
        const res = await addressAPI.getProvinces();
        setProvinces(res.provinces || []);
      } catch (err) {
        console.error('Failed to load provinces:', err);
      } finally {
        setLoading(false);
      }
    };
    fetchProvinces();
  }, []);

  return (
    <Space size="middle" style={{ width: '100%', marginBottom: 24 }}>
      <Select
        placeholder="Event type"
        style={{ width: 150 }}
        value={eventType}
        onChange={onEventTypeChange}
      >
        <Option value="all">All</Option>
        <Option value="upcoming">Upcoming</Option>
        <Option value="past">Past</Option>
      </Select>
      <TimeSortFilter
        value={timeSort}
        onChange={onTimeSortChange}
      />
      <Select
        placeholder="Filter by province"
        style={{ width: 200 }}
        value={province}
        onChange={onProvinceChange}
        loading={loading}
      >
        <Option value="all">All</Option>
        {provinces.map((p) => (
          <Option key={p.code} value={p.code}>
            {p.name}
          </Option>
        ))}
      </Select>
    </Space>
  );
};

export default EventFilters;

