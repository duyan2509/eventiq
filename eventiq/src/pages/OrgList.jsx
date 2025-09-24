import React from 'react';
import { Card, Row, Col, Button } from 'antd';
import { useNavigate } from 'react-router-dom';
import { useEffect, useState } from 'react';
import { organizationAPI } from '../services/api';
import { useMessage } from '../hooks/useMessage';

const OrgList = () => {
    const navigate = useNavigate();
    const { error , contextHolder} = useMessage();
    const [organizations, setOrganizations] = useState([]);
    const [loading, setLoading] = useState(false);

    useEffect(() => {
        const fetchOrgs = async () => {
            setLoading(true);
            try {
                const orgs = await organizationAPI.getMyOrganizations();
                setOrganizations(orgs);
            } catch (err) {
                error('Failed to load organizations');
            } finally {
                setLoading(false);
            }
        };
        fetchOrgs();
    }, []);

    return (
        <div className="p-6">
            {contextHolder}
            <h1 className="text-2xl font-bold mb-6">My Organizations</h1>
            <Row gutter={[16, 16]}>
                {organizations.map(org => (
                    <Col xs={24} sm={12} md={8} lg={6} key={org.id}>
                        <Card
                            
                            hoverable
                            cover={
                                <img
                                    alt={org.name}
                                    src={org.avatar || 'default-org-image.png'}
                                    className="h-48 object-cover"
                                />
                            }
                            loading={loading}
                            onClick={() => navigate(`/org/${org.id}`)}
                        >
                            <Card.Meta
                                title={org.name}
                                description={org.description}
                            />
                        </Card>
                    </Col>
                ))}
            </Row>
        </div>
    );
};

export default OrgList;