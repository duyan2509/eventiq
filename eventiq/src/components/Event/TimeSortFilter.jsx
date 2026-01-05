import React from 'react';
import { Radio } from 'antd';

const { Group, Button } = Radio;

const TimeSortFilter = ({ value, onChange }) => {
  return (
    <Group
      value={value}
      onChange={(e) => onChange(e.target.value)}
      size="large"
    >
      <Button value="asc">Earliest First</Button>
      <Button value="desc">Latest First</Button>
    </Group>
  );
};

export default TimeSortFilter;
