import React, { useState, useEffect } from 'react';
import { useParams, useNavigate } from 'react-router-dom';
import { Card, Typography, Descriptions, Image, Tag, Button, Space, Spin } from 'antd';
import { ArrowLeftOutlined, CalendarOutlined, EnvironmentOutlined, TeamOutlined } from '@ant-design/icons';
import { eventAPI } from '../services/api';
import { useMessage } from '../hooks/useMessage';

const { Title, Paragraph } = Typography;

const EventDetail = () => {
    const { eventId } = useParams();
    const navigate = useNavigate();
    const { error, contextHolder } = useMessage();
    const [event, setEvent] = useState(null);
    const [loading, setLoading] = useState(true);

    useEffect(() => {
        if (eventId) {
            fetchEvent();
        }
    }, [eventId]);

    const fetchEvent = async () => {
        setLoading(true);
        try {
            const data = await eventAPI.getEvent(eventId);
            setEvent(data);
        } catch (err) {
            error('Failed to load event details');
        } finally {
            setLoading(false);
        }
    };

    const handleOrgClick = (orgId) => {
        if (orgId) {
            navigate(`/org/${orgId}`);
        }
    };

    if (loading) {
        return (
            <div style={{ textAlign: 'center', padding: '50px' }}>
                <Spin size="large" />
            </div>
        );
    }

    if (!event) {
        return (
            <div style={{ textAlign: 'center', padding: '50px' }}>
                <Title level={3}>Event not found</Title>
            </div>
        );
    }

    const getStatusTag = (status) => {
        const colors = {
            'Draft': 'default',
            'Pending': 'processing',
            'InProgress': 'warning',
            'Published': 'success',
        };
        return <Tag color={colors[status] || 'default'}>{status}</Tag>;
    };

    return (
        <div style={{ padding: '24px', maxWidth: '1200px', margin: '0 auto' }}>
            {contextHolder}
            <Button
                icon={<ArrowLeftOutlined />}
                onClick={() => navigate(-1)}
                style={{ marginBottom: 16 }}
            >
                Back
            </Button>

            <Card>
                {event.banner && (
                    <Image
                        src={event.banner}
                        alt={event.name}
                        style={{ width: '100%', maxHeight: '400px', objectFit: 'cover', marginBottom: 24 }}
                        preview={false}
                    />
                )}

                <Title level={1}>{event.name}</Title>
                
                <Space style={{ marginBottom: 16 }}>
                    {getStatusTag(event.status)}
                    {event.organization && (
                        <span>
                            <TeamOutlined /> Organization:{' '}
                            <a
                                href={`/org/${event.organization.id}`}
                                onClick={(e) => {
                                    e.preventDefault();
                                    handleOrgClick(event.organization.id);
                                }}
                                style={{ color: '#1890ff', textDecoration: 'none' }}
                            >
                                {event.organization.name}
                            </a>
                        </span>
                    )}
                </Space>

                <Descriptions bordered column={{ xs: 1, sm: 2, md: 3 }}>
                    <Descriptions.Item label="Start Date">
                        <CalendarOutlined /> {new Date(event.start).toLocaleString()}
                    </Descriptions.Item>
                    <Descriptions.Item label="End Date">
                        <CalendarOutlined /> {new Date(event.end).toLocaleString()}
                    </Descriptions.Item>
                    <Descriptions.Item label="Status">
                        {getStatusTag(event.status)}
                    </Descriptions.Item>
                    {event.eventAddress && (
                        <>
                            <Descriptions.Item label="Address" span={3}>
                                <EnvironmentOutlined />{' '}
                                {[
                                    event.eventAddress.detail,
                                    event.eventAddress.communeName,
                                    event.eventAddress.districtName,
                                    event.eventAddress.provinceName,
                                ]
                                    .filter(Boolean)
                                    .join(', ')}
                            </Descriptions.Item>
                        </>
                    )}
                </Descriptions>

                {event.description && (
                    <div style={{ marginTop: 24 }}>
                        <Title level={4}>Description</Title>
                        <Paragraph>{event.description}</Paragraph>
                    </div>
                )}

                {event.accountName && event.accountNumber && (
                    <div style={{ marginTop: 24 }}>
                        <Title level={4}>Payment Information</Title>
                        <Descriptions bordered>
                            <Descriptions.Item label="Account Name">
                                {event.accountName}
                            </Descriptions.Item>
                            <Descriptions.Item label="Account Number">
                                {event.accountNumber}
                            </Descriptions.Item>
                            {event.bankName && (
                                <Descriptions.Item label="Bank">
                                    {event.bankName}
                                </Descriptions.Item>
                            )}
                        </Descriptions>
                    </div>
                )}
            </Card>
        </div>
    );
};

export default EventDetail;

