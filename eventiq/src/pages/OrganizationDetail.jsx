import React, { useEffect, useState } from 'react';
import { useParams } from 'react-router-dom';
import { Card, Tag, Col, Pagination, Button } from 'antd';
import { useNavigate } from 'react-router-dom';
import { convertFromRaw } from 'draft-js';
import { organizationAPI } from '../services/api';
import { useMessage } from '../hooks/useMessage';

const OrganizationDetail = () => {
    const { orgId } = useParams();
    const navigate = useNavigate();
    const { error } = useMessage();
    const [events, setEvents] = useState([]);
    const [loading, setLoading] = useState(false);
    const [pagination, setPagination] = useState({
        page: 1,
        size: 10,
        total: 0
    });

    useEffect(() => {
        fetchEvents();
    }, [orgId, pagination.page]);

    const fetchEvents = async () => {
        setLoading(true);
        try {
            const response = await organizationAPI.getOrgEvents(orgId, pagination.page, pagination.size);
            console.log(response.data)
            setEvents(response.data);
            setPagination(prev => ({
                ...prev,
                total: response.total
            }));
        } catch (err) {
            error('Failed to load events');
        } finally {
            setLoading(false);
        }
    };

    const handlePageChange = (page) => {
        setPagination(prev => ({
            ...prev,
            page: page
        }));
    };

    const getDescriptionText = (description) => {
        if (!description) return '';
        try {
            const raw = JSON.parse(description);
            const content = convertFromRaw(raw);
            return content.getPlainText().slice(0, 200);
        } catch {
            // Fallback: return as-is
            return description;
        }
    };

    return (
        <div className="p-6">
            <h1 className="text-2xl font-bold mb-6">Organization Events</h1>
            <Col gutter={[16, 16]} className="mb-4">
                {events.map(event => (
                        <Card
                        className="mb-4"
                        key={event.id}
                            hoverable
                            cover={
                                <img
                                    alt={event.name}
                                    src={event.banner || 'default-event-image.png'}
                                    className="h-48 object-cover"
                                />
                            }
                            loading={loading}
                            onClick={() => navigate(`/org/${orgId}/event/${event.id}`)}
                        >
                            <Card.Meta
                                title={event.name}
                                description={getDescriptionText(event.description)}
                            />
                            <Tag color="blue" className="mt-2">{event.status}</Tag>
                            <p>
                                {event.eventAddress?.detail || ''}
                                {event.eventAddress?.communeName ? `, ${event.eventAddress.communeName}` : ''}
                                {event.eventAddress?.provinceName ? `, ${event.eventAddress.provinceName}` : ''}
                            </p>
                            <div className="mt-4">
                                <Button 
                                    type="primary" 
                                    onClick={(e) => {
                                        e.stopPropagation();
                                        navigate(`/org/${orgId}/event/${event.id}/staff`);
                                    }}
                                >
                                    Staff Config
                                </Button>
                            </div>
                        </Card>
                ))}
            </Col>
            
            <div className="flex justify-center">
                <Pagination
                    current={pagination.page}
                    pageSize={pagination.size}
                    total={pagination.total}
                    onChange={handlePageChange}
                />
            </div>
        </div>
    );
};

export default OrganizationDetail;