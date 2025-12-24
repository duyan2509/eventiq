import React, { useState, useEffect } from 'react';
import { Card, Row, Col, Statistic, Typography } from 'antd';
import { CalendarOutlined, DollarOutlined, UserOutlined, CheckCircleOutlined } from '@ant-design/icons';
import { adminAPI } from '../services/api';

const { Title } = Typography;

const AdminDashboard = () => {
    const [stats, setStats] = useState({
        totalEvents: 0,
        pendingEvents: 0,
        publishedEvents: 0,
        totalRevenue: 0,
    });
    const [loading, setLoading] = useState(true);

    useEffect(() => {
        loadStats();
    }, []);

    const loadStats = async () => {
        setLoading(true);
        try {
            // Load events by status to calculate stats
            const [allEvents, pendingEvents, publishedEvents] = await Promise.all([
                adminAPI.getEvents(1, 1, null),
                adminAPI.getEvents(1, 1, 'Pending'),
                adminAPI.getEvents(1, 1, 'Published'),
            ]);

            setStats({
                totalEvents: allEvents.total || 0,
                pendingEvents: pendingEvents.total || 0,
                publishedEvents: publishedEvents.total || 0,
                totalRevenue: 0, // TODO: Calculate from orders/checkouts
            });
        } catch (err) {
            console.error('Failed to load stats:', err);
        } finally {
            setLoading(false);
        }
    };

    return (
        <div>
            <Title level={2}>Dashboard</Title>
            <Row gutter={[16, 16]}>
                <Col xs={24} sm={12} lg={6}>
                    <Card>
                        <Statistic
                            title="Tổng số Event"
                            value={stats.totalEvents}
                            prefix={<CalendarOutlined />}
                            loading={loading}
                        />
                    </Card>
                </Col>
                <Col xs={24} sm={12} lg={6}>
                    <Card>
                        <Statistic
                            title="Event đang chờ duyệt"
                            value={stats.pendingEvents}
                            prefix={<CheckCircleOutlined />}
                            valueStyle={{ color: '#1890ff' }}
                            loading={loading}
                        />
                    </Card>
                </Col>
                <Col xs={24} sm={12} lg={6}>
                    <Card>
                        <Statistic
                            title="Event đã xuất bản"
                            value={stats.publishedEvents}
                            prefix={<CheckCircleOutlined />}
                            valueStyle={{ color: '#52c41a' }}
                            loading={loading}
                        />
                    </Card>
                </Col>
                <Col xs={24} sm={12} lg={6}>
                    <Card>
                        <Statistic
                            title="Tổng doanh thu"
                            value={stats.totalRevenue}
                            prefix={<DollarOutlined />}
                            precision={0}
                            suffix="₫"
                            valueStyle={{ color: '#cf1322' }}
                            loading={loading}
                        />
                    </Card>
                </Col>
            </Row>
        </div>
    );
};

export default AdminDashboard;

