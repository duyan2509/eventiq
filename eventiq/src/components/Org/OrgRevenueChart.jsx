import React from 'react';
import { Card, Empty } from 'antd';
import { PieChart, Pie, Cell, ResponsiveContainer, Legend, Tooltip } from 'recharts';

const OrgRevenueChart = ({ ticketClassRevenues, loading }) => {
    const formatPrice = (price) => {
        return new Intl.NumberFormat('vi-VN', {
            style: 'currency',
            currency: 'VND',
        }).format(price);
    };

    const data = ticketClassRevenues?.filter(tc => tc.revenue > 0).map(tc => ({
        name: tc.ticketClassName,
        value: tc.revenue
    })) || [];

    const COLORS = ['#0088FE', '#00C49F', '#FFBB28', '#FF8042', '#8884d8', '#82ca9d', '#ffc658', '#ff7300'];

    const renderLabel = (entry) => {
        return `${entry.name}: ${formatPrice(entry.value)}`;
    };

    const CustomTooltip = ({ active, payload }) => {
        if (active && payload && payload.length) {
            return (
                <div style={{ 
                    backgroundColor: 'white', 
                    padding: '8px', 
                    border: '1px solid #ccc',
                    borderRadius: '4px'
                }}>
                    <p style={{ margin: 0 }}>{`${payload[0].name}: ${formatPrice(payload[0].value)}`}</p>
                </div>
            );
        }
        return null;
    };

    if (loading) {
        return (
            <Card title="Revenue by Ticket Class" style={{ marginBottom: 24 }}>
                <div style={{ textAlign: 'center', padding: '50px' }}>
                    Loading...
                </div>
            </Card>
        );
    }

    if (!ticketClassRevenues || ticketClassRevenues.length === 0 || data.length === 0) {
        return (
            <Card title="Revenue by Ticket Class" style={{ marginBottom: 24 }}>
                <Empty description="No revenue data available" />
            </Card>
        );
    }

    return (
        <Card title="Revenue by Ticket Class" style={{ marginBottom: 24 }}>
            <ResponsiveContainer width="100%" height={400}>
                <PieChart>
                    <Pie
                        data={data}
                        cx="50%"
                        cy="50%"
                        labelLine={false}
                        label={renderLabel}
                        outerRadius={120}
                        fill="#8884d8"
                        dataKey="value"
                    >
                        {data.map((entry, index) => (
                            <Cell key={`cell-${index}`} fill={COLORS[index % COLORS.length]} />
                        ))}
                    </Pie>
                    <Tooltip content={<CustomTooltip />} />
                    <Legend />
                </PieChart>
            </ResponsiveContainer>
        </Card>
    );
};

export default OrgRevenueChart;

