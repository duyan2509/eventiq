import React, { useState, useEffect } from 'react';
import { useParams, useNavigate } from 'react-router-dom';
import { Typography, Button, Space, Spin, Empty } from 'antd';
import { ArrowLeftOutlined, DollarOutlined } from '@ant-design/icons';
import { revenueAPI } from '../services/api';
import OrgRevenueStats from '../components/Org/OrgRevenueStats';
import OrgRevenueChart from '../components/Org/OrgRevenueChart';
import OrgRevenueTable from '../components/Org/OrgRevenueTable';

const { Title } = Typography;

const OrgRevenueReport = () => {
    const { orgId, eventId } = useParams();
    const navigate = useNavigate();
    const [loading, setLoading] = useState(true);
    const [stats, setStats] = useState(null);
    const [eventName, setEventName] = useState('');

    useEffect(() => {
        if (eventId && orgId) {
            fetchRevenueStats();
        }
    }, [eventId, orgId]);

    const fetchRevenueStats = async () => {
        try {
            setLoading(true);
            const statsData = await revenueAPI.getOrgRevenueStats(eventId, orgId);
            setStats(statsData);

            // Get event name from report
            const reportData = await revenueAPI.getOrgRevenueReport(eventId, orgId);
            setEventName(reportData.eventName);
        } catch (error) {
            console.error('Error fetching revenue stats:', error);
        } finally {
            setLoading(false);
        }
    };

    if (loading && !stats) {
        return (
            <div style={{ textAlign: 'center', padding: '50px' }}>
                <Spin size="large" />
            </div>
        );
    }

    if (!stats) {
        return (
            <div style={{ padding: '24px' }}>
                <Empty description="No revenue data available" />
            </div>
        );
    }

    return (
        <div style={{ padding: '24px' }}>
            <Space style={{ marginBottom: 24 }}>
                <Button
                    icon={<ArrowLeftOutlined />}
                    onClick={() => navigate(-1)}
                >
                    Back
                </Button>
                <Title level={3} style={{ margin: 0 }}>
                    Revenue Report {eventName && (
                       <span>
                            for event: {eventName}
                       </span>
                    )}
                </Title>
            </Space>



            <OrgRevenueStats stats={stats} loading={loading} />

            <OrgRevenueChart
                ticketClassRevenues={stats.ticketClassRevenues}
                loading={loading}
            />
            <OrgRevenueTable
                eventId={eventId}
                organizationId={orgId}
                loading={loading}
            />
        </div>
    );
};

export default OrgRevenueReport;
