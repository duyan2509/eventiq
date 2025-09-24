import React, { useEffect, useState } from 'react';
import { useParams } from 'react-router-dom';
import { Card, Tag, Col, Pagination } from 'antd';
import { useNavigate } from 'react-router-dom';
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
                                description={event.description}
                            />
                            <Tag color="blue" className="mt-2">{event.status}</Tag>
                            <p>{event.eventAddress.detail}, {event.eventAddress.communeName}, {event.eventAddress.provinceName}</p>
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